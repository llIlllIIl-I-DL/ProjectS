using UnityEngine;

public class PlayerSprintingState : IPlayerState
{
    private PlayerStateManager stateManager;

    public PlayerSprintingState(PlayerStateManager stateManager)
    {
        this.stateManager = stateManager;
    }

    public void Enter()
    {
        // 스프린트 상태 설정
        stateManager.SetSprinting(true);
        Debug.Log("스프린트 상태 시작");
    }

    public void HandleInput()
    {
        var inputHandler = stateManager.GetInputHandler();

        // 이동 입력이 없을 때만 스프린트 종료
        if (!inputHandler.IsMoving())
        {
            stateManager.ChangeState(PlayerStateType.Idle);
        }
    }

    public void Update()
    {
        var collisionDetector = stateManager.GetCollisionDetector();

        // 지면에서 떨어지면 낙하 상태로 전환
        if (!collisionDetector.IsGrounded)
        {
            stateManager.ChangeState(PlayerStateType.Falling);
        }
    }

    public void FixedUpdate()
    {
        // 스프린트 이동 처리 (빠른 속도로)
        var inputHandler = stateManager.GetInputHandler();
        var movement = stateManager.GetMovement();

        // true 파라미터로 스프린트 이동 적용
        movement.Move(inputHandler.MoveDirection, true);
    }

    public void Exit()
    {
        // 스프린트 상태 종료
        stateManager.SetSprinting(false);
        Debug.Log("스프린트 상태 종료");
    }
}