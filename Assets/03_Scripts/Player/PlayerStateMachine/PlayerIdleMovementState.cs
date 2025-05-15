using UnityEngine;

public class PlayerIdleMovementState : PlayerMovementStateBase
{
    public PlayerIdleMovementState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        // Idle 상태 진입 시 초기화
        stateMachine.SetJumping(false);
        stateMachine.SetWallSliding(false);
    }

    public override void HandleInput()
    {
        // 입력 처리
        var inputHandler = stateMachine.GetInputHandler();

        // 이동 입력이 있으면 Running 상태로 전환
        if (inputHandler.IsMoving())
        {
            stateMachine.ChangeState(MovementStateType.Running);
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
        // 상태 로직 업데이트
    }

    public override void FixedUpdate()
    {
        // 입력이 없으면 마찰 적용
        var movement = stateMachine.GetMovement();
        movement.Move(Vector2.zero); // 제로 벡터를 전달하여 마찰 적용
    }

    public override void Exit()
    {
        // 상태 종료 시 정리 작업
    }
} 