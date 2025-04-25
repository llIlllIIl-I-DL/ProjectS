using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapManager : MonoBehaviour
{
    [Header("Map 설정")]
    public TextAsset mapJsonFile;
    public Transform mapParent;
    
    [Header("씬 관리 설정")]
    public bool useMultiSceneSetup = false;
    public float sceneLoadDistance = 30f;
    public List<SceneModuleData> sceneModules = new List<SceneModuleData>();

    // 컴포넌트 참조
    private Transform playerTransform;
    private IMapLoader mapLoader;
    private IModuleInstantiator moduleInstantiator;
    private ISceneManager sceneManager;

    // 모듈 관리 데이터
    private Dictionary<ModuleInstanceId, GameObject> instancedModules = new Dictionary<ModuleInstanceId, GameObject>();

    [System.Serializable]
    public class SceneModuleData
    {
        public string sceneName;
        public List<string> moduleGuids = new List<string>();
    }

    private void Awake()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // 서비스 초기화
        moduleInstantiator = new ModuleInstantiator(mapParent);
        mapLoader = new MapLoader(moduleInstantiator);
        
        if (useMultiSceneSetup)
        {
            sceneManager = new SceneStreamingManager(sceneLoadDistance, sceneModules);
        }
    }

    private void Start()
    {
        if (mapJsonFile != null)
        {
            LoadMap(mapJsonFile.text);
        }
        
        // 플레이어 찾기
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // 스트리밍 씬 관리자 초기화
        if (useMultiSceneSetup && playerTransform != null)
        {
            sceneManager.Initialize(instancedModules);
        }
    }
    
    private void Update()
    {
        // 씬 관리 업데이트
        if (useMultiSceneSetup && playerTransform != null)
        {
            sceneManager.UpdateSceneLoading(playerTransform.position, instancedModules);
        }
    }

    public void LoadMap(string json)
    {
        try
        {
            ClearMap();
            instancedModules = mapLoader.LoadMapFromJson(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"맵 로딩 중 오류 발생: {e.Message}\n{e.StackTrace}");
        }
    }

    private void ClearMap()
    {
        // 맵의 모든 자식 오브젝트 제거
        if (mapParent != null)
        {
            foreach (Transform child in mapParent)
            {
                Destroy(child.gameObject);
            }
        }

        // 인스턴스 및 캐시 초기화
        instancedModules.Clear();
        moduleInstantiator.ClearCache();
    }

    // 에디터 전용 기능
    #if UNITY_EDITOR
    public void OrganizeModulesIntoScenes()
    {
        EditorSceneOrganizer organizer = new EditorSceneOrganizer();
        sceneModules = organizer.OrganizeByTheme(instancedModules, moduleInstantiator);
    }
    #endif
}

// 모듈 인스턴스 ID (안정적인 키 생성을 위한 구조체)
[System.Serializable]
public struct ModuleInstanceId : IEquatable<ModuleInstanceId>
{
    public string moduleGUID;
    public float posX;
    public float posY;

    public ModuleInstanceId(string guid, Vector2 position)
    {
        moduleGUID = guid;
        posX = position.x;
        posY = position.y;
    }

    public override string ToString()
    {
        return $"{moduleGUID}_{posX:F2}_{posY:F2}";
    }

    public string GetModuleGuid()
    {
        return moduleGUID;
    }

    public bool Equals(ModuleInstanceId other)
    {
        return moduleGUID == other.moduleGUID &&
               Math.Abs(posX - other.posX) < 0.001f &&
               Math.Abs(posY - other.posY) < 0.001f;
    }

    public override bool Equals(object obj)
    {
        return obj is ModuleInstanceId other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + (moduleGUID != null ? moduleGUID.GetHashCode() : 0);
            hash = hash * 23 + posX.GetHashCode();
            hash = hash * 23 + posY.GetHashCode();
            return hash;
        }
    }
}

// 맵 로더 인터페이스
public interface IMapLoader
{
    Dictionary<ModuleInstanceId, GameObject> LoadMapFromJson(string json);
}

// 맵 로더 구현
public class MapLoader : IMapLoader
{
    private IModuleInstantiator moduleInstantiator;

    public MapLoader(IModuleInstantiator instantiator)
    {
        this.moduleInstantiator = instantiator;
    }

    public Dictionary<ModuleInstanceId, GameObject> LoadMapFromJson(string json)
    {
        try
        {
            RoomData mapData = JsonUtility.FromJson<RoomData>(json);
            if (mapData == null || mapData.placedModules == null)
            {
                Debug.LogError("JSON 파싱 실패: 유효하지 않은 맵 데이터");
                return new Dictionary<ModuleInstanceId, GameObject>();
            }

            var instances = InstantiateModules(mapData);
            SetupConnections(mapData, instances);
            return instances;
        }
        catch (Exception e)
        {
            Debug.LogError($"맵 데이터 처리 중 오류: {e.Message}");
            return new Dictionary<ModuleInstanceId, GameObject>();
        }
    }

    private Dictionary<ModuleInstanceId, GameObject> InstantiateModules(RoomData mapData)
    {
        Dictionary<ModuleInstanceId, GameObject> instances = new Dictionary<ModuleInstanceId, GameObject>();

        foreach (var moduleData in mapData.placedModules)
        {
            var instanceId = new ModuleInstanceId(moduleData.moduleGUID, moduleData.position);
            GameObject instance = moduleInstantiator.InstantiateModule(moduleData);
            
            if (instance != null)
            {
                instances[instanceId] = instance;
            }
        }

        return instances;
    }

    private void SetupConnections(RoomData mapData, Dictionary<ModuleInstanceId, GameObject> instances)
    {
        // 모듈 GUID -> 위치 매핑 생성 (빠른 조회용)
        Dictionary<string, Vector2> modulePositions = new Dictionary<string, Vector2>();
        foreach (var module in mapData.placedModules)
        {
            modulePositions[module.moduleGUID] = module.position;
        }

        foreach (var moduleData in mapData.placedModules)
        {
            var sourceId = new ModuleInstanceId(moduleData.moduleGUID, moduleData.position);

            if (instances.TryGetValue(sourceId, out GameObject sourceInstance))
            {
                foreach (var connData in moduleData.connections)
                {
                    // 올바른 대상 위치 가져오기
                    if (modulePositions.TryGetValue(connData.connectedModuleGUID, out Vector2 targetPos))
                    {
                        var targetId = new ModuleInstanceId(connData.connectedModuleGUID, targetPos);

                        if (instances.TryGetValue(targetId, out GameObject targetInstance))
                        {
                            // 소스 모듈의 룸 행동 컴포넌트
                            RoomBehavior sourceRoom = sourceInstance.GetComponent<RoomBehavior>();
                            if (sourceRoom != null)
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

// 모듈 인스턴스화 인터페이스
public interface IModuleInstantiator
{
    GameObject InstantiateModule(RoomData.PlacedModuleData moduleData);
    RoomModule GetModuleByGUID(string guid);
    void ClearCache();
}

// 모듈 인스턴스화 구현
public class ModuleInstantiator : IModuleInstantiator
{
    private Transform parent;
    private Dictionary<string, RoomModule> moduleCache = new Dictionary<string, RoomModule>();
    
    public ModuleInstantiator(Transform parent)
    {
        this.parent = parent;
    }
    
    public GameObject InstantiateModule(RoomData.PlacedModuleData moduleData)
    {
        // 모듈 에셋 로드
        RoomModule moduleAsset = GetModuleByGUID(moduleData.moduleGUID);

        if (moduleAsset == null || moduleAsset.modulePrefab == null)
        {
            Debug.LogWarning($"모듈을 로드할 수 없음: {moduleData.moduleGUID}");
            return null;
        }

        // 모듈 인스턴스화
        GameObject instance = UnityEngine.Object.Instantiate(
            moduleAsset.modulePrefab,
            (Vector3)moduleData.position,
            Quaternion.Euler(0, 0, moduleData.rotationStep * 90),
            parent
        );

        // 모듈 컴포넌트 추가
        RoomBehavior roomBehavior = instance.AddComponent<RoomBehavior>();
        roomBehavior.moduleData = moduleAsset;
        roomBehavior.instanceId = new ModuleInstanceId(moduleData.moduleGUID, moduleData.position).ToString();

        return instance;
    }

    public RoomModule GetModuleByGUID(string guid)
    {
        // 캐시에서 모듈 확인
        if (moduleCache.TryGetValue(guid, out RoomModule module))
        {
            return module;
        }

        // 런타임과 에디터 구분하여 에셋 로드
        #if UNITY_EDITOR
            module = LoadAssetFromEditor(guid);
        #else
            module = LoadAssetAtRuntime(guid);
        #endif

        if (module != null)
        {
            moduleCache[guid] = module;
        }

        return module;
    }

    #if UNITY_EDITOR
    private RoomModule LoadAssetFromEditor(string guid)
    {
        try
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(assetPath))
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<RoomModule>(assetPath);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"에디터 모드에서 모듈 로드 실패: {guid}, {e.Message}");
        }
        return null;
    }
    #endif

    private RoomModule LoadAssetAtRuntime(string guid)
    {
        // 런타임 환경에서 에셋을 로드하는 방법
        // Resources, Addressables, AssetBundle 등 사용
        // 현재는 예시로 비어있음 - 프로젝트에 맞게 구현 필요
        Debug.LogWarning("런타임 환경에서의 에셋 로드 방식이 구현되지 않았습니다.");
        return null;
    }
    
    public void ClearCache()
    {
        moduleCache.Clear();
    }
}

// 씬 관리 인터페이스
public interface ISceneManager
{
    void Initialize(Dictionary<ModuleInstanceId, GameObject> modules);
    void UpdateSceneLoading(Vector3 playerPosition, Dictionary<ModuleInstanceId, GameObject> modules);
}

// 씬 스트리밍 매니저 구현
public class SceneStreamingManager : ISceneManager
{
    private float loadDistance;
    private List<MapManager.SceneModuleData> sceneModules;
    
    private Dictionary<string, string> moduleSceneMap = new Dictionary<string, string>();
    private HashSet<string> loadedScenes = new HashSet<string>();
    private HashSet<string> scenesBeingLoaded = new HashSet<string>();
    
    public SceneStreamingManager(float sceneLoadDistance, List<MapManager.SceneModuleData> sceneModules)
    {
        this.loadDistance = sceneLoadDistance;
        this.sceneModules = sceneModules;
    }
    
    public void Initialize(Dictionary<ModuleInstanceId, GameObject> modules)
    {
        InitModuleSceneMapping();
        loadedScenes.Add(SceneManager.GetActiveScene().name);
    }
    
    private void InitModuleSceneMapping()
    {
        moduleSceneMap.Clear();
        foreach (var sceneData in sceneModules)
        {
            foreach (var guid in sceneData.moduleGuids)
            {
                moduleSceneMap[guid] = sceneData.sceneName;
            }
        }
    }
    
    public void UpdateSceneLoading(Vector3 playerPosition, Dictionary<ModuleInstanceId, GameObject> modules)
    {
        HashSet<string> neededScenes = new HashSet<string> { SceneManager.GetActiveScene().name };
        
        // 필요한 씬 목록 수집
        foreach (var moduleEntry in modules)
        {
            GameObject moduleInstance = moduleEntry.Value;
            float distance = Vector3.Distance(playerPosition, moduleInstance.transform.position);
            
            if (distance < loadDistance)
            {
                string moduleGuid = moduleEntry.Key.GetModuleGuid();
                
                if (moduleSceneMap.TryGetValue(moduleGuid, out string sceneName))
                {
                    neededScenes.Add(sceneName);
                }
            }
        }
        
        // 필요한 씬 로드
        LoadNeededScenes(neededScenes);
        
        // 불필요한 씬 언로드
        UnloadUnneededScenes(neededScenes);
    }
    
    private void LoadNeededScenes(HashSet<string> neededScenes)
    {
        foreach (string sceneName in neededScenes)
        {
            if (!loadedScenes.Contains(sceneName) && !scenesBeingLoaded.Contains(sceneName))
            {
                StartSceneLoad(sceneName);
            }
        }
    }
    
    private void UnloadUnneededScenes(HashSet<string> neededScenes)
    {
        List<string> scenesToUnload = new List<string>();
        foreach (string loadedScene in loadedScenes)
        {
            if (!neededScenes.Contains(loadedScene) && loadedScene != SceneManager.GetActiveScene().name)
            {
                scenesToUnload.Add(loadedScene);
            }
        }
        
        foreach (string sceneToUnload in scenesToUnload)
        {
            UnloadScene(sceneToUnload);
        }
    }
    
    private void StartSceneLoad(string sceneName)
    {
        try
        {
            scenesBeingLoaded.Add(sceneName);
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            
            if (asyncLoad == null)
            {
                Debug.LogError($"씬 로드 실패: {sceneName} - LoadSceneAsync가 null을 반환");
                scenesBeingLoaded.Remove(sceneName);
                return;
            }
            
            asyncLoad.completed += (op) => { 
                loadedScenes.Add(sceneName);
                scenesBeingLoaded.Remove(sceneName);
                Debug.Log($"씬 로드 완료: {sceneName}");
            };
            
            Debug.Log($"씬 로드 시작: {sceneName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"씬 로드 중 오류 발생: {sceneName}, {e.Message}");
            scenesBeingLoaded.Remove(sceneName);
        }
    }
    
    private void UnloadScene(string sceneName)
    {
        try
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);
            
            if (asyncUnload == null)
            {
                Debug.LogError($"씬 언로드 실패: {sceneName} - UnloadSceneAsync가 null을 반환");
                return;
            }
            
            asyncUnload.completed += (op) => { 
                loadedScenes.Remove(sceneName); 
                Debug.Log($"씬 언로드 완료: {sceneName}");
            };
            
            Debug.Log($"씬 언로드 시작: {sceneName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"씬 언로드 중 오류 발생: {sceneName}, {e.Message}");
        }
    }
}

#if UNITY_EDITOR
// 에디터 전용 씬 구성 도구
public class EditorSceneOrganizer
{
    public List<MapManager.SceneModuleData> OrganizeByTheme(
        Dictionary<ModuleInstanceId, GameObject> instances,
        IModuleInstantiator moduleInstantiator)
    {
        try
        {
            Dictionary<RoomModule.EnvironmentTheme, List<string>> themeModules = 
                new Dictionary<RoomModule.EnvironmentTheme, List<string>>();
            
            // 테마별로 모듈 분류
            foreach (var entry in instances)
            {
                string guid = entry.Key.GetModuleGuid();
                RoomModule moduleAsset = moduleInstantiator.GetModuleByGUID(guid);
                
                if (moduleAsset != null)
                {
                    if (!themeModules.ContainsKey(moduleAsset.theme))
                    {
                        themeModules[moduleAsset.theme] = new List<string>();
                    }
                    
                    if (!themeModules[moduleAsset.theme].Contains(guid))
                    {
                        themeModules[moduleAsset.theme].Add(guid);
                    }
                }
            }
            
            // SceneModuleData 생성
            var result = new List<MapManager.SceneModuleData>();
            foreach (var themePair in themeModules)
            {
                string sceneName = $"Scene_{themePair.Key}";
                
                var sceneData = new MapManager.SceneModuleData
                {
                    sceneName = sceneName,
                    moduleGuids = themePair.Value
                };
                
                result.Add(sceneData);
            }
            
            Debug.Log($"테마별로 모듈을 {result.Count}개의 씬으로 구성했습니다.");
            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"씬 구성 중 오류 발생: {e.Message}");
            return new List<MapManager.SceneModuleData>();
        }
    }
}
#endif