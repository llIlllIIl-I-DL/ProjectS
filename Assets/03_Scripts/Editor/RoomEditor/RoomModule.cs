using UnityEngine;

// 모듈 데이터를 저장하기 위한 스크립터블 오브젝트
[CreateAssetMenu(fileName = "New Room Module", menuName = "Metroidvania/Room Module")]
public class RoomModule : ScriptableObject
{
    public GameObject modulePrefab;
    public Texture2D thumbnail;
    public ModuleCategory category;
    public EnvironmentTheme theme;
    public ConnectionPoint[] connectionPoints;
    public bool isSpecialRoom; // 보스룸, 아이템룸 등 특별한 방인지

    public enum ModuleCategory
    {
        Combat,
        Puzzle,
        Hub,
        Corridor,
        Village,
        Save,
        Boss,
        Secret
    }
    
    // 환경 테마 열거형 추가
    public enum EnvironmentTheme
    {
        Aether_Dome, // 에테르돔
        Last_Rain,       // 라스트레인
        Waste_Disposal_Plant,     // 폐기물 처리장
        Steel_Mill, // 제철소
        Sewers,    // 하수도
        Thermal_Power_Plant, //화력 발전소
        Central_Cooling_Unit,    // 중앙 냉각장치
    }
}