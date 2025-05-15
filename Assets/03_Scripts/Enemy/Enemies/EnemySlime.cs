using System.Collections;
using UnityEngine;
using Enemy.States;

/// <summary>
/// 순찰, 추적을 하는 슬라임 적 클래스 
/// 약간 통통튀듯이 움직이고움직인 자리에 점액을 남김, 점액은 지속시간 후에 사라지고 플레이어가 닿으면 데미지를 받음
/// </summary>
public class EnemySlime : BaseEnemy
{
    #region Variables

    [Header("순찰 설정")]
    [SerializeField] private float patrolDistance; // 순찰 거리
    [SerializeField] private float patrolWaitTime; // 방향 전환 시 대기 시간

    [Header("추격 설정")]
    [SerializeField] private float chaseSpeed; // 추격 속도 // 배수로 잡힘 ex) 1로 설정하면 기본 속도, 2로 설정하면 두 배 속도

    [Header("점프 설정")]
    [SerializeField] private float jumpPower;
    [SerializeField] private float jumpDistance;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float randomJumpChance; // 매 초마다 점프할 확률
    
    [Header("장판 설정")]
    [SerializeField] private float slimeDuration = 3f; // 슬라임 장판 지속 시간
    [SerializeField] private float slimeDamage = 1f; // 슬라임 장판 데미지
    [SerializeField] private float slimeDamageInterval = 1f; // 슬라임 장판 데미지 주는 간격

    private float randomJumpTimer = 0f;

    // 상태들
    private IdleState idleState;
    private PatrolState patrolState;
    private AttackState attackState;
    private ChaseState chaseState;
    private JumpState jumpState;

    // 순찰 시작점
    private Vector2 startPosition;

    // 상태 머신
    public IEnemyState currentState => stateMachine.CurrentState;

    // 상태 접근자 메서드들
    public IdleState GetIdleState() => idleState;
    public PatrolState GetPatrolState() => patrolState;
    public AttackState GetAttackState() => attackState;
    public ChaseState GetChaseState() => chaseState;
    public JumpState GetJumpState() => jumpState;

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
        
        // 순찰 상태일 때만 랜덤 점프 체크
        if (currentState == patrolState)
        {
            randomJumpTimer += Time.deltaTime;
            if (randomJumpTimer >= 1f) // 매 초마다 체크
            {
                randomJumpTimer = 0f;
                if (Random.value < randomJumpChance) // 설정된 확률로 점프
                {
                    SwitchToJumpState();
                }
            }
        }
    }

    /// <summary>
    /// 물리 기반 업데이트
    /// </summary>
    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // BaseEnemy의 FixedUpdate 호출
    }

    #endregion

    #region Core Methods

    /// <summary>
    /// 적 초기화 및 상태 설정
    /// </summary>
    protected override void InitializeEnemy()
    {
        // 순찰 경로 설정 (시작점 기준 좌우로 순찰)
        Vector2 leftPoint = startPosition - new Vector2(patrolDistance, 0);
        Vector2 rightPoint = startPosition + new Vector2(patrolDistance, 0);

        // 상태 생성 (두 개의 웨이포인트 설정)
        patrolState = new PatrolState(this, stateMachine, new Vector2[] { leftPoint, rightPoint }, patrolWaitTime);
        chaseState = new ChaseState(this, stateMachine, chaseSpeed);
        jumpState = new JumpState(this, stateMachine, jumpPower, jumpDistance, jumpCooldown);

        // 상태 머신 초기화
        stateMachine.ChangeState(patrolState);
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
        stateMachine.FixedUpdate();
    }

    /// <summary>
    /// 단순 충돌 공격
    /// </summary>
    public override void PerformAttack()
    {
        // 슬라임은 충돌 공격만 처리하기때문에 이부분은 비워둠
    }

    /// <summary>
    /// 넉백 처리
    /// </summary>
    public override void ApplyKnockback(Vector2 direction, float force)
    {
        base.ApplyKnockback(direction, 2f);
        StartCoroutine(KnockbackCoroutine(0.5f));
    }

    /// <summary>
    /// 넉백 후 이동 재게 코루틴
    /// </summary>
    private IEnumerator KnockbackCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (playerDetected)
        {
            stateMachine.ChangeState(chaseState);
        }
        else
        {
            stateMachine.ChangeState(patrolState);
        }
    }

    #endregion

    #region Player Detection

    /// <summary>
    /// 플레이어 감지 시 호출됨
    /// </summary>
    protected override void OnPlayerDetected()
    {
        // 추격 상태로 전환
        if (currentState != chaseState && currentState != attackState)
        {
            stateMachine.ChangeState(chaseState);
        }
    }

    /// <summary>
    /// 플레이어를 놓쳤을 때 호출됨
    /// </summary>
    protected override void OnPlayerLost()
    {
        // 필요시 구현
    }

    #endregion

    #region State Switch Methods

    /// <summary>
    /// 대기 상태로 전환
    /// </summary>
    public override void SwitchToIdleState()
    {
        stateMachine.ChangeState(idleState);
    }

    /// <summary>
    /// 순찰 상태로 전환
    /// </summary>
    public override void SwitchToPatrolState()
    {
        stateMachine.ChangeState(patrolState);
    }

    /// <summary>
    /// 추격 상태로 전환
    /// </summary>
    public override void SwitchToChaseState()
    {
        stateMachine.ChangeState(chaseState);
    }

    /// <summary>
    /// 점프 상태로 전환
    /// </summary>
    public override void SwitchToJumpState()
    {
        stateMachine.ChangeState(jumpState);
    }

    #endregion
}