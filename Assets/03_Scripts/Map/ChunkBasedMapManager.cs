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
    
    [Header("청크 제한 설정")]
    public int minChunkX = -1000; // 최소 청크 X 좌표
    public int maxChunkX = 1000;  // 최대 청크 X 좌표
    public int minChunkY = -1000; // 최소 청크 Y 좌표
    public int maxChunkY = 1000;  // 최대 청크 Y 좌표
    public bool limitChunkRange = true; // 청크 범위 제한 활성화
    
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
    
    [System.Serializable]
    public class ModuleGuidPathMapping
    {
        public string guid;
        public string resourcesPath;
    }

    [System.Serializable]
    public class ModuleGuidMappingData
    {
        public List<ModuleGuidPathMapping> mappings = new List<ModuleGuidPathMapping>();
    }

    // GUID-경로 매핑 필드 추가
    private ModuleGuidMappingData guidMappingData;
    private bool mappingInitialized = false;
    
    [System.Serializable]
    public class RoomModuleGuidMapping
    {
        public string guid;
        public RoomModule module;  // 인스펙터에서 직접 연결할 모듈
    }

    [Header("모듈 매핑 설정")]
    public List<RoomModuleGuidMapping> moduleGuidMappings = new List<RoomModuleGuidMapping>();
    public bool enableDirectMapping = true; // 직접 매핑 활성화
    
    private void Start()
    {
        // 로그 파일 위치 출력 (빌드에서 디버깅 용이하게)
        Debug.Log($"로그 파일 위치: {Application.persistentDataPath}/Player.log");
        Debug.Log($"애플리케이션 버전: {Application.version}, 플랫폼: {Application.platform}");
        
        // GUID-경로 매핑 로드
        LoadGuidPathMapping();
        
        Debug.Log("========== 청크 기반 맵 매니저 초기화 시작 ==========");
        
        // 리소스 폴더 검사
        CheckResourcesFolder();
        
        // 플레이어 찾기 및 설정
        if (playerTransform == null)
        {
            Debug.Log("playerTransform이 null, Player 태그로 찾기 시도");
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform == null)
            {
                Debug.LogError("플레이어를 찾을 수 없습니다! 맵 매니저가 제대로 작동하지 않을 수 있습니다.");
                return;
            }
            Debug.Log($"플레이어 찾음: {playerTransform.name} (위치: {playerTransform.position})");
        }
        else
        {
            Debug.Log($"플레이어 이미 설정됨: {playerTransform.name} (위치: {playerTransform.position})");
        }
        
        // 설정 정보 출력
        Debug.Log($"청크 기반 맵 설정: [청크 크기: {chunkSize}] [로드 거리: {loadDistance}] [언로드 거리: {unloadDistance}]");
        Debug.Log($"리소스 경로: [{resourcesJsonFolder}] [씬 이름 형식: {sceneNameFormat}] [JSON 형식: {jsonFileFormat}]");
        
        // 빌드에 포함된 모든 씬 로깅
        Debug.Log("빌드에 포함된 씬 목록:");
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = Path.GetFileNameWithoutExtension(scenePath);
            Debug.Log($"  씬 #{i}: {sceneNameFromPath} (경로: {scenePath})");
        }
        
        // 현재 로드된 씬 로깅
        Debug.Log("현재 로드된 씬 목록:");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            Debug.Log($"  씬: {scene.name} (경로: {scene.path}, 로드됨: {scene.isLoaded})");
        }
        
        // 모듈 부모 오브젝트 생성
        modulesRoot = new GameObject("ModulesRoot").transform;
        modulesRoot.SetParent(transform);
        Debug.Log("모듈 부모 오브젝트 생성됨: ModulesRoot");
        
        // 기본 씬은 항상 로드되어 있어야 함
        if (!string.IsNullOrEmpty(baseSceneName) && 
            SceneManager.GetSceneByName(baseSceneName).IsValid() && 
            !SceneManager.GetSceneByName(baseSceneName).isLoaded)
        {
            Debug.Log($"기본 씬 로드 시도: {baseSceneName}");
            SceneManager.LoadScene(baseSceneName, LoadSceneMode.Additive);
        }
        else
        {
            Debug.Log($"기본 씬 상태: [이름: {baseSceneName}] [비어있음: {string.IsNullOrEmpty(baseSceneName)}] [이미 로드됨: {SceneManager.GetSceneByName(baseSceneName).isLoaded}]");
        }
        
        // 초기 청크 로드
        Debug.Log("초기 청크 로드 시작");
        StartCoroutine(ManageChunks());
        
        Debug.Log("========== 청크 기반 맵 매니저 초기화 완료 ==========");
    }
    
    /// <summary>
    /// GUID와 리소스 경로 매핑 데이터 로드
    /// </summary>
    private void LoadGuidPathMapping()
    {
        try
        {
            // Resources/GuidMapping.json 파일에서 매핑 정보 로드
            TextAsset mappingJson = Resources.Load<TextAsset>("GuidMapping");
            if (mappingJson != null)
            {
                Debug.Log("GUID-경로 매핑 파일 로드 성공");
                guidMappingData = JsonUtility.FromJson<ModuleGuidMappingData>(mappingJson.text);
                mappingInitialized = true;
                Debug.Log($"{guidMappingData.mappings.Count}개의 GUID 매핑 로드됨");
            }
            else
            {
                Debug.LogWarning("GUID-경로 매핑 파일이 없습니다 (Resources/GuidMapping.json)");
                // 빈 매핑 생성
                guidMappingData = new ModuleGuidMappingData();
                mappingInitialized = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"GUID-경로 매핑 로드 오류: {e.Message}");
            guidMappingData = new ModuleGuidMappingData();
            mappingInitialized = true;
        }
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
        Vector2Int chunkId = new Vector2Int(x, y);
        
        // 범위 제한 적용
        if (limitChunkRange)
        {
            chunkId.x = Mathf.Clamp(chunkId.x, minChunkX, maxChunkX);
            chunkId.y = Mathf.Clamp(chunkId.y, minChunkY, maxChunkY);
        }
        
        return chunkId;
    }
    
    /// <summary>
    /// 청크 ID가 유효한지 검사
    /// </summary>
    private bool IsValidChunkId(Vector2Int chunkId)
    {
        // 비정상적인 값 체크 (int.MinValue나 int.MaxValue에 가까운 값)
        if (chunkId.x == int.MinValue || chunkId.x == int.MaxValue || 
            chunkId.y == int.MinValue || chunkId.y == int.MaxValue)
        {
            return false;
        }
        
        // 범위 제한이 활성화된 경우, 지정된 범위 내에 있는지 확인
        if (limitChunkRange)
        {
            return chunkId.x >= minChunkX && chunkId.x <= maxChunkX && 
                   chunkId.y >= minChunkY && chunkId.y <= maxChunkY;
        }
        
        return true;
    }
    
    /// <summary>
    /// 청크 씬이 빌드 설정에 포함되어 있는지 확인
    /// </summary>
    private bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = Path.GetFileNameWithoutExtension(scenePath);
            
            if (sceneNameFromPath == sceneName)
            {
                return true;
            }
        }
        
        return false;
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
        Debug.Log($"청크 관리 시작 - 현재 플레이어 청크: {currentPlayerChunk} (위치: {playerTransform.position})");
        
        // 필요한 청크 계산
        HashSet<Vector2Int> neededChunks = new HashSet<Vector2Int>();
        int loadRadius = Mathf.CeilToInt(loadDistance / chunkSize);
        Debug.Log($"청크 로드 반경: {loadRadius} (청크 {loadRadius*2+1}x{loadRadius*2+1}개 영역)");
        
        // 현재 청크와 주변 청크 모두 포함
        for (int dx = -loadRadius; dx <= loadRadius; dx++)
        {
            for (int dy = -loadRadius; dy <= loadRadius; dy++)
            {
                Vector2Int chunkToCheck = new Vector2Int(currentPlayerChunk.x + dx, currentPlayerChunk.y + dy);
                
                // 청크 ID 유효성 검사 추가
                if (!IsValidChunkId(chunkToCheck))
                {
                    Debug.Log($"유효하지 않은 청크 ID: {chunkToCheck}, 건너뜀");
                    continue;
                }
                
                // 맨해튼 거리로 필터링 (원형 영역)
                if (GetChunkDistance(currentPlayerChunk, chunkToCheck) <= loadRadius)
                {
                    neededChunks.Add(chunkToCheck);
                }
            }
        }
        
        Debug.Log($"로드 필요 청크 {neededChunks.Count}개: {string.Join(", ", neededChunks)}");
        Debug.Log($"현재 로드된 청크 {loadedChunks.Count}개: {string.Join(", ", loadedChunks)}");
        Debug.Log($"현재 로드 중인 청크 {chunksBeingLoaded.Count}개: {string.Join(", ", chunksBeingLoaded)}");
        
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
        // 유효하지 않은 청크 ID 필터링
        if (!IsValidChunkId(chunkId))
        {
            Debug.LogWarning($"유효하지 않은 청크 ID: {chunkId}, 로드 건너뜀");
            chunksBeingLoaded.Remove(chunkId);
            yield break;
        }
        
        string chunkName = string.Format(sceneNameFormat, chunkId.x, chunkId.y);
        Debug.Log($"청크 로드 시작: {chunkName} (ID: {chunkId})");
        
        // 빌드 설정에 씬이 포함되어 있는지 확인
        if (!IsSceneInBuildSettings(chunkName))
        {
            Debug.LogWarning($"씬이 빌드 설정에 없음: {chunkName}, 로드 건너뜀");
            Debug.Log("빌드에 포함된 씬 목록:");
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                Debug.Log($"  씬 #{i}: {Path.GetFileNameWithoutExtension(scenePath)}");
            }
            chunksBeingLoaded.Remove(chunkId);
            yield break;
        }
        
        // 씬 로드 시도
        AsyncOperation asyncLoad = null;
        
        try
        {
            Debug.Log($"AsyncOperation 생성 시도: {chunkName}");
            asyncLoad = SceneManager.LoadSceneAsync(chunkName, LoadSceneMode.Additive);
            Debug.Log(asyncLoad != null ? $"AsyncOperation 생성 성공: {chunkName}" : $"AsyncOperation 생성 실패: {chunkName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"씬 로드 초기화 중 오류 발생: {chunkName}, {e.Message}\n{e.StackTrace}");
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
            Debug.Log($"씬 로드 진행 중: {chunkName} - 진행률: {asyncLoad.progress * 100}%");
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
        string jsonPath = Path.Combine(resourcesJsonFolder, jsonFileName);
        Debug.Log($"JSON 파일 로드 시도: {jsonPath}");
        
        TextAsset jsonAsset = null;
        
        try
        {
            jsonAsset = Resources.Load<TextAsset>(jsonPath);
            Debug.Log(jsonAsset != null ? 
                $"JSON 로드 성공: {jsonPath} (크기: {jsonAsset.text.Length}바이트)" : 
                $"JSON 로드 실패: {jsonPath} - 파일을 찾을 수 없음");
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON 로드 중 오류 발생: {jsonFileName}, {e.Message}\n{e.StackTrace}");
            yield break;
        }
        
        if (jsonAsset == null)
        {
            Debug.LogWarning($"JSON 파일을 찾을 수 없음: {jsonFileName} (경로: {jsonPath})");
            Debug.Log($"Resources 폴더 내 JSON 파일이 존재하는지 확인하세요: {resourcesJsonFolder}/{jsonFileName}");
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
        
        // 인벤토리 매니저 참조
        InventoryManager inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogWarning("InventoryManager를 찾을 수 없습니다.");
            yield break;
        }

        // 플레이어가 가진 아이템 ID 목록 가져오기
        List<string> ownedItemIDs = inventoryManager.GetAllItemIDs();

        // 모듈 인스턴스화
        int moduleCount = 0;
        int batchSize = 5; // 한 번에 처리할 모듈 수
        
        for (int i = 0; i < chunkData.placedModules.Count; i++)
        {
            var moduleData = chunkData.placedModules[i];
            
            // 코스튬 파츠 모듈인 경우 인벤토리 체크
            if (moduleData.moduleType == "Costume" && ownedItemIDs.Contains(moduleData.itemID))
            {
                Debug.Log($"이미 소유한 코스튬 파츠 스킵: {moduleData.itemID}");
                continue;
            }

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
        
        // 인스턴스화된 룸 내의 아이템 체크 및 필터링
        CheckAndFilterItems(instance);
        
        // 인스턴스 캐싱 및 반환
        instancedModules[instanceId] = instance;
        return instance;
    }
    
    // 룸 내의 아이템 체크 및 필터링 메서드
    private void CheckAndFilterItems(GameObject roomInstance)
    {
        // 인벤토리 매니저 참조
        InventoryManager inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null) return;
        
        // 룸 내의 모든 아이템 프리팹 찾기
        Item[] items = roomInstance.GetComponentsInChildren<Item>(true);
        
        foreach (Item item in items)
        {
            // Item 컴포넌트에 ItemData 참조가 있어야 함
            ItemData itemData = item.Itemdata;
            
            if (itemData != null)
            {
                // 코스튬 파츠이고 이미 인벤토리에 있는지 확인
                if (itemData.itemType == ItemType.CostumeParts)
                {
                    // 인벤토리에서 같은 ID의 아이템 찾기
                    ItemData ownedItem = inventoryManager.GetItemById(itemData.id);
                    
                    if (ownedItem != null)
                    {
                        // 이미 소유한 아이템이면 비활성화
                        Debug.Log($"이미 소유한 코스튬 아이템 비활성화: {itemData.ItemName}");
                        item.gameObject.SetActive(false);
                    }
                }
            }
        }
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
        
        Debug.Log($"모듈 로드 시도 (GUID: {guid})");
        
        // 1. 직접 매핑 확인 (가장 안정적)
        if (enableDirectMapping)
        {
            foreach (var mapping in moduleGuidMappings)
            {
                if (mapping.guid == guid && mapping.module != null)
                {
                    module = mapping.module;
                    Debug.Log($"직접 매핑으로 모듈 찾음: {guid} -> {module.name}");
                    moduleCache[guid] = module;
                    return module;
                }
            }
        }
        
        // 에셋 로드 (런타임/에디터 모드에 따라 다르게 처리)
#if UNITY_EDITOR
        try
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"에디터 모드 - GUID에서 에셋 경로 변환: {guid} -> {assetPath}");
            
            if (!string.IsNullOrEmpty(assetPath))
            {
                module = UnityEditor.AssetDatabase.LoadAssetAtPath<RoomModule>(assetPath);
                Debug.Log(module != null ? 
                    $"에디터 모드 - 모듈 로드 성공: {assetPath}" : 
                    $"에디터 모드 - 모듈 로드 실패: {assetPath}");
            }
            else
            {
                Debug.LogWarning($"에디터 모드 - GUID에 해당하는 경로 없음: {guid}");
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
            // 2. GUID 매핑 확인
            string mappedPath = null;
            if (mappingInitialized && guidMappingData != null)
            {
                var mapping = guidMappingData.mappings.Find(m => m.guid == guid);
                if (mapping != null)
                {
                    mappedPath = mapping.resourcesPath;
                    Debug.Log($"GUID 매핑 발견: {guid} -> {mappedPath}");
                }
            }
            
            // 매핑된 경로가 있으면 먼저 시도
            if (!string.IsNullOrEmpty(mappedPath))
            {
                Debug.Log($"매핑된 경로로 모듈 로드 시도: {mappedPath}");
                module = Resources.Load<RoomModule>(mappedPath);
                if (module != null)
                {
                    Debug.Log($"매핑된 경로로 모듈 로드 성공: {mappedPath}");
                }
            }
            
            // 3. 이름으로 찾기 시도 (모듈 이름이 일정한 규칙을 따르는 경우)
            if (module == null)
            {
                // GUID에서 모듈 이름을 추출하는 로직이 있다면 여기에 구현
                // 예: guid가 "Modules/Wall_001" 형태라면 "Wall_001"만 추출
                string possibleName = ExtractModuleNameFromGUID(guid);
                if (!string.IsNullOrEmpty(possibleName))
                {
                    Debug.Log($"이름으로 모듈 로드 시도: {possibleName}");
                    module = Resources.Load<RoomModule>($"Modules/{possibleName}");
                    if (module != null)
                    {
                        Debug.Log($"이름으로 모듈 로드 성공: {possibleName}");
                    }
                }
            }
            
            // 4. 백업: 일반적인 경로 시도
            if (module == null)
            {
                // 다양한 경로 시도
                string[] possiblePaths = {
                    $"Modules/{guid}",
                    $"RoomModules/{guid}",
                    $"Modules/RoomModules/{guid}",
                    guid // 직접 GUID만 사용
                };
                
                foreach (string path in possiblePaths)
                {
                    Debug.Log($"런타임 환경 - 모듈 로드 시도: {path}");
                    module = Resources.Load<RoomModule>(path);
                    
                    if (module != null)
                    {
                        Debug.Log($"런타임 환경 - 모듈 로드 성공: {path}");
                        break;
                    }
                }
                
                if (module == null)
                {
                    Debug.LogError($"런타임 환경 - 모든 경로에서 모듈 로드 실패 (GUID: {guid})");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"런타임 환경에서 모듈 로드 실패: {guid}, {e.Message}\n{e.StackTrace}");
        }
#endif

        if (module != null)
        {
            moduleCache[guid] = module;
            Debug.Log($"모듈 캐시에 추가됨: {guid}");
        }
        else
        {
            Debug.LogWarning($"모듈을 찾을 수 없음: {guid} - Resources 폴더에 모듈 에셋이 있는지 확인하세요");
        }
        
        return module;
    }
    
    // GUID에서 모듈 이름 추출 (필요한 경우 구현)
    private string ExtractModuleNameFromGUID(string guid)
    {
        // 예: guid가 경로를 포함하면 파일 이름만 추출
        int lastSlashIndex = guid.LastIndexOf('/');
        if (lastSlashIndex >= 0 && lastSlashIndex < guid.Length - 1)
        {
            return guid.Substring(lastSlashIndex + 1);
        }
        
        // 다른 이름 추출 로직이 필요하면 여기에 추가
        
        return guid; // 기본값으로 GUID 그대로 반환
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

    /// <summary>
    /// Resources 폴더 내용 검사 - 빌드 시 문제 감지용
    /// </summary>
    private void CheckResourcesFolder()
    {
        Debug.Log("=== Resources 폴더 검사 시작 ===");
        
        // 지정된 JSON 폴더 확인
        string fullJsonPath = resourcesJsonFolder;
        Debug.Log($"JSON 폴더 경로 확인: {fullJsonPath}");
        
        // 실제로 로드 시도
        try
        {
            // 청크 (0,0) JSON 파일 로드 시도
            string testJsonName = string.Format(jsonFileFormat, 0, 0);
            string testJsonPath = Path.Combine(resourcesJsonFolder, testJsonName);
            
            Debug.Log($"테스트 JSON 파일 로드 시도: {testJsonPath}");
            TextAsset testJson = Resources.Load<TextAsset>(testJsonPath);
            
            if (testJson != null)
            {
                Debug.Log($"테스트 JSON 파일 로드 성공: {testJsonPath} (크기: {testJson.text.Length}바이트)");
            }
            else
            {
                Debug.LogWarning($"테스트 JSON 파일 로드 실패: {testJsonPath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Resources 폴더 검사 중 오류: {e.Message}\n{e.StackTrace}");
        }
        
        // 모듈 폴더 확인
        try
        {
            // Modules 폴더 내 모든 에셋 로드 시도
            string[] modulePaths = {
                "Modules",
                "RoomModules",
                "Modules/RoomModules"
            };
            
            foreach (string path in modulePaths)
            {
                Debug.Log($"모듈 폴더 확인: {path}");
                UnityEngine.Object[] modules = Resources.LoadAll(path);
                
                if (modules != null && modules.Length > 0)
                {
                    Debug.Log($"모듈 폴더에서 {modules.Length}개 에셋 찾음: {path}");
                    foreach (var module in modules)
                    {
                        // UnityEngine.Object로 명시적 캐스팅하여 name 속성에 접근
                        Debug.Log($"  - {module.GetType().Name}: {module.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"모듈 폴더에서 에셋을 찾을 수 없음: {path}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"모듈 폴더 검사 중 오류: {e.Message}");
        }
        
        Debug.Log("=== Resources 폴더 검사 완료 ===");
    }
} 