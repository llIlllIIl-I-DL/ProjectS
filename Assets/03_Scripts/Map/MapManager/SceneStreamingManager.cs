// 씬 관리 인터페이스
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;

public interface ISceneManager
{
    void Initialize(Dictionary<ModuleInstanceId, GameObject> modules);
    void UpdateSceneLoading(Vector3 playerPosition, Dictionary<ModuleInstanceId, GameObject> modules);
}

// 맵 구성에 대한 아이디어 좋다.
// 청크방식으로 나누고 필요한 부분 위주로 렌더링(생성)되는 방식
// 이떄 씬과 프리팹 기반을 정해서 할수 있는데 씬으로 만든 근거는??
// 프리팹에 비해 어떤 장단이 있나?

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