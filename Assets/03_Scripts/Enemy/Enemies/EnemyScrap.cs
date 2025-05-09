using UnityEngine;
using Enemy.States;

/// <summary>
/// 날아다니는 기본 적 클래스
/// </summary>
public class EnemyScrap : BaseEnemy
{
    #region Variables
    
    [Header("비행 설정")]
    [SerializeField] private float hoverAmplitude = 0.5f; // 부유 진폭
    [SerializeField] private float hoverFrequency = 2f; // 부유 주파수

    [Header("추격 설정")]
    [SerializeField] private float chaseSpeed; // 추격 속도 // 배수로 잡힘 ex) 1로 설정하면 기본 속도, 2로 설정하면 두 배 속도

    [Header("공격 설정")]
    [SerializeField] private float attackSpeed; // 공격 속도
    
    [Header("순찰 설정")]
    [SerializeField] private float patrolDistance; // 순찰 거리
    [SerializeField] private float patrolWaitTime; // 방향 전환 시 대기 시간
    
    // 부유 효과를 위한 변수
    private float timeCounter = 0f;
    private float originalY;
    private bool isReturningToHeight = false;
    private float targetY;
    private float heightReturnSpeed = 1.5f;

    // 상태들
    private FlyingPatrolState flyingPatrolState;
    private IdleState idleState;
    private FlyingChaseState flyingChaseState;
    private AttackState attackState;

    // 순찰 시작점
    private Vector2 startPosition;

    // 상태 머신
    public IEnemyState currentState => stateMachine.CurrentState;

    // 상태 접근자 메서드들
    public IdleState GetIdleState() => idleState;
    public AttackState GetAttackState() => attackState;
    public FlyingChaseState GetChaseState() => flyingChaseState;
    
    #endregion

    #region Unity Lifecycle Methods
    
    /// <summary>
    /// 컴포넌트 초기화 및 시작 위치 저장
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        startPosition = transform.position;
        originalY = transform.position.y;
        
        // 날아다니는 적이므로 중력 영향 제거
        rb.gravityScale = 0f;
    }

    /// <summary>
    /// 프레임 기반 업데이트
    /// </summary>
    protected override void Update()
    {
        if (isDestroyed || isStunned) return;
    
        // 높이 복귀 처리
        if (isReturningToHeight)
        {
            HandleHeightReturn();
            // 높이 복귀 중일 때는 부유 효과 적용하지 않음
        }
        // 부유 효과는 복귀 중이 아니고, 추격 또는 공격 상태가 아닐 때만 적용
        else if (currentState != flyingChaseState && currentState != attackState)
        {
            ApplyHoverEffect();
        }

        // 플레이어 감지
        DetectPlayer();
    
        // 상태 머신 업데이트 (좌우 움직임)
        stateMachine.Update();
    }

    /// <summary>
    /// 물리 기반 업데이트
    /// </summary>
    protected override void FixedUpdate()
    {
        if (isDestroyed || isStunned) return;
    
        // 상태 머신의 물리 업데이트 호출
        stateMachine.FixedUpdate();
    }
    
    protected override void Start()
    {
        base.Start(); // 부모의 Start 호출
        InitializeEnemy(); // 이 메서드 호출이 Start에서 이루어져야 함
    }
    #endregion

    #region Core Methods
    
    /// <summary>
    /// 적 초기화 및 상태 설정
    /// </summary>
    protected override void InitializeEnemy()
    {
        flyingPatrolState = new FlyingPatrolState(this, stateMachine, patrolDistance, patrolWaitTime);
        flyingChaseState = new FlyingChaseState(this, stateMachine, chaseSpeed);
        attackState = new AttackState(this, stateMachine, attackSpeed);

        
        // 상태 머신 초기화
        stateMachine.ChangeState(flyingPatrolState);
        
        // 나머지 초기화...
    }

    /// <summary>
    /// 상태 머신 업데이트
    /// </summary>
    protected override void UpdateAI()
    {
        stateMachine.Update();
    }

    /// <summary>
    /// 이동 로직은 상태에서 처리
    /// </summary>
    protected override void HandleMovement()
    {

    }

    /// <summary>
    /// 공격 로직은 상태에서 처리
    /// </summary>
    public override void PerformAttack()
    {
        // 플레이어가 있고 공격 범위 내에 있는지 확인
        if (playerTransform != null && IsInAttackRange())
        {
            // 플레이어에게 데미지 주기
            IDamageable playerDamageable = playerTransform.GetComponent<IDamageable>();
            if (playerDamageable != null)
            {
                playerDamageable.TakeDamage(attackPower);
                Debug.Log($"{gameObject.name}이(가) 플레이어에게 {attackPower} 데미지를 입혔습니다.");

                // 공격 이펙트 등 추가 요소 구현 가능
            }
        }
    }
    
    #endregion

    #region Player Detection
    /// <summary>
    /// 플레이어 감지 시 호출됨
    /// </summary>
    protected override void OnPlayerDetected()
    {
        // 플레이어 감지 시 추격 상태로 전환
        if (currentState != flyingChaseState && currentState != attackState)
        {
            stateMachine.ChangeState(flyingChaseState);
        }
    }

    /// <summary>
    /// 플레이어를 놓쳤을 때 호출됨
    /// </summary>
    protected override void OnPlayerLost()
    {
        // 단순 패트롤 적은 플레이어를 놓치는 것에 반응하지 않음
    }
    
     #region State Switch Methods
    
    /// <summary>
    /// 공격 상태로 전환
    /// </summary>
    public override void SwitchToAttackState()
    {
        stateMachine.ChangeState(attackState);
    }
    /// <summary>
    /// 순찰 상태로 전환
    /// </summary>
    public override void SwitchToPatrolState()
    {
        stateMachine.ChangeState(flyingPatrolState);
    }

    /// <summary>
    /// 추격 상태로 전환
    /// </summary>
    public override void SwitchToChaseState()
    {
        stateMachine.ChangeState(flyingChaseState);
    }

    #endregion

    #endregion

    #region Flying Mechanics
    
    /// <summary>
    /// 지정된 높이로 자연스럽게 복귀
    /// </summary>
    private void HandleHeightReturn()
    {
        Vector3 currentPos = transform.position;
        float newY = Mathf.MoveTowards(currentPos.y, targetY, heightReturnSpeed * Time.deltaTime);
        transform.position = new Vector3(currentPos.x, newY, currentPos.z);

        // 목표 높이에 충분히 가까워지면 완료로 표시
        if (Mathf.Abs(newY - targetY) < 0.05f)
        {
            isReturningToHeight = false;
            originalY = targetY; // 원래 높이 업데이트
            timeCounter = 0f; // 부유 효과 타이머 리셋
        }
    }

    /// <summary>
    /// 지정된 높이로 돌아가기 시작
    /// </summary>
    public void ReturnToHeight(float height)
    {
        targetY = height;
        isReturningToHeight = true;
    }

    /// <summary>
    /// 부유 효과 적용 (상하로 부드럽게 움직임)
    /// </summary>
    private void ApplyHoverEffect()
    {
        if (isDestroyed || isStunned) return;
        
        timeCounter += Time.deltaTime;
        float hoverOffset = Mathf.Sin(timeCounter * hoverFrequency) * hoverAmplitude;

        Vector3 currentPos = transform.position;
        float newY = originalY + hoverOffset;
        
        transform.position = new Vector3(currentPos.x, newY, currentPos.z);
    }
    
    /// <summary>
    /// 현재 Y 위치를 원래 Y 위치로 설정
    /// </summary>
    public void SetCurrentPositionAsOriginalY()
    {
        originalY = transform.position.y;
        timeCounter = 0f; // 부유 효과 타이머 리셋
    }

    #endregion
}
