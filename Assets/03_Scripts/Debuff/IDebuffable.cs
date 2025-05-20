using UnityEngine;

public interface IDebuffable : IDamageable
{
    float CurrentHP { get; set; }
    float MoveSpeed { get; set; }
    float Defence { get; set; }

    // 필요시 디버프 관련 추가 메서드 선언 가능
    // 예: void ApplyDebuffEffect(DebuffType type, float value);
    // 예: void RemoveDebuffEffect(DebuffType type);
    GameObject gameObject { get; }
    Transform transform { get; }
} 