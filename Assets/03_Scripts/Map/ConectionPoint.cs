using UnityEngine;

[System.Serializable]
public class ConnectionPoint
{
    public Vector2 position; // 모듈 내 연결점의 상대적 위치
    public ConnectionDirection direction;
    public ConnectionType type;

    public enum ConnectionDirection
    {
        Up,
        Right,
        Down,
        Left
    }

    public enum ConnectionType
    {
        Normal,        // 일반 연결
        OneWay,        // 일방통행
        LockedDoor,    // 열쇠가 필요한 문
        AbilityGate    // 특정 능력이 필요한 관문
    }
}