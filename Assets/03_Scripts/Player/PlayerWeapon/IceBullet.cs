using UnityEngine;

// 얼음 총알
public class IceBullet : Bullet
{
    [SerializeField] private float freezeChance = 20f; // 얼릴 확률 %
    [SerializeField] private float freezeDuration = 2f; // 동결 지속시간

    protected override void Start()
    {
        bulletType = ElementType.Ice;
        base.Start();
    }

    protected override void ApplySpecialEffect(BaseEnemy enemy)
    {
        // 적을 얼릴 확률 계산
        if (Random.Range(0f, 100f) <= freezeChance)
        {
            // 적을 얼림 (구현 필요)
            Debug.Log($"적 {enemy.name}이(가) {freezeDuration}초 동안 얼음에 얼었습니다!");
            // 여기서 실제 동결 효과 적용 로직 구현 필요
        }
    }
} 