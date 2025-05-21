using UnityEngine;
using Enemy.States;

/// <summary>
/// 고정형 터렛 - 플레이어 감지 시 발사
/// </summary>
public class EnemySupuerDust : BaseEnemy
{
    #region Variables
    
    [Header("터렛 설정")]
    [SerializeField] private Transform firePoint;         // 발사 지점
    [SerializeField] private GameObject bulletPrefab;     // 총알 프리팹
    [SerializeField] private float fireRate;              // 발사 속도 (초당)
    [SerializeField] private float bulletSpeed;           // 총알 속도
    [SerializeField] private float maxRotationAngle;      // 최대 회전 각도 (제한된 각도만 회전하고 싶을 경우)
    [SerializeField] private bool rotateToPlayer = true;  // 플레이어 방향으로 회전 여부
    
    private AttackState attackState;
    
    // 참조 및 속성
    private Vector2 startPosition;
    private Quaternion initialRotation;
    private Vector3 fixedPosition; // 위치를 강제로 고정하기 위한 변수
    
    private bool canFire = true;
    private float attackTimer = 0f;
    
    private bool isHit = false;
    private float hitAnimationDuration = 0.5f; // Hit 애니메이션 길이에 맞게 조정
    private float hitAnimationTimer = 0f;
    
    #endregion

    #region Properties
    
    // 현재 상태 가져오기 (상태 전환 로직용)
    public IEnemyState currentState => stateMachine.CurrentState;
    
    #endregion

    #region Unity Lifecycle Methods
    
    /// <summary>
    /// 컴포넌트 초기화 및 시작 위치 저장
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        startPosition = transform.position;
        initialRotation = transform.rotation;
        
        // 발사 지점이 할당되지 않았으면 자기 자신으로 설정
        if (firePoint == null)
            firePoint = transform;
    }
    
    /// <summary>
    /// 시작 시 위치 고정값 설정
    /// </summary>
    protected override void Start()
    {
        base.Start();
        fixedPosition = transform.position;
    }
    
    /// <summary>
    /// 프레임 기반 업데이트
    /// </summary>
    protected override void Update()
    {
        base.Update();
        
        // 공격 쿨다운 처리
        if (!canFire)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                canFire = true;
            }
        }

        // Hit 애니메이션 타이머 처리
        if (isHit)
        {
            hitAnimationTimer += Time.deltaTime;
            if (hitAnimationTimer >= hitAnimationDuration)
            {
                isHit = false;
                hitAnimationTimer = 0f;
                animator.ResetTrigger("IsHit");
            }
        }
    }

    /// <summary>
    /// 물리 기반 업데이트 - 위치 고정
    /// </summary>
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        
        // 현재 Idle 상태가 아닐 때만 설정
        if (!animator.GetBool("IsIdle"))
        {
            animator.SetBool("IsIdle", true);
        }
        
        transform.position = fixedPosition;
    }
    
    #endregion

    #region Core Methods
    
    /// <summary>
    /// 적 초기화 및 상태 설정
    /// </summary>
    protected override void InitializeEnemy()
    {
        // 상태 생성
        RegisterState(new IdleState(this, stateMachine));
        RegisterState(new AttackState(this, stateMachine, fireRate));
        RegisterState(new PatrolState(this, stateMachine, new Vector2[] { startPosition }, 0f)); // 고정 위치
        
        // 초기 상태 설정
        SwitchToState<IdleState>();
    }
    
    /// <summary>
    /// 상태 머신 업데이트 및 플레이어 감지에 따른 상태 전환
    /// </summary>
    protected override void UpdateAI()
    {
        stateMachine.Update();
        
        // 플레이어 감지 시 상태 전환
        if (playerDetected && currentState != attackState)
        {
            SwitchToState<AttackState>();
        }
        else if (!playerDetected && currentState == attackState)
        {
            SwitchToState<IdleState>();
        }
        
        // 플레이어를 향해 회전 (제한된 각도 내에서)
        if (rotateToPlayer && playerDetected)
        {
            RotateTowardsPlayer();
        }
    }
    
    /// <summary>
    /// 이동 로직은 터렛에서 필요 없음 (고정 위치)
    /// </summary>
    protected override void HandleMovement()
    {
        // 터렛은 이동하지 않음
        if (rb != null)
        {
            rb.velocity = Vector2.zero; // 속도 강제로 0으로 설정
        }
    }
    
    #endregion

    #region Combat Methods
    
    /// <summary>
    /// 플레이어를 향해 회전 (최대 회전 각도 제한 적용)
    /// </summary>
    private void RotateTowardsPlayer()
    {
        if (playerTransform == null) return;
        
        // 플레이어 방향 구하기
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        Vector2 currentDirection = transform.right; // 현재 바라보는 방향
        
        // 스프라이트 방향 설정
        if (spriteRenderer != null)
        {
            // 플레이어가 왼쪽에 있으면 스프라이트 뒤집기
            spriteRenderer.flipX = direction.x < 0;
        }
    }

    /// <summary>
    /// 공격 실행 - 총알 발사
    /// </summary>
    private void FireBullet()
    {
        if (bulletPrefab == null || playerTransform == null) 
        {
            Debug.LogWarning("총알 프리팹이나 플레이어 참조가 없습니다!");
            return;
        }
        
        // 발사 방향 계산 - 플레이어 직접 조준
        Vector2 directionToPlayer = (playerTransform.position - firePoint.position).normalized;
        
        // 총알 생성 및 발사
        GameObject bullet = ObjectPoolingManager.Instance.GetObject(ObjectPoolingManager.PoolType.EnemyBullet);
        if (bullet != null)
        {
            bullet.transform.position = firePoint.position;
            
            // 총알 회전 설정 (플레이어 방향으로)
            float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // 발사자 설정
            if (bullet.TryGetComponent<Bullet>(out Bullet bulletScript))
            {
                bulletScript.Shooter = gameObject;
            }
            
            // 속도 설정
            if (bullet.TryGetComponent<Rigidbody2D>(out Rigidbody2D bulletRb))
            {
                bulletRb.velocity = directionToPlayer * bulletSpeed;
            }
            
            Debug.DrawRay(firePoint.position, directionToPlayer * 3f, Color.red, 0.5f);
            Debug.Log($"총알 발사: 방향 = {directionToPlayer}, 각도 = {angle}도");
        }
    }

    /// <summary>
    /// 공격 실행 - 총알 발사
    /// </summary>
    public override void PerformAttack()
    {
        if (!canFire) return;
        
        attackTimer = fireRate;
        canFire = false;
        animator.SetTrigger("IsAttack"); // 공격 애니메이션만 트리거
    }
    
    /// <summary>
    /// 피해를 받았을 때 처리
    /// </summary>
    public override void TakeDamage(float damage)
    {
        if (isDestroyed) return;
        
        float finalDamage = Mathf.Max(damage - defence, 1f);
        
        // Hit 애니메이션 처리 - 현재 진행 중인 애니메이션을 중단하지 않음
        if (animator != null && !isHit)
        {
            isHit = true;
            hitAnimationTimer = 0f;
            animator.SetLayerWeight(1, 1f); // Hit 애니메이션용 레이어 가중치 설정
            animator.SetTrigger("IsHit");
        }
        
        base.TakeDamage(finalDamage);

        if (currentHealth <= 0)
        {
            DestroyEntity();
            animator.SetTrigger("IsDead");
        }
    }
    
    #endregion
}