using UnityEngine;

public class PlayerDashingState : IPlayerState
{
    private PlayerStateManager stateManager;
    private float dashStartTime;

    public PlayerDashingState(PlayerStateManager stateManager)
    {
        this.stateManager = stateManager;
    }

    public void Enter()
    {
        // 대시 시작 시간 기록
        dashStartTime = Time.time;
        Debug.Log("대시 상태 시작");

        // PlayerMovement.Dash() 메서드는 이미 PlayerStateManager에서 직접 호출되므로 여기서는 호출하지 않음
    }

    public void HandleInput()
    {
        // 대시 중에는 다른 입력이 무시됨
        // 대시가 끝난 후 점프 입력은 PlayerStateManager에서 처리됨
    }

    public void Update()
    {
        var settings = stateManager.GetSettings();

        // 대시 지속 시간이 끝나면 상태 전환
        // 이 부분은 실제로 PlayerStateManager의 DashCooldownRoutine에서 처리됨
        // 여기서는 추가 체크 용도로만 사용
        if (Time.time >= dashStartTime + settings.dashDuration && stateManager.IsDashing)
        {
            // 실제 상태 전환은 코루틴에서 처리되므로 주석 처리
            // var collisionDetector = stateManager.GetCollisionDetector();
            // var inputHandler = stateManager.GetInputHandler();
            // 
            // if (!collisionDetector.IsGrounded)
            // {
            //     stateManager.ChangeState(PlayerStateType.Falling);
            // }
            // else
            // {
            //     stateManager.ChangeState(inputHandler.IsMoving() ? 
            //                             PlayerStateType.Running : 
            //                             PlayerStateType.Idle);
            // }
        }
    }

    public void FixedUpdate()
    {
        // 대시 중 물리 처리는 PlayerMovement.Dash() 메서드와 
        // DashCoroutine에서 자동으로 처리되므로 여기서는 추가 작업 불필요
    }

    public void Exit()
    {
        Debug.Log("대시 상태 종료");
    }
}