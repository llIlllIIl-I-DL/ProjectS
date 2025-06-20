using System.Collections;
using UnityEngine;
using Enemy.States;

/// <summary>
/// 간단한 순찰 적 (Met/굼바 스타일)
/// </summary>
public class EnemyDust : BaseEnemy
{
    #region Variables

    [Header("순찰 설정")]
    [SerializeField] private float patrolDistance; // 순찰 거리
    [SerializeField] private float patrolWaitTime; // 방향 전환 시 대기 시간

    // 순찰 시작점
    private Vector2 startPosition;

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
        // Debug.Log("PatrolEnemy Update"); // 디버그 로그 - 개발 완료 후 제거
    }

    /// <summary>
    /// 물리 기반 업데이트
    /// </summary>
    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // BaseEnemy의 FixedUpdate 호출
        // Debug.Log($"Current Position: {transform.position}"); // 디버그 로그 - 개발 완료 후 제거
    }

    /// <summary>
    /// 데미지 처리
    /// </summary>
    // public override void TakeDamage(float damage)
    // {
    //     base.TakeDamage(damage);
    // }
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
        // 단순 패트롤 적은 충돌 데미지만 주므로 여기선 구현 불필요
        // 실제 충돌 데미지는 BaseEnemy의 OnCollisionEnter2D에서 처리됨
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
        SwitchToState<PatrolState>();
        Debug.Log($"{gameObject.name}이(가) 넉백 후 이동을 재개합니다.");
    }

    #endregion

    #region Player Detection

    /// <summary>
    /// 플레이어 감지 시 호출됨 (반응 없음)
    /// </summary>
    protected override void OnPlayerDetected()
    {
        // 단순 패트롤 적은 플레이어 감지에 반응하지 않음
    }

    /// <summary>
    /// 플레이어를 놓쳤을 때 호출됨 (반응 없음)
    /// </summary>
    protected override void OnPlayerLost()
    {
        // 단순 패트롤 적은 플레이어를 놓치는 것에 반응하지 않음
    }

    #endregion
}