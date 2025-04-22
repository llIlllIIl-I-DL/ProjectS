using System.Collections.Generic;
using UnityEngine;

public class MapLoader : MonoBehaviour
{
    [Header("Resources/Maps/{fileName}.json")]
    [SerializeField] string mapFileName = "Maps/your_map";

    [Header("에디터에서 만든 RoomModule ScriptableObject들을 여기에 드래그하세요")]
    [SerializeField] List<RoomModule> roomModules;

    // GUID → RoomModule 매핑 딕셔너리
    Dictionary<string, RoomModule> moduleDict;

    void Awake()
    {
        moduleDict = new Dictionary<string, RoomModule>(roomModules.Count);
        foreach (var m in roomModules)
        {
            // 에디터에서 ScriptableObject에 미리 GUID 필드를 채워 두셔야 합니다
            moduleDict[m.assetGUID] = m;
        }
    }

    void Start()
    {
        // 1) Resources 폴더에서 JSON(TextAsset) 로드
        TextAsset jsonAsset = Resources.Load<TextAsset>(mapFileName);
        if (jsonAsset == null)
        {
            Debug.LogError($"맵 JSON을 찾을 수 없습니다: Resources/{mapFileName}.json");
            return;
        }

        // 2) JsonUtility로 파싱
        RoomData data = JsonUtility.FromJson<RoomData>(jsonAsset.text);
        if (data == null)
        {
            Debug.LogError("맵 JSON 파싱에 실패했습니다.");
            return;
        }

        // 3) 파싱된 데이터로 맵 복원
        BuildMapFromData(data);
    }

    void BuildMapFromData(RoomData data)
    {
        foreach (var placed in data.placedModules)
        {
            // 1) GUID로 RoomModule 가져오기
            if (!moduleDict.TryGetValue(placed.moduleGUID, out var roomModule))
            {
                Debug.LogWarning($"등록되지 않은 모듈 GUID: {placed.moduleGUID}");
                continue;
            }

            // 2) 프리팹 인스턴스화
            GameObject go = Instantiate(roomModule.modulePrefab);

            // 3) 위치 및 회전 적용
            go.transform.position = new Vector3(placed.position.x,
                                                placed.position.y,
                                                0f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, placed.rotationStep * 90f);

            // 4) (옵션) 연결선이나 추가 정보 복원 로직
            //    e.g. go.GetComponent<YourRoomBehaviour>().InitializeConnections(placed.connections);
        }
    }
}