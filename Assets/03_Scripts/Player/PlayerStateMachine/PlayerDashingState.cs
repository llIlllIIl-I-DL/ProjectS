using UnityEngine;

public class PlayerDashingMovementState : PlayerMovementStateBase
{
    private float dashStartTime;

    public PlayerDashingMovementState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        // 대시 시작 시간 기록
        dashStartTime = Time.time;
        Debug.Log("대시 상태 시작");

        // PlayerMovement.Dash() 메서드는 이미 상태머신에서 직접 호출되므로 여기서는 호출하지 않음
    }

    public override void HandleInput()
    {
        // 대시 중에는 다른 입력이 무시됨
        // 대시가 끝난 후 점프 입력은 상태머신에서 처리됨
    }

    public override void Update()
    {
        // PlayerSettings를 PlayerStateManager를 통해 가져옴
        var stateManager = stateMachine.gameObject.GetComponent<PlayerStateManager>();
        PlayerSettings settings = null;
        if (stateManager != null)
        {
            settings = stateManager.GetSettings();
        }
        if (settings == null) return;

        // 대시 지속 시간이 끝나면 상태 전환
        // 이 부분은 실제로 상태머신의 DashCooldownRoutine에서 처리됨
        // 여기서는 추가 체크 용도로만 사용
        if (Time.time >= dashStartTime + settings.dashDuration)
        {
            // 실제 상태 전환은 코루틴에서 처리되므로 주석 처리
            // var collisionDetector = stateMachine.GetCollisionDetector();
            // var inputHandler = stateMachine.GetInputHandler();
            // 
            // if (!collisionDetector.IsGrounded)
            // {
            //     stateMachine.ChangeState(MovementStateType.Falling);
            // }
            // else
            // {
            //     stateMachine.ChangeState(inputHandler.IsMoving() ? 
            //                             MovementStateType.Running : 
            //                             MovementStateType.Idle);
            // }
        }
    }

    public override void FixedUpdate()
    {
        // 대시 중 물리 처리는 PlayerMovement.Dash() 메서드와 
        // DashCoroutine에서 자동으로 처리되므로 여기서는 추가 작업 불필요
    }

    public override void Exit()
    {
        Debug.Log("대시 상태 종료");
    }
}