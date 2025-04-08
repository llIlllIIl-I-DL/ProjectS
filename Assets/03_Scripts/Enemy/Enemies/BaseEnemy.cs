using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 적 캐릭터의 기본 클래스
/// </summary>
public abstract class BaseEnemy : MonoBehaviour, IDamageable
{
        #region Variables

    [Header("기본 스탯")]
    [SerializeField] protected float maxHealth; // 최대 체력
    [SerializeField] protected float currentHealth; // 현재 체력
    [SerializeField] protected float moveSpeed; // 이동 속도
    [SerializeField] protected float attackPower; // 공격력
    [SerializeField] protected float attackRange; // 공격 범위
    [SerializeField] protected float detectionRange; // 감지 범위

    [Header("드롭 아이템")]
    [SerializeField] protected GameObject[] possibleDrops; // 드롭 가능한 아이템들
    [SerializeField] protected float dropChance = 0.3f; // 드롭 확률
    
    [Header("충돌 데미지")]
    [SerializeField] protected float contactDamage = 1f; // 충돌 데미지 값
    [SerializeField] protected bool dealsDamageOnContact = true; // 충돌 데미지 적용 여부

    // 상태 관련
    protected bool isDead = false; // 사망 여부
    protected bool isStunned = false; // 기절 여부
    protected Transform playerTransform; 
    protected Rigidbody2D rb;
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;
    
    // 플레이어 감지
    protected bool playerDetected = false;
    protected Vector2 lastKnownPlayerPosition; // 마지막으로 감지된 플레이어 위치

    // 상태 관리
    protected EnemyStateMachine stateMachine; // 상태 머신
    
    #endregion

    #region Unity Lifecycle Methods
    
    /// <summary>
    /// 컴포넌트 캐싱 및 초기화
    /// </summary>
    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        stateMachine = new EnemyStateMachine();
    }
    
    /// <summary>
    /// 시작 시 초기화
    /// </summary>
    protected virtual void Start()
    {
        // 플레이어 참조 찾기
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        InitializeEnemy();
    }
    
    /// <summary>
    /// 프레임 단위 업데이트
    /// </summary>
    protected virtual void Update()
    {
        if (isDead || isStunned) return;
        
        // 플레이어 감지
        DetectPlayer();
        
        // AI 업데이트
        UpdateAI();
    }
    
    /// <summary>
    /// 물리 업데이트
    /// </summary>
    protected virtual void FixedUpdate()
    {
        if (isDead || isStunned) return;
        
        // 물리 기반 이동
        HandleMovement();
    }
    
    /// <summary>
    /// 충돌 처리
    /// </summary>
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (!dealsDamageOnContact) return; // 충돌 데미지가 비활성화된 경우 무시
        
        // 플레이어와 충돌했는지 확인
        if (collision.gameObject.CompareTag("Player"))
        {
            // 플레이어에게 데미지 주기
            IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(contactDamage);
                Debug.Log($"{gameObject.name}이(가) 플레이어에게 {contactDamage} 충돌 데미지를 입혔습니다.");
            }
        }
    }
    
    /// <summary>
    /// 기즈모 그리기 (에디터 전용)
    /// </summary>
    private void OnDrawGizmos()
    {
        // 감지 범위 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // 공격 범위 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    
    #endregion

    #region Core Functions
    
    /// <summary>
    /// 적 초기화 로직 - 상속받은 클래스에서 구현
    /// </summary>
    protected abstract void InitializeEnemy();
    
    /// <summary>
    /// AI 로직 업데이트 (상태 머신에서 관리) - 상속받은 클래스에서 구현
    /// </summary>
    protected abstract void UpdateAI();
    
    /// <summary>
    /// 이동 로직 처리 - 상속받은 클래스에서 구현
    /// </summary>
    protected abstract void HandleMovement();
    
    /// <summary>
    /// 공격 실행 - 상속받은 클래스에서 구현
    /// </summary>
    public abstract void PerformAttack();
    
    /// <summary>
    /// 피해를 입음
    /// </summary>
    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        
        // 피격 효과
        StartCoroutine(FlashEffect());
        
        // 사망 체크
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 사망 처리
    /// </summary>
    protected virtual void Die()
    {
        isDead = true;
        
        // 애니메이션
        animator?.SetTrigger("Die");
        
        // 콜라이더 비활성화
        GetComponent<Collider2D>().enabled = false;
        
        // 아이템 드롭
        DropItem();
        
        // 지연 파괴
        Destroy(gameObject, 2f);
    }
    
    #endregion
    
    #region Utility Functions
    
    /// <summary>
    /// 피격 효과
    /// </summary>
    protected virtual IEnumerator FlashEffect()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
    
    /// <summary>
    /// 플레이어 감지
    /// </summary>
    protected virtual void DetectPlayer()
    {
        if (playerTransform == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        bool wasDetected = playerDetected;
        
        // 감지 범위 내에 있는지 확인
        playerDetected = distanceToPlayer <= detectionRange;
        
        // 감지 상태가 변경되었을 때
        if (playerDetected != wasDetected)
        {
            if (playerDetected)
            {
                OnPlayerDetected();
            }
            else
            {
                OnPlayerLost();
            }
        }
        
        // 플레이어가 감지되면 위치 업데이트
        if (playerDetected)
        {
            lastKnownPlayerPosition = playerTransform.position;
        }
    }
    
    /// <summary>
    /// 플레이어가 감지되었을 때 호출 - 상속받은 클래스에서 오버라이드
    /// </summary>
    protected virtual void OnPlayerDetected()
    {
        // 플레이어 감지 반응 (자식 클래스에서 오버라이드)
    }
    
    /// <summary>
    /// 플레이어를 놓쳤을 때 호출 - 상속받은 클래스에서 오버라이드
    /// </summary>
    protected virtual void OnPlayerLost()
    {
        // 플레이어를 놓친 반응 (자식 클래스에서 오버라이드)
    }
    
    /// <summary>
    /// 아이템 드롭
    /// </summary>
    protected virtual void DropItem()
    {
        if (possibleDrops.Length == 0) return;
        
        if (Random.value <= dropChance)
        {
            int dropIndex = Random.Range(0, possibleDrops.Length);
            Instantiate(possibleDrops[dropIndex], transform.position, Quaternion.identity);
        }
    }
    
    /// <summary>
    /// 바라보는 방향 설정
    /// </summary>
    public virtual void SetFacingDirection(Vector2 direction)
    {
        if (spriteRenderer != null && direction.x != 0)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }
    
    #endregion
    
    #region Movement and Actions
    
    /// <summary>
    /// 지정된 방향으로 이동
    /// </summary>
    public void MoveInDirection(Vector2 direction, float speedMultiplier = 1f)
    {
        if (isDead || isStunned) return; // 사망 또는 기절 상태일 때 이동하지 않음
        
        rb.velocity = direction * moveSpeed * speedMultiplier; // 이동 속도 적용
    }

    /// <summary>
    /// 이동 정지
    /// </summary>
    public void StopMoving()
    {
        if (rb != null) // Rigidbody2D가 null이 아닐 때만 정지
            rb.velocity = Vector2.zero; // 속도 강제로 0으로 설정
    }
    
    #endregion

    #region State Switch Methods
    
    /// <summary>
    /// 대기 상태로 전환 - 상속받은 클래스에서 구현
    /// </summary>
    public virtual void SwitchToIdleState()
    {
        // 기본 구현은 비어있음 - 자식 클래스에서 구현
    }

    /// <summary>
    /// 순찰 상태로 전환 - 상속받은 클래스에서 구현
    /// </summary>
    public virtual void SwitchToPatrolState()
    {
        // 기본 구현은 비어있음 - 자식 클래스에서 구현
    }
    
    /// <summary>
    /// 추격 상태로 전환 - 상속받은 클래스에서 구현
    /// </summary>
    public virtual void SwitchToChaseState()
    {
        // 기본 구현은 비어있음 - 자식 클래스에서 구현
    }

    /// <summary>
    /// 공격 상태로 전환 - 상속받은 클래스에서 구현
    /// </summary>
    public virtual void SwitchToAttackState()
    {
        // 기본 구현은 비어있음 - 자식 클래스에서 구현
    }
    
    /// <summary>
    /// 돌진 공격 상태로 전환 - 상속받은 클래스에서 구현
    /// </summary>
    public virtual void SwitchToChargeAttackState()
    {
        // 기본 구현은 비어있음 - 자식 클래스에서 구현
    }
    
    /// <summary>
    /// 내려찍기 공격 상태로 전환 - 상속받은 클래스에서 구현
    /// </summary>
    public virtual void SwitchToSlamAttackState()
    {
        // 기본 구현은 비어있음 - 자식 클래스에서 구현
    }
    
    #endregion

    #region Getters and Setters
    
    /// <summary>
    /// 플레이어 감지 여부 반환
    /// </summary>
    public bool IsPlayerDetected() 
    {
        return playerDetected;
    }

    /// <summary>
    /// 마지막으로 알려진 플레이어 위치 반환
    /// </summary>
    public Vector2 GetLastKnownPlayerPosition() => lastKnownPlayerPosition;

    /// <summary>
    /// 현재 플레이어 위치 반환
    /// </summary>
    public Vector2 GetPlayerPosition() => playerTransform != null ? playerTransform.position : transform.position;

    /// <summary>
    /// 플레이어가 공격 범위 내에 있는지 확인
    /// </summary>
    public bool IsInAttackRange() 
    {
        if (playerTransform == null) return false;
        
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        return distance <= attackRange;
    }
    
    #endregion
}