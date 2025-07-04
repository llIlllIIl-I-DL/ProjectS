using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 모든 적 캐릭터의 기본 클래스
/// </summary>
public abstract class BaseEnemy : DestructibleEntity, IDebuffable
{
    #region Variables

    [Header("기본 스탯")]
    [SerializeField] protected float moveSpeed; // 이동 속도
    [SerializeField] protected float attackPower; // 공격력
    [SerializeField] protected float defence; // 방어력
    [SerializeField] protected float attackRange; // 공격 범위
    [SerializeField] protected float detectionRange; // 감지 범위
    
    [Header("충돌 데미지")]
    [SerializeField] protected float contactDamage = 1f; // 충돌 데미지 값
    [SerializeField] protected bool dealsDamageOnContact = true; // 충돌 데미지 적용 여부
    [SerializeField] protected float damageInterval = 1.5f; // 데미지 주기
    private float lastDamageTime = -999f;

    [Header("이펙트 설정")]

    [SerializeField] protected GameObject destructionEffect; // 파괴 효과
    [SerializeField] protected GameObject hitEffect; // 피격 효과

    // Enemy 고유 상태 관련
    protected bool isStunned = false; // 기절 여부
    protected Transform playerTransform; 
    protected Animator animator;
    public Animator Animator => animator;
    
    // 플레이어 감지
    protected bool playerDetected = false;
    protected Vector2 lastKnownPlayerPosition; // 마지막으로 감지된 플레이어 위치

    // 상태 관리
    protected EnemyStateMachine stateMachine; // 상태 머신

    private string poolKey; // 어느 풀에 속하는지 식별
    private EnemyManager manager;
    
    #endregion

    #region State Management

    // 모든 상태를 저장할 딕셔너리 - 타입을 키로 사용
    protected Dictionary<Type, IEnemyState> statesByType = new Dictionary<Type, IEnemyState>();

    /// <summary>
    /// 상태 등록 - 제네릭 메서드로 타입 추론 활용
    /// </summary>
    protected T RegisterState<T>(T state) where T : IEnemyState
    {
        Type stateType = typeof(T);
        if (statesByType.ContainsKey(stateType))
        {
            statesByType[stateType] = state;
        }
        else
        {
            statesByType.Add(stateType, state);
        }
        return state;
    }

    /// <summary>
    /// 상태 가져오기 - 제네릭 메서드
    /// </summary>
    public T GetState<T>() where T : class, IEnemyState
    {
        Type stateType = typeof(T);
        if (statesByType.TryGetValue(stateType, out IEnemyState state) && state is T typedState)
        {
            return typedState;
        }
        return null;
    }

    /// <summary>
    /// 상태 전환 - 제네릭 메서드
    /// </summary>
    public void SwitchToState<T>() where T : class, IEnemyState
    {
        T state = GetState<T>();
        if (state != null)
        {
            stateMachine.ChangeState(state);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}에 등록되지 않은 상태({typeof(T).Name})로 전환을 시도했습니다.");
        }
    }

    #endregion

    #region Unity Lifecycle Methods
    
    /// <summary>
    /// 컴포넌트 캐싱 및 초기화
    /// </summary>
    protected override void Awake()
    {
        base.Awake(); // 부모 클래스 초기화 추가
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        stateMachine = new EnemyStateMachine();
        
        // spriteRenderer가 부모 클래스에서 초기화되지 않았을 경우를 대비
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
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
        if (isDestroyed || isStunned) return;
        
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
        if (isDestroyed || isStunned) return;
        
        // 물리 기반 이동
        HandleMovement();
    }
    
    /// <summary>
    /// 충돌 처리
    /// </summary>
    protected virtual void OnCollisionStay2D(Collision2D collision)
    {
        if (!dealsDamageOnContact) return; // 충돌 데미지가 비활성화된 경우 무시
        if (isDestroyed) return; // 이미 파괴된 경우 무시
        
        // 플레이어와 충돌했는지 확인
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time - lastDamageTime < damageInterval) return; // 데미지 주기 체크
            lastDamageTime = Time.time;
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
    /// 사망 처리
    /// </summary>
    // protected override void Destroy()
    // {
    //     if (isDestroyed) return; // 이미 파괴된 경우 무시
        
    //     isDestroyed = true;
    //     StopMoving(); // 이동 정지
    //     PlayDestructionEffect(); // 파괴 효과 재생
    // }
    // BaseEnemy 클래스에서 구현으로 일단 주석처리
    
    #endregion
    
    #region Utility Functions

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
    public override void DropItem()
    {
        
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
        if (isDestroyed || isStunned) return; // 사망 또는 기절 상태일 때 이동하지 않음
        
        rb.velocity = direction * moveSpeed * speedMultiplier; // 이동 속도 적용
    }

    /// <summary>
    /// 이동 정지
    /// </summary>
    public void StopMoving()
    {
        rb.velocity = Vector2.zero; // 속도 초기화
    }

    /// <summary>
    /// 넉백 적용
    /// </summary>
    public virtual void ApplyKnockback(Vector2 direction, float force)
    {
        if (rb == null) return; // Rigidbody2D가 없으면 무시
        StopMoving(); // 이동 정지
        if (stateMachine != null)
            stateMachine.ChangeState(null); // 상태 머신 초기화
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse); // 넉백 힘 적용
        // 다시 스테이트 머신으로 돌아가야 함
        // 이부분은 자식클래스에서 오버라이드해서 타야할 상태머신에 맞게 변경

        Debug.Log($"{gameObject.name}이(가) 넉백을 받았습니다. 방향: {direction}, 힘: {force}");
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
    public Vector2 LastKnownPlayerPosition => lastKnownPlayerPosition;

    /// <summary>
    /// 현재 플레이어 위치 반환
    /// </summary>
    public Vector2 PlayerPosition => playerTransform != null ? playerTransform.position : transform.position;

    /// <summary>
    /// 플레이어가 공격 범위 내에 있는지 확인
    /// </summary>
    public virtual bool IsInAttackRange() 
    {
        if (playerTransform == null) return false;
        
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        return distance <= attackRange;
    }

    /// <summary>
    /// 에너미 상태이상 여부 확인
    /// 
    public float AttackPower {get => attackPower; set => attackPower = value;}

    public float Defence {get => defence; set => defence = value;}

    public float MoveSpeed {get => moveSpeed; set => moveSpeed = value;}

    #endregion

    #region Effects

    /// <summary>
    /// 파괴 효과 재생
    /// </summary>
    public override void PlayDestructionEffect()
    {
        if (destructionEffect != null)
        {
            GameObject effect = Instantiate(destructionEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f); // 1초 후에 효과 삭제
        }
    }

    public override void PlayHitEffect(Vector2 hitpoint = default)
    {
        if (hitpoint == default)
            hitpoint = transform.position;
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, hitpoint, Quaternion.identity);
            Destroy(effect, 1f); // 1초 후에 효과 삭제
        }
    }

    #endregion

    public void Initialize(string key, EnemyManager mgr)
    {
        poolKey = key;
        manager = mgr;
    }

    // 풀로 돌아가기
    public void ReturnToPool()
    {
        if (manager != null)
            manager.ReturnToPool(this, poolKey);
            Debug.Log($"{gameObject.name}이(가) 풀로 {poolKey} 반환");
    }

    // 스폰될 때 호출
    public virtual void OnSpawned()
    {
        // 체력 리셋, 상태 리셋 등
        currentHealth = maxHealth;
        isDestroyed = false;
        GetComponent<Collider2D>().enabled = true; // 콜라이더 활성화
        // 기타 초기화 로직
    }

    // 죽었을 때
    protected virtual void Die()
    {
        Invoke("ReturnToPool", 1f);
        // 죽음 애니메이션, 효과음 등
        // 상태이상 초기화
        DebuffManager.Instance.RemoveAllDebuffs(this);
        isDestroyed = true;
        PlayDestructionEffect();
        DropItem();
        // 콜라이더 비활성화
        GetComponent<Collider2D>().enabled = false;
        Debug.Log($"{gameObject.name} Detroy가 아니라 Die로 호출 됨");
    }
    /// <summary>
    /// 방어력을 고려한 데미지 처리
    /// </summary>
    public override void TakeDamage(float damage)
    {
        if (isDestroyed) return;
        
        // 방어력 계산
        float finalDamage = Mathf.Max(damage - defence, 1f);
        
        // 애니메이션 트리거를 먼저 설정
        if (animator != null)
        {
            animator.SetTrigger("IsHit");
        }
        
        // 부모 클래스의 TakeDamage 호출
        base.TakeDamage(finalDamage);

        if (currentHealth <= 0)
        {
            DestroyEntity();
            animator.SetTrigger("IsDead");
        }
        
        Debug.Log($"{gameObject.name}이(가) {damage} 데미지를 받았고, 방어력 {defence}로 인해 {finalDamage} 데미지만큼만 피해를 입었습니다.");
    }

    // IDebuffable 구현
    public float CurrentHP
    {
        get => currentHealth;
        set => currentHealth = Mathf.Clamp(value, 0, maxHealth);
    }
}