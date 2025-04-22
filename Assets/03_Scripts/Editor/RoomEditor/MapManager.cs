using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapManager : MonoBehaviour
{
    public TextAsset mapJsonFile;
    public Transform mapParent;
    
    // 씬 관리 변수 추가
    [Header("Scene Management")]
    public bool useMultiSceneSetup = false;
    public float sceneLoadDistance = 30f;
    public List<SceneModuleData> sceneModules = new List<SceneModuleData>();
    
    private Transform playerTransform;
    private Dictionary<string, string> moduleSceneMap = new Dictionary<string, string>(); // 모듈ID -> 씬이름 매핑
    private HashSet<string> loadedScenes = new HashSet<string>();

    private Dictionary<string, RoomModule> moduleCache = new Dictionary<string, RoomModule>();
    private Dictionary<string, GameObject> instancedModules = new Dictionary<string, GameObject>();

    [System.Serializable]
    public class SceneModuleData
    {
        public string sceneName;
        public List<string> moduleGuids = new List<string>();
    }

    private void Start()
    {
        if (mapJsonFile != null)
        {
            LoadMap(mapJsonFile.text);
        }
        
        // 플레이어 찾기
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // 모듈-씬 매핑 생성
        if (useMultiSceneSetup)
        {
            InitModuleSceneMapping();
        }
    }
    
    private void Update()
    {
        // 멀티씬 사용 시 플레이어 위치에 따라 씬 로드/언로드
        if (useMultiSceneSetup && playerTransform != null)
        {
            ManageSceneLoading();
        }
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
        
        // 기본 씬은 항상 로드
        loadedScenes.Add(SceneManager.GetActiveScene().name);
    }
    
    private void ManageSceneLoading()
    {
        // 플레이어 위치를 기준으로 근처에 있는 모듈 확인
        Vector3 playerPos = playerTransform.position;
        HashSet<string> neededScenes = new HashSet<string> { SceneManager.GetActiveScene().name };
        
        foreach (var moduleEntry in instancedModules)
        {
            GameObject moduleInstance = moduleEntry.Value;
            float distance = Vector3.Distance(playerPos, moduleInstance.transform.position);
            
            if (distance < sceneLoadDistance)
            {
                string moduleGuid = moduleEntry.Key.Split('_')[0]; // instanceId에서 GUID 추출
                
                if (moduleSceneMap.TryGetValue(moduleGuid, out string sceneName))
                {
                    neededScenes.Add(sceneName);
                }
            }
        }
        
        // 로드해야 할 씬 로드
        foreach (string sceneName in neededScenes)
        {
            if (!loadedScenes.Contains(sceneName))
            {
                LoadScene(sceneName);
            }
        }
        
        // 필요 없는 씬 언로드
        List<string> scenesToUnload = new List<string>();
        foreach (string loadedScene in loadedScenes)
        {
            if (!neededScenes.Contains(loadedScene) && 
                loadedScene != SceneManager.GetActiveScene().name) // 활성 씬은 언로드하지 않음
            {
                scenesToUnload.Add(loadedScene);
            }
        }
        
        foreach (string sceneToUnload in scenesToUnload)
        {
            UnloadScene(sceneToUnload);
        }
    }
    
    private void LoadScene(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        asyncLoad.completed += (op) => { loadedScenes.Add(sceneName); };
        Debug.Log($"Loading scene: {sceneName}");
    }
    
    private void UnloadScene(string sceneName)
    {
        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneName);
        asyncUnload.completed += (op) => { loadedScenes.Remove(sceneName); };
        Debug.Log($"Unloading scene: {sceneName}");
    }

    public void LoadMap(string json)
    {
        // 기존 맵 정리
        ClearMap();

        // JSON 파싱
        RoomData mapData = JsonUtility.FromJson<RoomData>(json);

        // 모듈 인스턴스화
        foreach (var moduleData in mapData.placedModules)
        {
            InstantiateModule(moduleData);
        }

        // 모듈 간 연결 설정
        SetupConnections(mapData);
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

        // 캐시 초기화
        instancedModules.Clear();
    }

    private void InstantiateModule(RoomData.PlacedModuleData moduleData)
    {
        // 모듈 에셋 로드
        RoomModule moduleAsset = GetModuleByGUID(moduleData.moduleGUID);

        if (moduleAsset != null && moduleAsset.modulePrefab != null)
        {
            // 모듈 인스턴스화
            GameObject instance = Instantiate(
                moduleAsset.modulePrefab,
                (Vector3)moduleData.position,
                Quaternion.Euler(0, 0, moduleData.rotationStep * 90),
                mapParent
            );

            // 모듈 ID 저장 (검색용)
            string instanceId = moduleData.moduleGUID + "_" + moduleData.position.ToString();
            instancedModules[instanceId] = instance;

            // 모듈 컴포넌트 추가 (필요시)
            RoomBehavior roomBehavior = instance.AddComponent<RoomBehavior>();
            roomBehavior.moduleData = moduleAsset;
            roomBehavior.instanceId = instanceId;
        }
        else
        {
            Debug.LogWarning("Failed to load module: " + moduleData.moduleGUID);
        }
    }

    private void SetupConnections(RoomData mapData)
    {
        // 연결 설정 (도어, 게이트 등)
        foreach (var moduleData in mapData.placedModules)
        {
            string sourceId = moduleData.moduleGUID + "_" + moduleData.position.ToString();

            if (instancedModules.TryGetValue(sourceId, out GameObject sourceInstance))
            {
                foreach (var connData in moduleData.connections)
                {
                    string targetId = connData.connectedModuleGUID + "_" + GetConnectedModulePosition(mapData, connData);

                    if (instancedModules.TryGetValue(targetId, out GameObject targetInstance))
                    {
                        // 소스 모듈의 룸 행동 컴포넌트
                        RoomBehavior sourceRoom = sourceInstance.GetComponent<RoomBehavior>();
                        if (sourceRoom != null)
                        {
                            // 연결점 정보 가져오기
                            RoomModule sourceModule = GetModuleByGUID(moduleData.moduleGUID);
                            if (sourceModule != null && connData.connectionPointIndex < sourceModule.connectionPoints.Length)
                            {
                                // 연결 설정 (예: 도어 컴포넌트 찾기 및 설정)
                                sourceRoom.SetupConnection(connData.connectionPointIndex, targetInstance);
                            }
                        }
                    }
                }
            }
        }
    }

    private string GetConnectedModulePosition(RoomData mapData, RoomData.ConnectionData connData)
    {
        foreach (var module in mapData.placedModules)
        {
            if (module.moduleGUID == connData.connectedModuleGUID)
            {
                return module.position.ToString();
            }
        }
        return Vector2.zero.ToString();
    }

    private RoomModule GetModuleByGUID(string guid)
    {
        // 캐시에서 모듈 확인
        if (moduleCache.TryGetValue(guid, out RoomModule module))
        {
            return module;
        }

        // 에셋 로드
        string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        if (!string.IsNullOrEmpty(assetPath))
        {
            module = AssetDatabase.LoadAssetAtPath<RoomModule>(assetPath);
            if (module != null)
            {
                moduleCache[guid] = module;
            }
        }

        return module;
    }
    
    // 씬 그룹화 도구 (에디터 모드용)
    #if UNITY_EDITOR
    public void OrganizeModulesIntoScenes()
    {
        Dictionary<RoomModule.EnvironmentTheme, List<string>> themeModules = new Dictionary<RoomModule.EnvironmentTheme, List<string>>();
        
        // 테마별로 모듈 분류
        foreach (var entry in instancedModules)
        {
            string guid = entry.Key.Split('_')[0];
            RoomModule moduleAsset = GetModuleByGUID(guid);
            
            if (moduleAsset != null)
            {
                if (!themeModules.ContainsKey(moduleAsset.theme))
                {
                    themeModules[moduleAsset.theme] = new List<string>();
                }
                
                themeModules[moduleAsset.theme].Add(guid);
            }
        }
        
        // SceneModuleData 생성
        sceneModules.Clear();
        foreach (var themePair in themeModules)
        {
            string sceneName = $"Scene_{themePair.Key}";
            
            SceneModuleData sceneData = new SceneModuleData
            {
                sceneName = sceneName,
                moduleGuids = themePair.Value
            };
            
            sceneModules.Add(sceneData);
        }
        
        Debug.Log($"Organized modules into {sceneModules.Count} scenes based on themes.");
    }
    #endif
}