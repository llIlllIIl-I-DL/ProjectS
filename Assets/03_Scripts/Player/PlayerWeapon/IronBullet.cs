using UnityEngine;

// 철 총알
public class IronBullet : Bullet
{
    [SerializeField] private float extraDamageMultiplier = 1.5f;
    
    // 오버차지 관련 설정
    [Header("오버차지 설정")]
    [SerializeField] private float overchargedLifetime = 3f; // 오버차지 상태에서 충돌 후 잔존 시간
    [SerializeField] private float overchargedScale = 1.2f; // 오버차지 상태에서 크기 배율
    
    private bool hasCollided = false; // 충돌 발생 여부
    private float destroyTimer = 0f; // 파괴 타이머

    protected override void Start()
    {
        bulletType = ElementType.Iron;
        damage *= extraDamageMultiplier; // 더 높은 기본 데미지
        
        // 오버차지 상태라면 크기 증가
        if (isOvercharged)
        {
            transform.localScale *= overchargedScale;
            damage *= 1.5f; // 오버차지 상태에서 추가 데미지
        }
        
        base.Start();
    }
    
    protected override void Update()
    {
        // 항상 기본 클래스의 Update 메서드 호출
        base.Update();
        
        // 충돌 후 오버차지 상태라면 일정 시간 후 파괴
        if (hasCollided && isOvercharged)
        {
            destroyTimer += Time.deltaTime;
            
            // 잔존 시간이 지나면 파괴
            if (destroyTimer >= overchargedLifetime)
            {
                Debug.Log("철 총알의 잔존 시간이 끝나 파괴됩니다.");
                Destroy(gameObject);
            }
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와의 충돌은 무시
        if (other.gameObject.CompareTag("Player") || 
            (playerObject != null && other.transform.IsChildOf(playerObject.transform)))
        {
            Debug.Log("플레이어 또는 플레이어 자식과 충돌하여 무시됨");
            return;
        }
        
        // 적 레이어 확인
        if (other.CompareTag("Enemy"))
        {
            // 적에게 데미지 주기
            BaseEnemy enemy = other.GetComponent<BaseEnemy>();

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
                
                // 충돌 발생 설정
                hasCollided = true;
                
                // 오버차지 상태가 아니면 즉시 파괴, 오버차지 상태라면 잠시 유지 후 파괴
                if (!isOvercharged)
                {
                    Debug.Log("적과 충돌하여 철 총알 즉시 제거됨");
                    Destroy(gameObject);
                }
                else
                {
                    // 오버차징된 철 총알은 잠시 유지
                    Debug.Log($"오버차징된 철 총알이 {overchargedLifetime}초 동안 유지됩니다.");
                    
                    // 충돌 후 동작 변경
                    AfterCollisionSetup();
                }
            }
        }
        // 벽이나 다른 장애물과 충돌
        else if (((1 << other.gameObject.layer) & (LayerMask.GetMask("Ground", "Wall"))) != 0)
        {
            // 충돌 발생 설정
            hasCollided = true;
            
            // 오버차지 상태가 아니면 즉시 파괴, 오버차지 상태라면 잠시 유지 후 파괴
            if (!isOvercharged)
            {
                Debug.Log("벽과 충돌하여 철 총알 즉시 제거됨: " + other.gameObject.name);
                Destroy(gameObject);
            }
            else
            {
                // 오버차징된 철 총알은 잠시 유지
                Debug.Log($"오버차징된 철 총알이 {overchargedLifetime}초 동안 유지됩니다.");
                
                // 충돌 후 동작 변경
                AfterCollisionSetup();
            }
        }
        else
        {
            // 기타 충돌 디버깅
            Debug.Log("다른 오브젝트와 충돌: " + other.gameObject.name + ", 태그: " + other.tag + ", 레이어: " + LayerMask.LayerToName(other.gameObject.layer));
        }
    }
    
    // 충돌 후 설정 변경
    private void AfterCollisionSetup()
    {
        // 충돌 후에는 더 이상 이동하지 않도록 설정
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }
        
        // 충돌 효과 (파티클 등)
        // 필요시 파티클 시스템 추가 구현
        
        // 오브젝트의 레이어 변경하여 다른 충돌 방지
        gameObject.layer = LayerMask.NameToLayer("Default");
        
        // 충돌체 비활성화
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    protected override void ApplySpecialEffect(BaseEnemy enemy)
    {
        // 철 총알은 기본 데미지가 더 높음 (이미 데미지에 적용됨)
        
        // 오버차징 상태에서는 추가 효과 (필요시 구현)
        if (isOvercharged)
        {
            // 추가적인 효과 적용 (예: 방어력 감소)
            Debug.Log($"오버차징된 철 총알로 적 {enemy.name}의 방어력이 감소됩니다.");
        }
    }
} 