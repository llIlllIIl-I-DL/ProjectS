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