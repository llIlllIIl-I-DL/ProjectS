using UnityEngine;

// 철 총알
public class IronBullet : Bullet
{
    [SerializeField] private float extraDamageMultiplier = 1.5f;
    
    // 오버차지 관련 설정
    [Header("오버차지 설정")]
    [SerializeField] private float overchargedLifetime = 6f; // 오버차지 상태에서 충돌 후 잔존 시간
    [SerializeField] private float overchargedScale = 1.2f; // 오버차지 상태에서 크기 배율
    
    private bool hasCollided = false; // 충돌 발생 여부
    private float destroyTimer = 0f; // 파괴 타이머
    private bool isAttachedToWall = false;
    private Transform attachedWall = null;

    protected override void Start()
    {
        BulletType = ElementType.Iron;
        Damage *= extraDamageMultiplier; // 더 높은 기본 데미지
        
        // 오버차지 상태라면 크기 증가 및 특별한 설정
        if (IsOvercharged)
        {
            transform.localScale *= overchargedScale;
            Damage *= 1.5f; // 오버차지 상태에서 추가 데미지
            lifeTime = overchargedLifetime; // 오버차지 상태에서 라이프타임 증가
            
            // 오버차지 상태에서는 IsTrigger 비활성화
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = false;
            }
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
                ObjectPoolingManager.Instance.ReturnBullet(gameObject, BulletType);
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
                ObjectPoolingManager.Instance.ReturnBullet(gameObject, BulletType);
            }
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOvercharged)
        {
            base.OnTriggerEnter2D(other);
        }
        else
        {
            // 오버차지 상태에서는 충돌해도 파괴되지 않고 계속 진행
            AfterCollisionSetup();
        }
    }

    protected void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsOvercharged)
        {
            // 오버차지 상태에서는 충돌해도 파괴되지 않고 계속 진행
            AfterCollisionSetup();
        }
    }
    
    // 충돌 후 설정 변경
    private void AfterCollisionSetup()
    {
        
        // 충돌 효과 (파티클 등)
        // 필요시 파티클 시스템 추가 구현
        
        // 오브젝트의 레이어 변경하여 다른 충돌 방지
        gameObject.layer = LayerMask.NameToLayer("Ground");
        
        // 충돌체 비활성화
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = false;
        }
    }

    protected override void ApplySpecialEffect(IDebuffable target)
    {
        // IronBullet만의 특수 효과가 있다면 여기에 작성
    }
} 