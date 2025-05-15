using UnityEngine;

public class PlayerRunningMovementState : PlayerMovementStateBase
{
    public PlayerRunningMovementState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        // Running 상태 진입 시 초기화
        stateMachine.SetJumping(false);
        stateMachine.SetWallSliding(false);
    }

    public override void HandleInput()
    {
        var inputHandler = stateMachine.GetInputHandler();
        
        // 입력이 없으면 Idle 상태로 전환
        if (!inputHandler.IsMoving())
        {
            stateMachine.ChangeState(MovementStateType.Idle);
            return;
        }
        
        // 점프 입력이 있으면 Jumping 상태로 전환
        if (inputHandler.JumpPressed)
        {
            stateMachine.ChangeState(MovementStateType.Jumping);
            return;
        }
    }

    public override void Update()
    {
        var collisionDetector = stateMachine.GetCollisionDetector();
        
        // 땅에서 벗어나면 Falling 상태로 전환
        if (!collisionDetector.IsGrounded)
        {
            stateMachine.ChangeState(MovementStateType.Falling);
            return;
        }
    }

    public override void FixedUpdate()
    {
        // 이동 처리
        var inputHandler = stateMachine.GetInputHandler();
        var movement = stateMachine.GetMovement();
        movement.Move(inputHandler.MoveDirection);
    }

    public override void Exit()
    {
        // 상태 종료 시 정리 작업
    }
} 