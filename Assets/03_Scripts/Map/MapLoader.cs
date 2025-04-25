using System.Collections.Generic;
using System;
using UnityEngine;

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