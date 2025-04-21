using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public TextAsset mapJsonFile;
    public Transform mapParent;

    private Dictionary<string, RoomModule> moduleCache = new Dictionary<string, RoomModule>();
    private Dictionary<string, GameObject> instancedModules = new Dictionary<string, GameObject>();

    private void Start()
    {
        if (mapJsonFile != null)
        {
            LoadMap(mapJsonFile.text);
        }
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
}