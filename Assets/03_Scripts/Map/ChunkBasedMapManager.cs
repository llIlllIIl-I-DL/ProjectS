using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    
    [Header("프리팹 청크 설정")]
    public GameObject[] chunkPrefabs; // 청크 프리팹 배열
    public string defaultChunkPrefabName = "DefaultChunk"; // 기본 청크 프리팹 이름
    public GameObject defaultChunkPrefab; // 기본 청크 프리팹 직접 할당
    public Transform chunksRoot; // 인스턴스화된 청크의 부모 Transform
    
    [Header("JSON 파일 설정")]
    public bool loadModulesFromJson = true; // true면 JSON에서 모듈 로드, false면 씬에 이미 배치된 모듈 사용
    public string jsonFileFormat = "Chunk_{0}_{1}"; // JSON 파일 이름 형식
    
    [Header("청크 제한 설정")]
    public int minChunkX = -1000; // 최소 청크 X 좌표
    public int maxChunkX = 1000;  // 최대 청크 X 좌표
    public int minChunkY = -1000; // 최소 청크 Y 좌표
    public int maxChunkY = 1000;  // 최대 청크 Y 좌표
    public bool limitChunkRange = true; // 청크 범위 제한 활성화
    
    [Header("모듈 매핑 설정")]
    public List<RoomModuleGuidMapping> moduleGuidMappings = new List<RoomModuleGuidMapping>();
    public bool enableDirectMapping = true; // 직접 매핑 활성화
    public string moduleResourcesPath = "Modules"; // RoomModule SO가 저장된 Resources 폴더 경로
    
    // 현재 로드된 청크 관리
    private HashSet<Vector2Int> loadedChunks = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> chunksBeingLoaded = new HashSet<Vector2Int>();
    private Vector2Int currentPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);
    
    // 캐싱
    private Dictionary<Vector2Int, ChunkInfo> chunkInfoCache = new Dictionary<Vector2Int, ChunkInfo>();
    private Dictionary<string, RoomModule> moduleCache = new Dictionary<string, RoomModule>();
    private Dictionary<string, GameObject> instancedModules = new Dictionary<string, GameObject>();
    
    // 프리팹 기반 청크 관리
    private Dictionary<Vector2Int, GameObject> instancedChunks = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
    
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

    [System.Serializable]
    public class RoomModuleGuidMapping
    {
        public string guid;
        public RoomModule module;  // 인스펙터에서 직접 연결할 모듈
    }

    private void Start()
    {
        Debug.Log($"로그 파일 위치: {Application.persistentDataPath}/Player.log");
        Debug.Log($"애플리케이션 버전: {Application.version}, 플랫폼: {Application.platform}");
        
        LoadAllModules();
        
        Debug.Log("========== 청크 기반 맵 매니저 초기화 시작 ==========");
        
        CheckResourcesFolder();
        
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
        
        Debug.Log($"청크 기반 맵 설정: [청크 크기: {chunkSize}] [로드 거리: {loadDistance}] [언로드 거리: {unloadDistance}]");
        
        // 청크 루트 생성
        if (chunksRoot == null) 
        {
            chunksRoot = new GameObject("ChunksRoot").transform;
            chunksRoot.SetParent(transform);
            Debug.Log("청크 부모 오브젝트 생성됨: ChunksRoot");
        }
        
        // 프리팹 캐시 초기화
        if (chunkPrefabs != null && chunkPrefabs.Length > 0)
        {
            foreach (var prefab in chunkPrefabs)
            {
                if (prefab != null)
                {
                    prefabCache[prefab.name] = prefab;
                    Debug.Log($"청크 프리팹 캐싱됨: {prefab.name}");
                }
            }
            Debug.Log($"총 {prefabCache.Count}개 청크 프리팹 캐싱 완료");
        }
        else
        {
            Debug.LogWarning("청크 프리팹이 설정되지 않았습니다!");
        }
        
        // 모듈 부모 오브젝트 생성
        modulesRoot = new GameObject("ModulesRoot").transform;
        modulesRoot.SetParent(transform);
        Debug.Log("모듈 부모 오브젝트 생성됨: ModulesRoot");
        
        // 초기 청크 로드
        Debug.Log("초기 청크 로드 시작");
        StartCoroutine(ManageChunks());
        
        Debug.Log("========== 청크 기반 맵 매니저 초기화 완료 ==========");
    }

    private void LoadAllModules()
    {
        Debug.Log($"모듈 리소스 로드 시작 (경로: {moduleResourcesPath})");
        
        RoomModule[] modules = Resources.LoadAll<RoomModule>(moduleResourcesPath);
        
        foreach (var module in modules)
        {
            if (!string.IsNullOrEmpty(module.assetGUID) && module.modulePrefab != null)
            {
                moduleCache[module.assetGUID] = module;
                Debug.Log($"모듈 캐싱됨: {module.name} (GUID: {module.assetGUID})");
            }
            else
            {
                Debug.LogWarning($"유효하지 않은 모듈 발견: {module.name} (GUID 없음 또는 프리팹 없음)");
            }
        }
        
        Debug.Log($"모듈 리소스 로드 완료: {moduleCache.Count}개 모듈 로드됨");
    }

    private void Update()
    {
        Vector2Int newPlayerChunk = GetChunkFromPosition(playerTransform.position);
        
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
        if (chunkId.x == int.MinValue || chunkId.x == int.MaxValue || 
            chunkId.y == int.MinValue || chunkId.y == int.MaxValue)
        {
            return false;
        }
        
        if (limitChunkRange)
        {
            return chunkId.x >= minChunkX && chunkId.x <= maxChunkX && 
                   chunkId.y >= minChunkY && chunkId.y <= maxChunkY;
        }
        
        return true;
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
        
        HashSet<Vector2Int> neededChunks = new HashSet<Vector2Int>();
        int loadRadius = Mathf.CeilToInt(loadDistance / chunkSize);
        
        // 현재 청크와 주변 청크 확인
        for (int dx = -loadRadius; dx <= loadRadius; dx++)
        {
            for (int dy = -loadRadius; dy <= loadRadius; dy++)
            {
                Vector2Int chunkToCheck = new Vector2Int(currentPlayerChunk.x + dx, currentPlayerChunk.y + dy);
                
                if (!IsValidChunkId(chunkToCheck))
                {
                    continue;
                }
                
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
                StartCoroutine(LoadChunkPrefab(chunk));
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
            StartCoroutine(UnloadChunkPrefab(chunk));
            yield return new WaitForSeconds(0.05f);
        }
    }
    
    /// <summary>
    /// 프리팹 기반 청크 로드 코루틴
    /// </summary>
    private IEnumerator LoadChunkPrefab(Vector2Int chunkId)
    {
        // 유효하지 않은 청크 ID 필터링
        if (!IsValidChunkId(chunkId))
        {
            Debug.LogWarning($"유효하지 않은 청크 ID: {chunkId}, 로드 건너뜀");
            chunksBeingLoaded.Remove(chunkId);
            yield break;
        }
        
        string chunkKey = $"Chunk_{chunkId.x}_{chunkId.y}";
        Debug.Log($"프리팹 청크 로드 시작: {chunkKey} (ID: {chunkId})");
        
        // 이미 인스턴스화된 청크인지 확인
        if (instancedChunks.ContainsKey(chunkId))
        {
            Debug.Log($"청크가 이미 로드됨: {chunkKey}");
            instancedChunks[chunkId].SetActive(true);
            loadedChunks.Add(chunkId);
            chunksBeingLoaded.Remove(chunkId);
            yield break;
        }
        
        // 프리팹 결정 (기본값 또는 특별 청크)
        GameObject prefab = null;
        string prefabName = defaultChunkPrefabName;
        
        // 1. 직접 할당된 기본 프리팹 확인
        if (defaultChunkPrefab != null)
        {
            prefab = defaultChunkPrefab;
        }
        
        // 2. 프리팹 배열에서 확인
        if (prefab == null && chunkPrefabs != null && chunkPrefabs.Length > 0)
        {
            foreach (var p in chunkPrefabs)
            {
                if (p != null)
                {
                    prefab = p;
                    break;
                }
            }
        }
        
        // 3. 캐시된 프리팹 확인
        if (prefab == null && prefabCache.TryGetValue(prefabName, out GameObject cachedPrefab))
        {
            prefab = cachedPrefab;
        }
        
        // 4. 리소스에서 로드 시도
        if (prefab == null)
        {
            string[] possiblePaths = new string[] 
            {
                $"Modules/{prefabName}",
                $"ChunkPrefabs/{prefabName}",
                $"Prefabs/Chunks/{prefabName}",
                prefabName // 직접 이름으로 시도
            };
            
            foreach (string path in possiblePaths)
            {
                prefab = Resources.Load<GameObject>(path);
                if (prefab != null)
                {
                    prefabCache[prefabName] = prefab;
                    break;
                }
            }
        }
        
        // 프리팹을 찾지 못한 경우
        if (prefab == null)
        {
            Debug.LogError($"청크 프리팹을 찾을 수 없음: {prefabName}");
            chunksBeingLoaded.Remove(chunkId);
            yield break;
        }
        
        // 프리팹 인스턴스화
        Vector3 position = new Vector3(
            chunkId.x * chunkSize + (chunkSize / 2),
            chunkId.y * chunkSize + (chunkSize / 2),
            0
        );
        
        GameObject chunkInstance = Instantiate(prefab, position, Quaternion.identity, chunksRoot);
        chunkInstance.name = chunkKey;
        
        // 청크 ID 설정 (ChunkInfo 컴포넌트가 있는 경우)
        ChunkInfo chunkInfo = chunkInstance.GetComponent<ChunkInfo>();
        if (chunkInfo != null)
        {
            chunkInfo.chunkId = chunkId;
            chunkInfo.boundMin = new Vector2(chunkId.x * chunkSize, chunkId.y * chunkSize);
            chunkInfo.boundMax = new Vector2((chunkId.x + 1) * chunkSize, (chunkId.y + 1) * chunkSize);
            chunkInfo.chunkSize = chunkSize;
            chunkInfo.isInitialized = true;
        }
        else
        {
            // ChunkInfo 컴포넌트 추가
            chunkInfo = chunkInstance.AddComponent<ChunkInfo>();
            chunkInfo.chunkId = chunkId;
            chunkInfo.boundMin = new Vector2(chunkId.x * chunkSize, chunkId.y * chunkSize);
            chunkInfo.boundMax = new Vector2((chunkId.x + 1) * chunkSize, (chunkId.y + 1) * chunkSize);
            chunkInfo.chunkSize = chunkSize;
            chunkInfo.isInitialized = true;
        }
        
        // 청크 인스턴스 등록
        instancedChunks[chunkId] = chunkInstance;
        loadedChunks.Add(chunkId);
        
        // JSON에서 모듈 로드
        if (loadModulesFromJson)
        {
            yield return StartCoroutine(LoadModulesFromJson(chunkId, chunkInfo));
        }
        
        // 아이템 필터링 (인벤토리 체크)
        FilterChunkItems(chunkInstance);
        
        chunksBeingLoaded.Remove(chunkId);
        Debug.Log($"프리팹 청크 로드 완료: {chunkKey}");
    }
    
    /// <summary>
    /// 청크 내의 아이템 필터링 (이미 소유한 아이템 제거)
    /// </summary>
    private void FilterChunkItems(GameObject chunkInstance)
    {
        // 인벤토리 매니저 참조
        InventoryManager inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null) return;
        
        // 청크 내의 모든 아이템 찾기
        Item[] items = chunkInstance.GetComponentsInChildren<Item>(true);
        Debug.Log($"청크 내 아이템 필터링: {items.Length}개 발견");
        
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
    /// JSON에서 모듈 데이터 로드하고 인스턴스화
    /// </summary>
    private IEnumerator LoadModulesFromJson(Vector2Int chunkId, ChunkInfo chunkInfo)
    {
        string jsonFileName = string.Format(jsonFileFormat, chunkId.x, chunkId.y);
        string jsonPath = Path.Combine(resourcesJsonFolder, jsonFileName);
        Debug.Log($"JSON 파일 로드 시도: {jsonPath}");
        
        // JSON 파일 로드
        TextAsset jsonAsset = Resources.Load<TextAsset>(jsonPath);
        if (jsonAsset == null)
        {
            Debug.LogWarning($"JSON 파일을 찾을 수 없음: {jsonFileName}");
            yield break;
        }

        // JSON 파싱
        RoomData chunkData = null;
        try
        {
            chunkData = JsonUtility.FromJson<RoomData>(jsonAsset.text);
            Debug.Log($"JSON 파싱 성공: {chunkData.placedModules.Count}개 모듈 데이터");
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

        int moduleCount = 0;
        int batchSize = 5; // 한 번에 처리할 모듈 수

        // JSON의 배치 데이터를 기반으로 모듈 생성
        for (int i = 0; i < chunkData.placedModules.Count; i++)
        {
            var moduleData = chunkData.placedModules[i];
            GameObject instance = InstantiateModuleFromMapping(moduleData, chunkId);
            if (instance != null)
            {
                moduleCount++;
            }

            // 프레임 드랍 방지
            if ((i + 1) % batchSize == 0)
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
    }

    /// <summary>
    /// 매핑된 모듈을 사용하여 프리팹 인스턴스화
    /// </summary>
    private GameObject InstantiateModuleFromMapping(RoomData.PlacedModuleData moduleData, Vector2Int chunkId)
    {
        string instanceId = $"{moduleData.moduleGUID}_{moduleData.position.x}_{moduleData.position.y}";
        
        // 이미 인스턴스화된 경우 반환
        if (instancedModules.TryGetValue(instanceId, out GameObject existingInstance))
        {
            return existingInstance;
        }

        // 캐시에서 모듈 찾기
        RoomModule moduleAsset = null;
        if (moduleCache.TryGetValue(moduleData.moduleGUID, out moduleAsset))
        {
            Debug.Log($"캐시에서 모듈 찾음: {moduleData.moduleGUID}");
        }
        else
        {
            moduleAsset = GetModuleByGUID(moduleData.moduleGUID);
            if (moduleAsset == null)
            {
                Debug.LogWarning($"모듈을 찾을 수 없음: {moduleData.moduleGUID}");
                return null;
            }
        }

        if (moduleAsset.modulePrefab == null)
        {
            Debug.LogWarning($"모듈의 프리팹이 없음: {moduleData.moduleGUID}");
            return null;
        }

        try
        {
            // 프리팹 인스턴스화
            GameObject instance = Instantiate(
                moduleAsset.modulePrefab,
                moduleData.position,
                Quaternion.Euler(0, 0, moduleData.rotationStep * 90),
                modulesRoot
            );

            // 인스턴스 설정
            instance.name = $"Module_{moduleAsset.name}_{chunkId.x}_{chunkId.y}";
            
            // RoomBehavior 설정
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
        catch (Exception e)
        {
            Debug.LogError($"모듈 인스턴스화 중 오류: {moduleData.moduleGUID}, {e.Message}");
            return null;
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
    /// 프리팹 기반 청크 언로드 코루틴
    /// </summary>
    private IEnumerator UnloadChunkPrefab(Vector2Int chunkId)
    {
        string chunkKey = $"Chunk_{chunkId.x}_{chunkId.y}";
        Debug.Log($"프리팹 청크 언로드 시작: {chunkKey}");
        
        // 인스턴스 참조 가져오기
        if (instancedChunks.TryGetValue(chunkId, out GameObject chunkInstance))
        {
            // 청크에 속한 모듈 제거
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
                }
            }
            
            // 즉시 비활성화 (시각적 효과 위해)
            chunkInstance.SetActive(false);
            
            // 완전히 제거
            Destroy(chunkInstance);
            instancedChunks.Remove(chunkId);
        }
        
        // 로드된 청크 목록에서 제거
        loadedChunks.Remove(chunkId);
        
        Debug.Log($"프리팹 청크 언로드 완료: {chunkKey}");
        
        yield return null;
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
        
        // 1. 직접 매핑 확인
        if (enableDirectMapping)
        {
            foreach (var mapping in moduleGuidMappings)
            {
                if (mapping.guid == guid && mapping.module != null)
                {
                    module = mapping.module;
                    moduleCache[guid] = module;
                    return module;
                }
            }
        }
        
        // 2. Resources 폴더에서 모듈 찾기
        string[] searchPaths = {
            $"{moduleResourcesPath}/{guid}",
            $"{moduleResourcesPath}/Rooms/{guid}",
            $"{moduleResourcesPath}/Modules/{guid}",
            guid // 직접 GUID로 시도
        };
        
        foreach (string path in searchPaths)
        {
            module = Resources.Load<RoomModule>(path);
            if (module != null)
            {
                moduleCache[guid] = module;
                return module;
            }
        }
        
        Debug.LogWarning($"모듈을 찾을 수 없음: {guid}");
        return null;
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
    /// 프리팹 청크 인스턴스 얻기 (디버깅용)
    /// </summary>
    public Dictionary<Vector2Int, GameObject> GetChunkInstances()
    {
        return new Dictionary<Vector2Int, GameObject>(instancedChunks);
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