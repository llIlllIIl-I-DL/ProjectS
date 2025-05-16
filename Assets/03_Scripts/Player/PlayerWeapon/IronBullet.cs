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
    private bool isAttachedToWall = false;
    private Transform attachedWall = null;

    protected override void Start()
    {
        BulletType = ElementType.Iron;
        Damage *= extraDamageMultiplier; // 더 높은 기본 데미지
        
        // 오버차지 상태라면 크기 증가
        if (IsOvercharged)
        {
            transform.localScale *= overchargedScale;
            Damage *= 1.5f; // 오버차지 상태에서 추가 데미지
        }
        
        base.Start();
    }
    
    protected override void Update()
    {
        base.Update();

        if (isAttachedToWall)
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= lifeTime)
            {
                Destroy(gameObject);
            }
            // 벽이 움직이면 따라가게 하고 싶으면:
            if (attachedWall != null)
            {
                transform.position = attachedWall.position;
            }
        }
        else if (hasCollided && IsOvercharged)
        {
            destroyTimer += Time.deltaTime;
            if (destroyTimer >= overchargedLifetime)
            {
                Destroy(gameObject);
            }
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
    }
    
    // 충돌 후 설정 변경
    private void AfterCollisionSetup()
    {
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

    protected override void ApplySpecialEffect(IDebuffable target)
    {
        // IronBullet만의 특수 효과가 있다면 여기에 작성
    }
} 