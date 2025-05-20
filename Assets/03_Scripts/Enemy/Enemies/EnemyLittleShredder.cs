using System.Collections;
using UnityEngine;
using Enemy.States;

/// <summary>
/// 순찰, 추적, 공격을 하는 멍멍이 // 플레이어가 자신보다 높은 곳에 있으면 점프를 함
/// </summary>
public class EnemyLittleShredder : BaseEnemy
{
    #region Variables

    [Header("순찰 설정")]
    [SerializeField] private float patrolDistance; // 순찰 거리
    [SerializeField] private float patrolWaitTime; // 방향 전환 시 대기 시간
    [Header("추격 설정")]
    [SerializeField] private float chaseSpeed; // 추격 속도 // 배수로 잡힘 ex) 1로 설정하면 기본 속도, 2로 설정하면 두 배 속도
    [Header("공격 설정")]
    [SerializeField] private float attackSpeed; // 공격 속도
    [Header("점프 설정")]
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private float jumpDistance = 3f;
    [SerializeField] private float jumpCooldown = 3f;
    [SerializeField] private float randomJumpChance = 0.1f; // 매 초마다 점프할 확률

    private float randomJumpTimer = 0f;

    // 상태들
    private IdleState idleState;
    private PatrolState patrolState;
    private AttackState attackState;
    private ChaseState chaseState;

    // 순찰 시작점
    private Vector2 startPosition;

    // 상태 머신
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
        
        // 순찰 상태일 때만 랜덤 점프 체크
        if (currentState == patrolState)
        {
            randomJumpTimer += Time.deltaTime;
            if (randomJumpTimer >= 1f) // 매 초마다 체크
            {
                randomJumpTimer = 0f;
                if (Random.value < randomJumpChance) // 설정된 확률로 점프
                {
                    Debug.Log("랜덤 점프 시도!");
                    SwitchToState<JumpState>();
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
        RegisterState(new PatrolState(this, stateMachine, new Vector2[] { leftPoint, rightPoint }, patrolWaitTime));
        RegisterState(new AttackState(this, stateMachine, attackSpeed));
        RegisterState(new ChaseState(this, stateMachine, chaseSpeed));
        RegisterState(new JumpState(this, stateMachine, jumpPower, jumpDistance, jumpCooldown));

        // 상태 머신 초기화
        SwitchToState<PatrolState>();
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
        // 추격 후 공격상태에서 범위안에 들어왔다면 물어뜯기
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
            SwitchToState<ChaseState>();
        }
        else
        {
            SwitchToState<PatrolState>();
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
            SwitchToState<ChaseState>();
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
}