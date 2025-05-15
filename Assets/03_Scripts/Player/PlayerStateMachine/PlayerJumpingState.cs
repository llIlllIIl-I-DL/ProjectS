using UnityEngine;

public class PlayerJumpingMovementState : PlayerMovementStateBase
{
    private float jumpStartTime;

    public PlayerJumpingMovementState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        // 점프 상태 설정
        stateMachine.SetJumping(true);
        jumpStartTime = Time.time;
        Debug.Log("점프 상태 시작");
    }

    public override void HandleInput()
    {
        var inputHandler = stateMachine.GetInputHandler();

        // 점프 버튼을 놓으면 점프 컷 (JumpRelease 이벤트로 처리되므로 여기서는 추가 작업 불필요)
    }

    public override void Update()
    {
        var movement = stateMachine.GetMovement();
        var collisionDetector = stateMachine.GetCollisionDetector();

        // 상승이 멈추고 하강하기 시작하면 Falling 상태로 전환
        if (movement.Velocity.y <= 0)
        {
            stateMachine.ChangeState(MovementStateType.Falling);
            return;
        }

        // 벽에 닿으면 WallSliding 상태로 전환
        if (collisionDetector.IsTouchingWall && !collisionDetector.IsGrounded)
        {
            stateMachine.ChangeState(MovementStateType.WallSliding);
            return;
        }
    }

    public override void FixedUpdate()
    {
        // 공중에서 이동 처리
        var inputHandler = stateMachine.GetInputHandler();
        var movement = stateMachine.GetMovement();

        // 점프 중 이동 (공중 조작)
        movement.Move(inputHandler.MoveDirection);

        // 공중에서 중력 가속도 적용은 Rigidbody2D에 의해 자동으로 처리됨
    }

    public override void Exit()
    {
        Debug.Log("점프 상태 종료");
    }
}