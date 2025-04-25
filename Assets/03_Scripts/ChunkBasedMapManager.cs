using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;

/// <summary>
/// 청크 기반 맵 매니저 클래스
/// 분할된 맵 청크를 관리하고 플레이어 위치에 따라 필요한 청크를 로드하거나 언로드합니다.
/// </summary>
public class ChunkBasedMapManager : MonoBehaviour
{
    [Header("기본 설정")]
    public Transform playerTransform;
    public string resourcesJsonFolder = "Maps/Chunks"; // JSON 파일이 저장된 Resources 폴더 경로
    
    [Header("청크 관리 설정")]
    public float chunkSize = 100f;
    public float loadDistance = 150f; // 이 거리 내의 청크를 로드
    public float unloadDistance = 200f; // 이 거리 밖의 청크를 언로드
    
    [Header("청크 씬 설정")]
    public string sceneNameFormat = "Chunk_{0}_{1}"; // 청크 씬 이름 형식 (x, y 좌표가 들어감)
    public string baseSceneName; // 기본 씬 이름 (항상 로드되어 있어야 함)
    
    [Header("JSON 파일 설정")]
    public bool loadModulesFromJson = true; // true면 JSON에서 모듈 로드, false면 씬에 이미 배치된 모듈 사용
    public string jsonFileFormat = "Chunk_{0}_{1}"; // JSON 파일 이름 형식
    
    // 현재 로드된 청크 관리
    private HashSet<Vector2Int> loadedChunks = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> chunksBeingLoaded = new HashSet<Vector2Int>();
    private Vector2Int currentPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);
    
    // 캐싱
    private Dictionary<Vector2Int, ChunkInfo> chunkInfoCache = new Dictionary<Vector2Int, ChunkInfo>();
    private Dictionary<string, RoomModule> moduleCache = new Dictionary<string, RoomModule>();
    private Dictionary<string, GameObject> instancedModules = new Dictionary<string, GameObject>();
    
    // 모듈 부모 오브젝트
    private Transform modulesRoot;
    
    private void Start()
    {
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform == null)
            {
                Debug.LogError("플레이어를 찾을 수 없습니다!");
                return;
            }
        }
        
        // 모듈 부모 오브젝트 생성
        modulesRoot = new GameObject("ModulesRoot").transform;
        modulesRoot.SetParent(transform);
        
        // 기본 씬은 항상 로드되어 있어야 함
        if (!string.IsNullOrEmpty(baseSceneName) && 
            SceneManager.GetSceneByName(baseSceneName).IsValid() && 
            !SceneManager.GetSceneByName(baseSceneName).isLoaded)
        {
            SceneManager.LoadScene(baseSceneName, LoadSceneMode.Additive);
        }
        
        // 초기 청크 로드
        StartCoroutine(ManageChunks());
    }
    
    private void Update()
    {
        // 플레이어의 현재 청크 계산
        Vector2Int newPlayerChunk = GetChunkFromPosition(playerTransform.position);
        
        // 청크가 변경되었을 때만 청크 관리 시작
        if (newPlayerChunk != currentPlayerChunk)
        {
            currentPlayerChunk = newPlayerChunk;
            StartCoroutine(ManageChunks());
        }
    }
    
    /// <summary>
    /// 위치 좌표로부터 청크 ID 계산
    /// </summary>
    private Vector2Int GetChunkFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / chunkSize);
        int y = Mathf.FloorToInt(position.y / chunkSize);
        return new Vector2Int(x, y);
    }
    
    /// <summary>
    /// 청크 거리 계산 (맨해튼 거리)
    /// </summary>
    private int GetChunkDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
    
    /// <summary>
    /// 청크 관리 코루틴 (로드/언로드)
    /// </summary>
    private IEnumerator ManageChunks()
    {
        // 필요한 청크 계산
        HashSet<Vector2Int> neededChunks = new HashSet<Vector2Int>();
        int loadRadius = Mathf.CeilToInt(loadDistance / chunkSize);
        
        // 현재 청크와 주변 청크 모두 포함
        for (int dx = -loadRadius; dx <= loadRadius; dx++)
        {
            for (int dy = -loadRadius; dy <= loadRadius; dy++)
            {
                Vector2Int chunkToCheck = new Vector2Int(currentPlayerChunk.x + dx, currentPlayerChunk.y + dy);
                
                // 맨해튼 거리로 필터링 (원형 영역)
                if (GetChunkDistance(currentPlayerChunk, chunkToCheck) <= loadRadius)
                {
                    neededChunks.Add(chunkToCheck);
                }
            }
        }
        
        // 청크 로드
        foreach (Vector2Int chunk in neededChunks)
        {
            if (!loadedChunks.Contains(chunk) && !chunksBeingLoaded.Contains(chunk))
            {
                chunksBeingLoaded.Add(chunk);
                StartCoroutine(LoadChunk(chunk));
                
                // 동시에 너무 많은 청크를 로드하지 않도록 대기
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        // 불필요한 청크 언로드
        int unloadRadius = Mathf.CeilToInt(unloadDistance / chunkSize);
        List<Vector2Int> chunksToUnload = new List<Vector2Int>();
        
        foreach (Vector2Int loadedChunk in loadedChunks)
        {
            if (GetChunkDistance(currentPlayerChunk, loadedChunk) > unloadRadius)
            {
                chunksToUnload.Add(loadedChunk);
            }
        }
        
        foreach (Vector2Int chunk in chunksToUnload)
        {
            StartCoroutine(UnloadChunk(chunk));
            yield return new WaitForSeconds(0.05f);
        }
    }
    
    /// <summary>
    /// 청크 로드 코루틴 
    /// try-catch 블록 내에서 yield 사용 불가능하므로 수정
    /// </summary>
    private IEnumerator LoadChunk(Vector2Int chunkId)
    {
        string chunkName = string.Format(sceneNameFormat, chunkId.x, chunkId.y);
        Debug.Log($"청크 로드 시작: {chunkName}");
        
        // 씬 로드 시도
        AsyncOperation asyncLoad = null;
        
        try
        {
            asyncLoad = SceneManager.LoadSceneAsync(chunkName, LoadSceneMode.Additive);
        }
        catch (Exception e)
        {
            Debug.LogError($"씬 로드 초기화 중 오류 발생: {chunkName}, {e.Message}");
            chunksBeingLoaded.Remove(chunkId);
            yield break;
        }
        
        if (asyncLoad == null)
        {
            Debug.LogWarning($"씬을 찾을 수 없음: {chunkName}");
            chunksBeingLoaded.Remove(chunkId);
            yield break;
        }
        
        asyncLoad.allowSceneActivation = true;
        
        // 씬 로드 완료 대기
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        // 예외 처리 구역 외부에서 작업 수행
        try
        {
            // 로드된 청크 등록
            loadedChunks.Add(chunkId);
            
            // 청크 정보 컴포넌트 찾기
            Scene loadedScene = SceneManager.GetSceneByName(chunkName);
            ChunkInfo chunkInfo = null;
            
            GameObject[] rootObjects = loadedScene.GetRootGameObjects();
            foreach (GameObject obj in rootObjects)
            {
                ChunkInfo foundInfo = obj.GetComponent<ChunkInfo>();
                if (foundInfo != null)
                {
                    chunkInfo = foundInfo;
                    chunkInfoCache[chunkId] = foundInfo;
                    break;
                }
            }
            
            // 초기화 완료
            if (chunkInfo != null)
            {
                chunkInfo.isInitialized = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"청크 초기화 중 오류 발생: {chunkName}, {e.Message}");
            // 오류가 있어도 계속 진행
        }
        
        // JSON에서 모듈 로드 - 별도의 try-catch로 분리
        if (loadModulesFromJson)
        {
            yield return StartCoroutine(LoadModulesFromJson(chunkId, chunkInfoCache.ContainsKey(chunkId) ? chunkInfoCache[chunkId] : null));
        }
        
        chunksBeingLoaded.Remove(chunkId);
        Debug.Log($"청크 로드 완료: {chunkName}");
    }
    
    /// <summary>
    /// JSON에서 모듈 데이터 로드 및 인스턴스화
    /// </summary>
    private IEnumerator LoadModulesFromJson(Vector2Int chunkId, ChunkInfo chunkInfo)
    {
        string jsonFileName = string.Format(jsonFileFormat, chunkId.x, chunkId.y);
        TextAsset jsonAsset = null;
        
        try
        {
            jsonAsset = Resources.Load<TextAsset>(Path.Combine(resourcesJsonFolder, jsonFileName));
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON 로드 중 오류 발생: {jsonFileName}, {e.Message}");
            yield break;
        }
        
        if (jsonAsset == null)
        {
            Debug.LogWarning($"JSON 파일을 찾을 수 없음: {jsonFileName}");
            yield break;
        }
        
        RoomData chunkData = null;
        
        try
        {
            // JSON 파싱
            chunkData = JsonUtility.FromJson<RoomData>(jsonAsset.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON 파싱 중 오류 발생: {jsonFileName}, {e.Message}");
            yield break;
        }
        
        if (chunkData == null || chunkData.placedModules == null)
        {
            Debug.LogWarning($"청크 데이터 파싱 실패: {jsonFileName}");
            yield break;
        }
        
        // 모듈 인스턴스화
        int moduleCount = 0;
        int batchSize = 5; // 한 번에 처리할 모듈 수
        
        for (int i = 0; i < chunkData.placedModules.Count; i++)
        {
            var moduleData = chunkData.placedModules[i];
            
            // try-catch 블록 밖에서 인스턴스화 (하지만 예외는 개별적으로 포착)
            GameObject instance = null;
            try
            {
                instance = InstantiateModuleFromData(moduleData, chunkId);
            }
            catch (Exception e)
            {
                Debug.LogError($"모듈 인스턴스화 중 오류: {moduleData.moduleGUID}, {e.Message}");
                // 오류가 있어도 다음 모듈로 계속 진행
            }
            
            if (instance != null)
            {
                moduleCount++;
            }
            
            // 프레임 드랍 방지를 위해 대량 모듈 생성 시 분산 처리
            // yield는 try-catch 블록 밖에 있어야 함
            if (chunkData.placedModules.Count > 20 && (i + 1) % batchSize == 0)
            {
                yield return null;
            }
        }
        
        // 연결 설정
        try
        {
            SetupModuleConnections(chunkData);
        }
        catch (Exception e)
        {
            Debug.LogError($"모듈 연결 설정 중 오류 발생: {e.Message}");
        }
        
        if (chunkInfo != null)
        {
            chunkInfo.moduleCount = moduleCount;
            chunkInfo.jsonFilePath = Path.Combine(resourcesJsonFolder, jsonFileName);
        }
        
        Debug.Log($"청크 JSON 로드 완료: {jsonFileName} (모듈 {moduleCount}개)");
    }
    
    /// <summary>
    /// 모듈 데이터로부터 게임 오브젝트 인스턴스화
    /// </summary>
    private GameObject InstantiateModuleFromData(RoomData.PlacedModuleData moduleData, Vector2Int chunkId)
    {
        // 모듈 에셋 로드
        RoomModule moduleAsset = GetModuleByGUID(moduleData.moduleGUID);
        if (moduleAsset == null || moduleAsset.modulePrefab == null)
        {
            Debug.LogWarning($"모듈 로드 실패: {moduleData.moduleGUID}");
            return null;
        }
        
        // ID 생성
        string instanceId = $"{moduleData.moduleGUID}_{moduleData.position.x}_{moduleData.position.y}";
        
        // 이미 인스턴스화 되었는지 확인
        if (instancedModules.TryGetValue(instanceId, out GameObject existingInstance))
        {
            return existingInstance;
        }
        
        // 모듈 인스턴스화
        GameObject instance = Instantiate(
            moduleAsset.modulePrefab,
            (Vector3)moduleData.position,
            Quaternion.Euler(0, 0, moduleData.rotationStep * 90),
            modulesRoot
        );
        
        // 인스턴스 이름 설정
        instance.name = $"Module_{moduleAsset.name}_{chunkId.x}_{chunkId.y}";
        
        // 모듈 컴포넌트 추가
        RoomBehavior roomBehavior = instance.GetComponent<RoomBehavior>();
        if (roomBehavior == null)
        {
            roomBehavior = instance.AddComponent<RoomBehavior>();
        }
        
        roomBehavior.moduleData = moduleAsset;
        roomBehavior.instanceId = instanceId;
        
        // 인스턴스 캐싱
        instancedModules[instanceId] = instance;
        
        return instance;
    }
    
    /// <summary>
    /// 모듈 간 연결 설정
    /// </summary>
    private void SetupModuleConnections(RoomData chunkData)
    {
        // 모듈 위치 매핑 생성 (빠른 조회용)
        Dictionary<string, Vector2> modulePositions = new Dictionary<string, Vector2>();
        foreach (var module in chunkData.placedModules)
        {
            modulePositions[module.moduleGUID] = module.position;
        }
        
        foreach (var moduleData in chunkData.placedModules)
        {
            string sourceId = $"{moduleData.moduleGUID}_{moduleData.position.x}_{moduleData.position.y}";
            
            if (instancedModules.TryGetValue(sourceId, out GameObject sourceInstance))
            {
                RoomBehavior sourceRoom = sourceInstance.GetComponent<RoomBehavior>();
                
                foreach (var connData in moduleData.connections)
                {
                    // 연결 대상 모듈 위치 확인
                    if (modulePositions.TryGetValue(connData.connectedModuleGUID, out Vector2 targetPos))
                    {
                        string targetId = $"{connData.connectedModuleGUID}_{targetPos.x}_{targetPos.y}";
                        
                        if (instancedModules.TryGetValue(targetId, out GameObject targetInstance))
                        {
                            // 연결 설정
                            if (sourceRoom != null)
                            {
                                RoomModule sourceModule = GetModuleByGUID(moduleData.moduleGUID);
                                if (sourceModule != null && connData.connectionPointIndex < sourceModule.connectionPoints.Length)
                                {
                                    sourceRoom.SetupConnection(connData.connectionPointIndex, targetInstance);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 청크 언로드 코루틴 - try-catch 블록 내에서 yield 사용 불가능하므로 수정
    /// </summary>
    private IEnumerator UnloadChunk(Vector2Int chunkId)
    {
        string chunkName = string.Format(sceneNameFormat, chunkId.x, chunkId.y);
        Debug.Log($"청크 언로드 시작: {chunkName}");
        
        // 모듈 제거 (예외 처리)
        if (loadModulesFromJson)
        {
            try
            {
                // 이 청크에 속한 모듈 식별
                Rect chunkBounds = new Rect(
                    chunkId.x * chunkSize,
                    chunkId.y * chunkSize,
                    chunkSize,
                    chunkSize
                );
                
                List<string> modulesToRemove = new List<string>();
                
                foreach (var entry in instancedModules)
                {
                    GameObject moduleInstance = entry.Value;
                    if (moduleInstance != null)
                    {
                        Vector3 pos = moduleInstance.transform.position;
                        if (chunkBounds.Contains(new Vector2(pos.x, pos.y)))
                        {
                            modulesToRemove.Add(entry.Key);
                        }
                    }
                }
                
                // 모듈 제거
                foreach (string key in modulesToRemove)
                {
                    if (instancedModules.TryGetValue(key, out GameObject instance) && instance != null)
                    {
                        Destroy(instance);
                    }
                    instancedModules.Remove(key);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"모듈 제거 중 오류 발생: {e.Message}");
                // 계속 진행
            }
        }
        
        // 씬 언로드 시도
        AsyncOperation asyncUnload = null;
        
        try
        {
            asyncUnload = SceneManager.UnloadSceneAsync(chunkName);
        }
        catch (Exception e)
        {
            Debug.LogError($"씬 언로드 초기화 중 오류 발생: {chunkName}, {e.Message}");
            loadedChunks.Remove(chunkId);
            yield break;
        }
        
        if (asyncUnload == null)
        {
            Debug.LogWarning($"씬을 언로드할 수 없음: {chunkName}");
            loadedChunks.Remove(chunkId);
            yield break;
        }
        
        // 언로드 완료 대기
        while (!asyncUnload.isDone)
        {
            yield return null;
        }
        
        // 청크 정보 캐시에서 제거
        chunkInfoCache.Remove(chunkId);
        
        // 로드된 청크 목록에서 제거
        loadedChunks.Remove(chunkId);
        
        Debug.Log($"청크 언로드 완료: {chunkName}");
    }
    
    /// <summary>
    /// GUID로 모듈 에셋 가져오기 (캐싱 포함)
    /// </summary>
    private RoomModule GetModuleByGUID(string guid)
    {
        // 캐시 확인
        if (moduleCache.TryGetValue(guid, out RoomModule module))
        {
            return module;
        }
        
        // 에셋 로드 (런타임/에디터 모드에 따라 다르게 처리)
#if UNITY_EDITOR
        try
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(assetPath))
            {
                module = UnityEditor.AssetDatabase.LoadAssetAtPath<RoomModule>(assetPath);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"에디터 모드에서 모듈 로드 실패: {guid}, {e.Message}");
        }
#else
        // 런타임 환경에서는 Resources나 Addressables 등을 사용
        try
        {
            // 예시: Resources 폴더에서 로드
            module = Resources.Load<RoomModule>($"Modules/{guid}");
        }
        catch (Exception e)
        {
            Debug.LogError($"런타임 환경에서 모듈 로드 실패: {guid}, {e.Message}");
        }
#endif

        if (module != null)
        {
            moduleCache[guid] = module;
        }
        else
        {
            Debug.LogWarning($"모듈을 찾을 수 없음: {guid}");
        }
        
        return module;
    }
    
    /// <summary>
    /// 지정된 위치로 플레이어 이동 (디버깅 및 테스트용)
    /// </summary>
    public void TeleportPlayer(Vector2 position)
    {
        if (playerTransform != null)
        {
            playerTransform.position = new Vector3(position.x, position.y, playerTransform.position.z);
            
            // 즉시 청크 관리 실행
            Vector2Int newChunk = GetChunkFromPosition(playerTransform.position);
            if (newChunk != currentPlayerChunk)
            {
                currentPlayerChunk = newChunk;
                StartCoroutine(ManageChunks());
            }
        }
    }
    
    /// <summary>
    /// 청크 정보 얻기 (디버깅용)
    /// </summary>
    public List<Vector2Int> GetLoadedChunks()
    {
        return new List<Vector2Int>(loadedChunks);
    }
    
    /// <summary>
    /// 에디터에서 시각화
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            // 로드 범위
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerTransform.position, loadDistance);
            
            // 언로드 범위
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerTransform.position, unloadDistance);
            
            // 현재 청크
            Vector2Int currentChunk = GetChunkFromPosition(playerTransform.position);
            Vector3 chunkCenter = new Vector3(
                (currentChunk.x * chunkSize) + (chunkSize / 2),
                (currentChunk.y * chunkSize) + (chunkSize / 2),
                0
            );
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(chunkCenter, new Vector3(chunkSize, chunkSize, 1));
        }
    }
} 