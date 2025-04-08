using UnityEngine;

public class PlayerWallSlidingState : IPlayerState
{
    private PlayerStateManager stateManager;

    public PlayerWallSlidingState(PlayerStateManager stateManager)
    {
        this.stateManager = stateManager;
    }

    public void Enter()
    {
        // 벽 슬라이딩 상태 설정
        stateManager.SetWallSliding(true);
        Debug.Log("벽 슬라이딩 상태 시작");
    }

    public void HandleInput()
    {
        var inputHandler = stateManager.GetInputHandler();

        // 점프 입력은 PlayerStateManager에서 HandleJumpInput으로 처리

        // 벽에서 반대 방향으로 입력하면 벽에서 떨어짐 (선택적)
        var facingDirection = stateManager.GetMovement().FacingDirection;
        if ((facingDirection > 0 && inputHandler.IsLeftPressed) ||
            (facingDirection < 0 && inputHandler.IsRightPressed))
        {
            // 벽에서 떨어지도록 처리할 수 있음
            // 여기서는 그냥 자연스럽게 떨어지도록 둠
        }
    }

    public void Update()
    {
        var collisionDetector = stateManager.GetCollisionDetector();

        // 땅에 닿으면 Idle 또는 Running 상태로 전환
        if (collisionDetector.IsGrounded)
        {
            var inputHandler = stateManager.GetInputHandler();
            stateManager.ChangeState(inputHandler.IsMoving() ?
                                    PlayerStateType.Running :
                                    PlayerStateType.Idle);
            return;
        }

        // 벽에서 떨어지면 Falling 상태로 전환
        if (!collisionDetector.IsTouchingWall)
        {
            stateManager.ChangeState(PlayerStateType.Falling);
            return;
        }
    }

    public void FixedUpdate()
    {
        // 벽 슬라이딩 처리
        var inputHandler = stateManager.GetInputHandler();
        var movement = stateManager.GetMovement();

        // 아래 방향키를 누르고 있으면 빠르게 슬라이딩
        bool fastSlide = inputHandler.IsDownPressed;
        movement.WallSlide(stateManager.GetSettings().wallSlideSpeed, fastSlide);
    }

    public void Exit()
    {
        // 벽 슬라이딩 상태 종료
        stateManager.SetWallSliding(false);
        Debug.Log("벽 슬라이딩 상태 종료");
    }
}