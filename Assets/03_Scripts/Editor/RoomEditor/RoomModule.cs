
using UnityEngine;

// 모듈 데이터를 저장하기 위한 스크립터블 오브젝트
[CreateAssetMenu(fileName = "New Room Module", menuName = "Metroidvania/Room Module")]
public class RoomModule : ScriptableObject
{
    public GameObject modulePrefab;
    public Texture2D thumbnail;
    public ModuleCategory category;
    public ConnectionPoint[] connectionPoints;
    public bool isSpecialRoom; // 보스룸, 아이템룸 등 특별한 방인지

    public enum ModuleCategory
    {
        Combat,
        Puzzle,
        Hub,
        Corridor,
        Shop,
        Save,
        Boss,
        Secret
    }
}