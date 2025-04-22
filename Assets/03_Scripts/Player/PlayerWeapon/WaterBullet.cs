using UnityEngine;

// 물 총알
public class WaterBullet : Bullet
{
    [SerializeField] private float slowPercent = 30f;
    [SerializeField] private float slowDuration = 3f;

    protected override void Start()
    {
        bulletType = ElementType.Water;
        base.Start();
    }

    protected override void ApplySpecialEffect(BaseEnemy enemy)
    {
        // 적의 이동속도를 감소시킴 (구현 필요)
        Debug.Log($"적 {enemy.name}의 이동속도가 {slowPercent}% 감소됨, 지속시간: {slowDuration}초");
        // 여기서 실제 슬로우 적용 로직 구현 필요
    }
} 