using UnityEngine;

// 일반 총알
public class NormalBullet : Bullet
{
    [SerializeField] private bool piercing = false; // 관통 여부
    protected override void Start()
    {
        BulletType = ElementType.Normal;
        base.Start();

        //관통
        if (piercing)
        {
            // 관통 설정
            Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Enemy"), true);
            Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Player"), true);
        }
    }

    protected override void ApplySpecialEffect(IDebuffable target)
    {
        // 일반 총알은 특별한 효과 없음
    }
} 