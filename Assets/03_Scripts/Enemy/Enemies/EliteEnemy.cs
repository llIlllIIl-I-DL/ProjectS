using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Enemy.States;

/// <summary>
/// 엘리트 적 - 플레이어 감지, 추격 및 공격, 롤의 알리스타처럼 돌진과 내려찍기 패턴이 있음
/// </summary>
public class EliteEnemy : BaseEnemy
{
    [Header("순찰 설정")]
    [SerializeField] private float patrolDistance; // 순찰 거리
    [SerializeField] private float patrolWaitTime; // 방향 전환 시 대기 시간

    [Header("추격 설정")]
    [SerializeField] private float chaseSpeed; // 추격 속도 // 배수로 잡힘 ex) 1로 설정하면 기본 속도, 2로 설정하면 두 배 속도

    [Header("공격 설정")]
    [SerializeField] private float attackSpeed; // 공격 속도

    [Header("돌진 공격")]
    [SerializeField] private float chargePower; // 공격력
    [SerializeField] private float chargeDistance; // 돌진 거리
    [SerializeField] private float chargeSpeed; // 돌진 속도
    [SerializeField] private float chargeCooldown; // 돌진 쿨타임

    [Header("내려찍기 공격")]
    [SerializeField] private float slamPower; // 공격력
    [SerializeField] private float slamJumpPower; // 점프 힘
    [SerializeField] private float slamDistance; // 내려찍기 거리
    [SerializeField] private float slamSpeed; // 내려찍기 속도
    [SerializeField] private float slamCooldown; // 내려찍기 쿨타임

    // 상태들
    private IdleState idleState;
    private PatrolState patrolState;
    private AttackState attackState;
    private ChaseState chaseState;
    private ChargeAttackState chargeAttackState;
    private SlamAttackState slamAttackState;
    private float chargeCooldownTimer = 0f;
    private bool chargeReady = true;
    private float slamCooldownTimer = 0f;
    private bool slamReady = true;

    public IEnemyState currentState => stateMachine.CurrentState;

    // 상태 접근자 메서드들
    public IdleState GetIdleState() => idleState;
    public AttackState GetAttackState() => attackState;
    public PatrolState GetPatrolState() => patrolState;
    public ChaseState GetChaseState() => chaseState;

    // 순찰 시작점
    private Vector2 startPosition;

    protected override void Awake()
    {
        base.Awake();
        startPosition = transform.position;
    }
    protected override void Update()
    {
        base.Update(); // BaseEnemy의 Update 호출
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // BaseEnemy의 FixedUpdate 호출
    }

    /// <summary>
    /// 적 초기화 및 상태 설정
    /// </summary>
    protected override void InitializeEnemy()
    {
        // 순찰 경로 설정 (시작점 기준 좌우로 순찰)
        Vector2 leftPoint = startPosition - new Vector2(patrolDistance, 0);
        Vector2 rightPoint = startPosition + new Vector2(patrolDistance, 0);

        // 상태 생성 및 초기화
        patrolState = new PatrolState(this, stateMachine, new Vector2[] { leftPoint, rightPoint }, patrolWaitTime); // 웨이 포인트도 같이 설정
        idleState = new IdleState(this, stateMachine, patrolWaitTime);
        attackState = new AttackState(this, stateMachine, attackSpeed);
        chaseState = new ChaseState(this, stateMachine, chaseSpeed, moveInYAxis: false);
        chargeAttackState = new ChargeAttackState(
            this,
            stateMachine,
            chargeSpeed,
            chargeDistance,
            chargePower, // 데미지
            "Charge" // 애니메이션 트리거
        );
        slamAttackState = new SlamAttackState(
            this,
            stateMachine,
            slamSpeed,
            slamDistance,
            3, // 데미지
            "Slam", // 애니메이션 트리거
            slamJumpPower, // 점프 힘
            false // X축으로만 이동
        );

        // 초기 상태 설정
        stateMachine.ChangeState(patrolState);
    }

    /// <summary>
    /// 상태 머신 업데이트
    /// </summary>
    protected override void UpdateAI()
    {
        // 쿨다운 관리
        UpdateCooldowns();
        
        // 공격 조건 확인 및 실행
        CheckAndPerformChargeAttack();
        CheckAndPerformSlamAttack();
        
        stateMachine.Update();
    }

    /// <summary>
    /// 이동 로직은 상태에서 처리
    /// </summary>
    protected override void HandleMovement()
    {
        stateMachine.FixedUpdate();
    }

    /// <summary>
    /// 공격 실행, 공격 범위 내에 있을 때 호출
    /// </summary>
    public override void PerformAttack()
    {
        Debug.Log("기본 공격 실행");
        
        // 플레이어 방향 설정
        Vector2 direction = GetPlayerPosition() - (Vector2)transform.position;
        SetFacingDirection(direction);
        
        // 애니메이션 트리거
        // GetComponent<Animator>()?.SetTrigger("Attack");
        
        // 공격 로직 (데미지 처리 등)
        if (IsInAttackRange())
        {
            // 플레이어에게 데미지
            // ...
        }
    }

    protected override void OnPlayerDetected()
    {
        // 플레이어 감지 시 상태 전환
        if (currentState != chaseState)
        {
            stateMachine.ChangeState(chaseState);
        }
    }

    protected override void OnPlayerLost()
    {

    }

    public override void SwitchToPatrolState()
    {
        stateMachine.ChangeState(patrolState);
    }
    public override void SwitchToAttackState()
    {
        stateMachine.ChangeState(attackState);
    }
    public override void SwitchToChaseState()
    {
        stateMachine.ChangeState(chaseState);
    }
    public override void SwitchToSlamAttackState()
    {
        stateMachine.ChangeState(idleState);
    }

    // 쿨다운 관리 메서드 추가
    private void UpdateCooldowns()
    {
        // 돌진 공격 쿨다운 관리
        if (!chargeReady)
        {
            chargeCooldownTimer += Time.deltaTime;
            if (chargeCooldownTimer >= chargeCooldown)
            {
                chargeReady = true;
                chargeCooldownTimer = 0f;
            }
        }
        
        // 내려찍기 공격 쿨다운 관리
        if (!slamReady)
        {
            slamCooldownTimer += Time.deltaTime;
            if (slamCooldownTimer >= slamCooldown)
            {
                slamReady = true;
                slamCooldownTimer = 0f;
            }
        }
    }

    // 충돌 감지 추가
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 돌진 상태일 때 충돌 처리를 ChargeAttackState에 위임
        if (currentState == chargeAttackState)
        {
            chargeAttackState.OnCollision(collision);
        }
        else if (currentState == slamAttackState)
        {
            // 필요 시 별도 처리
        }
    }

    /// <summary>
    /// 돌진 공격 조건 확인 및 실행
    /// </summary>
    private void CheckAndPerformChargeAttack()
    {
        // 이미 돌진 상태이거나 쿨다운 중이면 무시
        if (currentState == chargeAttackState || !chargeReady)
            return;
        
        // 추격 중일 때만 돌진 판단 (이동 중에 공격 패턴으로 판단)
        if (currentState == chaseState && playerDetected)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, GetPlayerPosition());
            
            // 플레이어가 공격 범위 밖이면서 추격 범위 안에 있고, 일정 거리(예: 추격 범위의 절반) 이상 떨어져 있을 때
            if (!IsInAttackRange() && distanceToPlayer > attackRange && distanceToPlayer <= detectionRange)
            {
                Debug.Log($"추격 범위 내에서 돌진 공격! 거리: {distanceToPlayer}");
                stateMachine.ChangeState(chargeAttackState);
                chargeReady = false;
                chargeCooldownTimer = 0f;
            }
        }
    }

    /// <summary>
    /// 내려찍기 공격 조건 확인 및 실행
    /// </summary>
    private void CheckAndPerformSlamAttack()
    {
        // 이미 내려찍기 상태이거나 쿨다운 중이면 무시
        if (currentState == slamAttackState || !slamReady)
            return;
        
        // 공격 상태일 때만 내려찍기 판단
        if (currentState == attackState && playerDetected)
        {
            // 일정 확률(20%)로 내려찍기 시도
            if (Random.value < 0.2f)
            {
                Debug.Log("내려찍기 공격 시작!");
                stateMachine.ChangeState(slamAttackState);
                slamReady = false;
                slamCooldownTimer = 0f;
            }
        }
    }
}