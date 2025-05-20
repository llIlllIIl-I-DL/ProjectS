using System.Collections;
using UnityEngine;
using Enemy.States;

/// <summary>
/// 엘리트 적 - 플레이어 감지, 추격 및 공격, 롤의 알리스타처럼 돌진과 내려찍기 패턴이 있음
/// </summary>
public class EnemyBigDust : BaseEnemy
{
    #region Variables

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

    private ChargeAttackState chargeAttackState;
    private SlamAttackState slamAttackState;

    // 쿨다운 관리
    private float chargeCooldownTimer = 0f;
    private bool chargeReady = true;
    private float slamCooldownTimer = 0f;
    private bool slamReady = true;

    // 참조 및 속성
    private Vector2 startPosition; // 순찰 시작점

    #endregion

    #region Properties

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
    }

    /// <summary>
    /// 프레임 기반 업데이트
    /// </summary>
    protected override void Update()
    {
        base.Update(); // BaseEnemy의 Update 호출
    }

    /// <summary>
    /// 물리 기반 업데이트
    /// </summary>
    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // BaseEnemy의 FixedUpdate 호출
    }

    /// <summary>
    /// 충돌 감지 처리
    /// </summary>
    protected override void OnCollisionStay2D(Collision2D collision)
    {
        // 상태별 충돌 처리 위임
        if (currentState == chargeAttackState)
        {
            chargeAttackState.OnCollision(collision);
        }
        else if (currentState == slamAttackState)
        {
            // 내려찍기 충돌 처리 (필요시 구현)
        }

        // 기본 충돌 처리 (데미지 등)
        base.OnCollisionStay2D(collision);
    }

    /// <summary>
    /// 데미지 처리
    /// </summary>
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
    }

    #endregion

    #region Core Methods

    /// <summary>
    /// 적 초기화 및 상태 설정
    /// </summary>
    protected override void InitializeEnemy()
    {
        // 순찰 경로 설정
        Vector2 leftPoint = startPosition - new Vector2(patrolDistance, 0);
        Vector2 rightPoint = startPosition + new Vector2(patrolDistance, 0);

        // 상태 생성 및 등록 - 변수 선언과 동시에 등록
        RegisterState(new PatrolState(this, stateMachine, new Vector2[] { leftPoint, rightPoint }, patrolWaitTime));
        RegisterState(new AttackState(this, stateMachine, attackSpeed));
        RegisterState(new ChaseState(this, stateMachine, chaseSpeed, moveInYAxis: false));
        RegisterState(new ChargeAttackState(this, stateMachine, chargeSpeed, chargeDistance, chargePower, "Charge"));
        RegisterState(new SlamAttackState(this, stateMachine, slamSpeed, slamDistance, 3, "Slam", slamJumpPower, false));

        // 초기 상태 설정
        SwitchToState<PatrolState>();
    }

    /// <summary>
    /// AI 업데이트 - 상태 머신 및 공격 패턴 관리
    /// </summary>
    protected override void UpdateAI()
    {
        // 쿨다운 관리
        UpdateCooldowns();

        // 공격 조건 확인 및 실행
        CheckAndPerformChargeAttack();
        CheckAndPerformSlamAttack();

        // 상태 머신 업데이트
        stateMachine.Update();
    }

    /// <summary>
    /// 이동 로직 처리 (상태에서 관리)
    /// </summary>
    protected override void HandleMovement()
    {
        stateMachine.FixedUpdate();
    }

    /// <summary>
    /// 공격 실행 (공격 범위 내에서 호출됨)
    /// </summary>
    public override void PerformAttack()
    {
        Debug.Log("기본 공격 실행");

        // 플레이어 방향 설정
        Vector2 direction = PlayerPosition - (Vector2)transform.position;
        SetFacingDirection(direction);

        // 애니메이션 트리거
        // GetComponent<Animator>()?.SetTrigger("Attack");

        // 공격 로직 - 범위 내 플레이어에게 데미지
        if (IsInAttackRange())
        {
            // 플레이어에게 데미지 (구현 필요)
        }
    }

    /// <summary>
    /// 넉백 처리
    /// </summary>
    public override void ApplyKnockback(Vector2 direction, float force)
    {
        base.ApplyKnockback(direction, 1f);
        StartCoroutine(KnockbackCoroutine(0.5f));
    }

    /// <summary>
    /// 넉백 후 이동 재개 코루틴
    /// </summary>
    private IEnumerator KnockbackCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (playerDetected)
        {
            SwitchToState<ChaseState>();
        }
        else
        {
            SwitchToState<PatrolState>();
        }
        Debug.Log($"{gameObject.name}이(가) 넉백 후 이동을 재개합니다.");
    }

    #endregion

    #region Player Detection

    /// <summary>
    /// 플레이어 감지 시 호출됨
    /// </summary>
    protected override void OnPlayerDetected()
    {
        // 플레이어 감지 시 추격 상태로 전환 - 타입 기반으로 간단하게 전환
        var currentState = stateMachine.CurrentState;
        if (currentState != GetState<ChaseState>() && currentState != GetState<AttackState>())
        {
            SwitchToState<ChaseState>();
        }
    }

    /// <summary>
    /// 플레이어를 놓쳤을 때 호출됨
    /// </summary>
    protected override void OnPlayerLost()
    {
        // 플레이어 놓침 처리 (필요시 구현)
    }

    #endregion

    #region Attack Pattern Methods

    /// <summary>
    /// 쿨다운 관리
    /// </summary>
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

    /// <summary>
    /// 돌진 공격 조건 확인 및 실행
    /// </summary>
    private void CheckAndPerformChargeAttack()
    {
        // 이미 돌진 상태이거나 쿨다운 중이면 무시
        if (stateMachine.CurrentState == GetState<ChargeAttackState>() || !chargeReady)
            return;

        // 추격 중일 때만 돌진 판단
        if (stateMachine.CurrentState == GetState<ChaseState>() && playerDetected)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, PlayerPosition);

            // 플레이어가 공격 범위 밖이면서 추격 범위 안에 있을 때
            if (!IsInAttackRange() && distanceToPlayer > attackRange && distanceToPlayer <= detectionRange)
            {
                SwitchToState<ChargeAttackState>();
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
        if (stateMachine.CurrentState == GetState<SlamAttackState>() || !slamReady)
            return;

        // 공격 상태일 때만 내려찍기 판단
        if (stateMachine.CurrentState == GetState<AttackState>() && playerDetected)
        {
            // 일정 확률(20%)로 내려찍기 시도
            if (Random.value < 0.2f)
            {
                Debug.Log("내려찍기 공격 시작!");
                SwitchToState<SlamAttackState>();
                slamReady = false;
                slamCooldownTimer = 0f;
            }
        }
    }

    #endregion
}