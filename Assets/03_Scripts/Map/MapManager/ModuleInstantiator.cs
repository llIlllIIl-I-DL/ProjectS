using System.Collections.Generic;
using System;
using UnityEngine;

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
