using UnityEngine;

// 독 총알
public class PoisonBullet : Bullet
{
    [SerializeField] private float poisonDamagePerSecond = 3f;
    [SerializeField] private float poisonDuration = 5f;

    protected override void Start()
    {
        BulletType = ElementType.Poison;
        base.Start();
    }

    protected override void ApplySpecialEffect(IDebuffable target)
    {
        // 적에게 독 효과 적용 (DoT 데미지 구현 필요)
        Debug.Log($"적 {target}에게 독 효과 적용, 초당 {poisonDamagePerSecond} 데미지, 지속시간: {poisonDuration}초");
        // 여기서 실제 독 데미지 적용 로직 구현 필요
    }
} 