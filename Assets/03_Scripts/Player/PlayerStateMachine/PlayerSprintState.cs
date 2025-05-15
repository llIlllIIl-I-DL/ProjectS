using UnityEngine;

public class PlayerSprintingMovementState : PlayerMovementStateBase
{
    public PlayerSprintingMovementState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        // 스프린트 상태 설정
        stateMachine.SetSprinting(true);
        Debug.Log("스프린트 상태 시작");
    }

    public override void HandleInput()
    {
        var inputHandler = stateMachine.GetInputHandler();

        // 이동 입력이 없을 때만 스프린트 종료
        if (!inputHandler.IsMoving())
        {
            stateMachine.ChangeState(MovementStateType.Idle);
        }
    }

    public override void Update()
    {
        var collisionDetector = stateMachine.GetCollisionDetector();

        // 지면에서 떨어지면 낙하 상태로 전환
        if (!collisionDetector.IsGrounded)
        {
            stateMachine.ChangeState(MovementStateType.Falling);
        }
    }

    public override void FixedUpdate()
    {
        // 스프린트 이동 처리 (빠른 속도로)
        var inputHandler = stateMachine.GetInputHandler();
        var movement = stateMachine.GetMovement();

        // true 파라미터로 스프린트 이동 적용
        movement.Move(inputHandler.MoveDirection, true);
    }

    public override void Exit()
    {
        // 스프린트 상태 종료
        stateMachine.SetSprinting(false);
        Debug.Log("스프린트 상태 종료");
    }
}