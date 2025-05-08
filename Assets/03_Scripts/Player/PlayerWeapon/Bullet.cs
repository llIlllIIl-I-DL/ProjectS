using UnityEngine;

// 총알 추상 클래스
public abstract class Bullet : MonoBehaviour
{
    [SerializeField] private float bulletSpeed = 4f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private ElementType bulletType = ElementType.Normal;
    
    [Header("오버차지 설정")]
    [SerializeField] private bool isOvercharged = false;  // 과열 상태 여부

    public float BulletSpeed
    {
        get => bulletSpeed;
        set => bulletSpeed = value;
    }
    public float Damage
    {
        get => damage;
        set => damage = value;
    }
    public float KnockbackForce
    {
        get => knockbackForce;
        set => knockbackForce = value;
    }
    public ElementType BulletType
    {
        get => bulletType;
        set => bulletType = value;
    }
    public bool IsOvercharged
    {
        get => isOvercharged;
        set => isOvercharged = value;
    }
    
    protected bool hasHitEnemy = false;
    protected GameObject playerObject; // 플레이어 게임오브젝트 참조

    // 총알 특수 효과 추상 메서드
    protected abstract void ApplySpecialEffect(BaseEnemy enemy);

    protected virtual void Start()
    {
        playerObject = GameObject.FindGameObjectWithTag("Player");
        var bulletCollider = GetComponent<Collider2D>();
        if (playerObject != null && bulletCollider != null)
        {
            var playerCollider = playerObject.GetComponent<Collider2D>();
            if (playerCollider != null)
                Physics2D.IgnoreCollision(bulletCollider, playerCollider, true);

            Collider2D[] playerChildColliders = playerObject.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D childCollider in playerChildColliders)
            {
                if (childCollider != null)
                    Physics2D.IgnoreCollision(bulletCollider, childCollider, true);
            }

            int bulletLayer = gameObject.layer;
            int playerLayer = playerObject.layer;
            Physics2D.IgnoreLayerCollision(bulletLayer, playerLayer, true);

#if UNITY_EDITOR
            Debug.Log($"레이어 충돌 무시 설정 - 총알 레이어: {LayerMask.LayerToName(bulletLayer)}, 플레이어 레이어: {LayerMask.LayerToName(playerLayer)}");
#endif
        }
#if UNITY_EDITOR
        Debug.Log("총알 생성됨: " + transform.position + ", 레이어: " + LayerMask.LayerToName(gameObject.layer));
#endif
    }
    
    // 업데이트 가상 메서드 추가
    protected virtual void Update()
    {
        // 파생 클래스에서 오버라이드 가능
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와의 충돌은 무시
        if (other.gameObject.CompareTag("Player") || 
            (playerObject != null && other.transform.IsChildOf(playerObject.transform)))
        {
            Debug.Log("플레이어 또는 플레이어 자식과 충돌하여 무시됨");
            return;
        }

        // 반사경 확인
        if (other.CompareTag("Mirror"))
        {
            ObjectMirror mirror = other.GetComponent<ObjectMirror>();
            if (mirror != null)
            {
                mirror.TakeDamage(damage);  
                Destroy(gameObject);
            }
        }

        // 파괴 가능한 오브젝트 확인
        if (other.CompareTag("Destructible"))
        {
            DestructibleObject destructible = other.GetComponent<DestructibleObject>();
            if (destructible != null)
            {
                destructible.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
        
        // 적 레이어 확인
        if (other.CompareTag("Enemy"))
        {
            // 적에게 데미지 주기
            BaseEnemy enemy = other.GetComponent<BaseEnemy>();//Enemy로 수정 필요

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                hasHitEnemy = true;

                // 각 타입별 특수 효과 적용
                ApplySpecialEffect(enemy);

                // 넉백 적용
                Rigidbody2D enemyRb = other.GetComponent<Rigidbody2D>();
                if (enemyRb != null)
                {
                    Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
                    enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
                }
                
                // 과열 공격이 적중했을 경우 플레이어에게 데미지
                if (isOvercharged)
                {
                    // 플레이어 찾기
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        PlayerHP playerHP = player.GetComponent<PlayerHP>();
                        if (playerHP != null)
                        {
                            // 플레이어 최대 체력의 5% 데미지
                            float selfDamage = playerHP.MaxHP * 0.05f;
                            playerHP.TakeDamage(selfDamage);
                            Debug.Log($"과열 공격 반동으로 플레이어가 {selfDamage} 데미지를 입었습니다!");
                        }
                    }
                }
            }

            // 총알 제거
            Debug.Log("적과 충돌하여 총알 제거됨");
            Destroy(gameObject);
        }
        // 벽이나 다른 장애물과 충돌
        else if (((1 << other.gameObject.layer) & (LayerMask.GetMask("Ground", "Wall"))) != 0)
        {
            // 총알 제거
            Debug.Log("벽과 충돌하여 총알 제거됨: " + other.gameObject.name);
            Destroy(gameObject);
        }
        else
        {
            // 기타 충돌 디버깅
            Debug.Log("다른 오브젝트와 충돌: " + other.gameObject.name + ", 태그: " + other.tag + ", 레이어: " + LayerMask.LayerToName(other.gameObject.layer));
        }
    }
    
    // 총알이 파괴될 때 호출되는 함수
    protected virtual void OnDestroy()
    {
        // 파생 클래스에서 오버라이드 가능
    }
}