using UnityEngine;

public class MapLoader : MonoBehaviour
{
    [Header("Resources/Maps/{fileName}.json")]
    [SerializeField] string mapFileName = "Maps/your_map";

    void Start()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>(mapFileName);
        if (jsonAsset == null)
        {
            Debug.LogError($"맵 JSON을 찾을 수 없습니다: Resources/{mapFileName}.json");
            return;
        }

        RoomData data = JsonUtility.FromJson<RoomData>(jsonAsset.text);
        BuildMapFromData(data);
    }

    void BuildMapFromData(RoomData data)
    {
        // 에디터 저장 로직의 역순으로
        // data.placedModules 순회하며 Prefab Instantiate → 위치·회전 복원
    }
}