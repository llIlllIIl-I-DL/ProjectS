using UnityEngine;

// 녹 총알
public class RustBullet : Bullet
{
    [SerializeField] private float armorReductionPercent = 20f; // 방어력 감소 퍼센트
    [SerializeField] private float effectDuration = 5f; // 효과 지속시간

    protected override void Start()
    {
        bulletType = ElementType.Rust;
        base.Start();
    }

    protected override void ApplySpecialEffect(BaseEnemy enemy)
    {
        // 적의 방어력을 일시적으로 감소시킴 (구현 필요)
        Debug.Log($"적 {enemy.name}의 방어력이 {armorReductionPercent}% 감소됨, 지속시간: {effectDuration}초");
        // 여기서 실제 적용 로직 구현 필요
    }
} 