using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 간단한 순찰 적 (Met/굼바 스타일)
/// </summary>
public class PatrolEnemy : BaseEnemy
{
    [Header("순찰 설정")]
    [SerializeField] private float patrolDistance; // 순찰 거리
    [SerializeField] private float patrolWaitTime; // 방향 전환 시 대기 시간

    // 상태들
    private PatrolState patrolState;
    private IdleState idleState;

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
        Debug.Log("PatrolEnemy Update"); // 디버그 로그
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // BaseEnemy의 FixedUpdate 호출
        Debug.Log($"Current Position: {transform.position}"); // 디버그 로그
    }

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
        idleState = new IdleState(this, stateMachine, patrolWaitTime);

        // 초기 상태 설정 ★중요: 이 부분이 빠졌음★
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
        // 단순 패트롤 적은 충돌 데미지만 주므로 여기선 구현 불필요
    }
}