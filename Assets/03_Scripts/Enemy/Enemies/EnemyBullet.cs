using UnityEngine;

public class EnemyBullet : Bullet
{
    [SerializeField] public bool isPiercing = false;
    [Header("충돌 애니메이션")]
    [SerializeField] private string groundHitAnimTrigger = "GroundHit";
    [SerializeField] private string wallHitAnimTrigger = "WallHit";

    private BaseEnemy enemy;
    
    /// <summary>
    /// 총알 데미지 설정
    /// </summary>
    public void SetDamage(float newDamage)
    {
        Damage = newDamage;
    }
    
    protected override void Start()
    {
        base.Start();
        // 일정 시간 후 자동 파괴 (Bullet의 lifeTime 사용)
        //Destroy(gameObject, LifeTime);
    }
    
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 충돌한 경우
        if (other.CompareTag("Player"))
        {   
            // 플레이어에게 데미지 주기
            IDamageable player = other.GetComponent<IDamageable>();
            if (player != null)
            {
                player.TakeDamage(Damage);
                Debug.Log($"플레이어에게 {Damage} 데미지!");
            }
            // 관통이 아니면 풀링 반환
            if (!isPiercing)
                ObjectPoolingManager.Instance.ReturnBullet(gameObject, BulletType);
        }
        // 벽이나 장애물과 충돌
        else if (((1 << other.gameObject.layer) & (LayerMask.GetMask("Ground", "Wall", "NoCollision"))) != 0)
        {
            // 레이어에 따라 다른 애니메이션 재생
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                if (enemy.Animator != null) enemy.Animator.SetTrigger(groundHitAnimTrigger);
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
            {
                if (enemy.Animator != null) enemy.Animator.SetTrigger(wallHitAnimTrigger);
            }

            ObjectPoolingManager.Instance.ReturnBullet(gameObject, BulletType);
        }
    }

    protected override void ApplySpecialEffect(IDebuffable target)
    {
        // 적 전용 총알은 특수 효과 없음
    }

    // 필요하다면 EnemyBullet만의 고유 기능 추가
    // (예: 관통 처리 등)
}