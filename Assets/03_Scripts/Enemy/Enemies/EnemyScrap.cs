using UnityEngine;
using Enemy.States;

/// <summary>
/// 간단한 순찰 적 (Met/굼바 스타일)
/// </summary>
public class EnemyScrap : BaseEnemy
{
    #region Variables
    
    [Header("순찰 설정")]
    [SerializeField] private float patrolDistance; // 순찰 거리
    [SerializeField] private float patrolWaitTime; // 방향 전환 시 대기 시간

    // 상태들
    private PatrolState patrolState;
    private IdleState idleState;

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
    
    #endregion

    #region Core Methods
    
    /// <summary>
    /// 적 초기화 및 상태 설정
    /// </summary>
    protected override void InitializeEnemy()
    {

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

    #region State Switch Methods
    
    
    #endregion
}