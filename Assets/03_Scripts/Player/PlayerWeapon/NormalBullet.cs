using UnityEngine;

// 일반 총알
public class NormalBullet : Bullet
{
    protected override void Start()
    {
        bulletType = ElementType.Normal;
        base.Start();
    }

    protected override void ApplySpecialEffect(BaseEnemy enemy)
    {
        // 일반 총알은 특별한 효과 없음
    }
} 