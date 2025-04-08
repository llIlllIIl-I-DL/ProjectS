using UnityEngine;

public class PlayerFallingState : IPlayerState
{
    private PlayerStateManager stateManager;
    private float fallStartTime;

    public PlayerFallingState(PlayerStateManager stateManager)
    {
        this.stateManager = stateManager;
    }

    public void Enter()
    {
        fallStartTime = Time.time;
        Debug.Log("낙하 상태 시작");
    }

    public void HandleInput()
    {
        // 낙하 중 입력 처리
        // 대부분의 입력 처리는 PlayerStateManager의 이벤트 핸들러에서 처리됨
    }

    public void Update()
    {
        var collisionDetector = stateManager.GetCollisionDetector();

        // 땅에 닿으면 상태 전환 (HandleGroundedChanged에서 처리)

        // 벽에 닿으면 벽 슬라이딩 상태로 전환
        if (collisionDetector.IsTouchingWall && !collisionDetector.IsGrounded)
        {
            stateManager.ChangeState(PlayerStateType.WallSliding);
        }
    }

    public void FixedUpdate()
    {
        // 낙하 중 이동 처리
        var inputHandler = stateManager.GetInputHandler();
        var movement = stateManager.GetMovement();
        var settings = stateManager.GetSettings();

        // 낙하 중 이동 (공중 조작)
        movement.Move(inputHandler.MoveDirection);

        // 낙하 중 중력 가속도 증가 (선택적)
        var rb = movement.GetType().GetProperty("Velocity")?.GetValue(movement) as Vector2?;
        if (rb.HasValue && rb.Value.y < 0)
        {
            // 이 부분은 PlayerMovement에 별도의 메서드로 구현하는 것이 좋음
            // 여기서는 예시로만 표시
        }
    }

    public void Exit()
    {
        Debug.Log("낙하 상태 종료");
    }
}