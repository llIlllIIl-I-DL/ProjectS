using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 적 캐릭터의 기본 클래스
/// </summary>
public abstract class BaseEnemy : MonoBehaviour, Idamageable
{
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

    protected EnemyStateMachine stateMachine; // 상태 머신
    
    
    // 컴포넌트 캐싱 및 초기화
    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        stateMachine = new EnemyStateMachine();
    }
    
    protected virtual void Start()
    {
        // 플레이어 참조 찾기
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        InitializeEnemy();
    }
    
    protected virtual void Update()
    {
        if (isDead || isStunned) return;
        
        // 플레이어 감지
        DetectPlayer();
        
        // AI 업데이트
        UpdateAI();
    }
    
    protected virtual void FixedUpdate()
    {
        if (isDead || isStunned) return;
        
        // 물리 기반 이동
        HandleMovement();
    }
    
    #region Core Functions
    
    /// <summary>
    /// 적 초기화 로직
    /// </summary>
    protected abstract void InitializeEnemy();
    
    /// <summary>
    /// AI 로직 업데이트 (상태 머신에서 관리)
    /// </summary>
    protected abstract void UpdateAI();
    
    /// <summary>
    /// 이동 로직 처리
    /// </summary>
    protected abstract void HandleMovement();
    
    /// <summary>
    /// 공격 실행
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
    /// 플레이어가 감지되었을 때 호출
    /// </summary>
    protected virtual void OnPlayerDetected()
    {
        // 플레이어 감지 반응 (자식 클래스에서 오버라이드)
    }
    
    /// <summary>
    /// 플레이어를 놓쳤을 때 호출
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
    
    #region Gizmos
    
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

    #region Getters and Setters
    public bool IsPlayerDetected() 
    {
        return playerDetected;
    }

    public Vector2 GetLastKnownPlayerPosition() => lastKnownPlayerPosition;

    public Vector2 GetPlayerPosition() => playerTransform != null ? playerTransform.position : transform.position;

    public bool IsInAttackRange() 
    {
        if (playerTransform == null) return false;
        
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        bool inRange = distance <= attackRange;        
        return inRange;
    }

    public void MoveInDirection(Vector2 direction, float speedMultiplier = 1f)
    {
        if (isDead || isStunned) return; // 사망 또는 기절 상태일 때 이동하지 않음
        
        rb.velocity = direction * moveSpeed * speedMultiplier; // 이동 속도 적용
    }

    public void StopMoving()
    {
        if (rb != null) // Rigidbody2D가 null이 아닐 때만 정지
        rb.velocity = Vector2.zero; // 속도 강제로 0으로 설정
    }

    public virtual void SwitchToIdleState()
    {
        // 기본 구현은 비어있음 - 자식 클래스에서 구현
    }

    public virtual void SwitchToPatrolState()
    {
        // 기본 구현은 비어있음 - 자식 클래스에서 구현
    }
    public virtual void SwitchToChaseState()
    {
        // 기본 구현은 비어있음 - 자식 클래스에서 구현
    }

    public virtual void SwitchToAttackState()
    {
        // 기본 구현은 비어있음 - 자식 클래스에서 구현
    }
    public virtual void SwitchToChargeAttackState()
    {
        // 기본 구현은 비어있음 - 자식 클래스에서 구현
    }
    public virtual void SwitchToSlamAttackState()
    {
        // 기본 구현은 비어있음 - 자식 클래스에서 구현
    }
    #endregion
}