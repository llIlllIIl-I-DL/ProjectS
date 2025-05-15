using UnityEngine;

public class PlayerFallingMovementState : PlayerMovementStateBase
{
    private float fallStartTime;

    public PlayerFallingMovementState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        fallStartTime = Time.time;
        Debug.Log("낙하 상태 시작");
    }

    public override void HandleInput()
    {
        // 낙하 중 입력 처리
        // 대부분의 입력 처리는 PlayerMovementStateMachine의 이벤트 핸들러에서 처리됨
    }

    public override void Update()
    {
        var collisionDetector = stateMachine.GetCollisionDetector();

        // 땅에 닿으면 상태 전환 (HandleGroundedChanged에서 처리)

        // 벽에 닿으면 벽 슬라이딩 상태로 전환
        if (collisionDetector.IsTouchingWall && !collisionDetector.IsGrounded)
        {
            stateMachine.ChangeState(MovementStateType.WallSliding);
        }
    }

    public override void FixedUpdate()
    {
        // 낙하 중 이동 처리
        var inputHandler = stateMachine.GetInputHandler();
        var movement = stateMachine.GetMovement();

        // 낙하 중 이동 (공중 조작)
        movement.Move(inputHandler.MoveDirection);

        // 낙하 중 중력 가속도 증가 (선택적)
        var rb = movement.GetType().GetProperty("Velocity")?.GetValue(movement) as Vector2?;
        if (rb.HasValue && rb.Value.y < 0)
        {
            // 이 부분은 PlayerMovement에 별도의 메서드로 구현하는 것이 좋음
        }
    }

    public override void Exit()
    {
        Debug.Log("낙하 상태 종료");
    }
}