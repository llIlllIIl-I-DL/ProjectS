using UnityEngine;

// 불 총알
public class FlameBullet : Bullet
{
    [SerializeField] private float burnDamagePerSecond = 5f;
    [SerializeField] private float burnDuration = 3f;

    protected override void Start()
    {
        BulletType = ElementType.Flame;
        base.Start();
    }

    protected override void ApplySpecialEffect(IDebuffable target)
    {
        // 적에게 화상 효과 적용 (DoT 데미지 구현 필요)
        Debug.Log($"적 {target}에게 화상 효과 적용, 초당 {burnDamagePerSecond} 데미지, 지속시간: {burnDuration}초");
        // 여기서 실제 화상 데미지 적용 로직 구현 필요
    }
} 