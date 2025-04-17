using UnityEngine;

public class PlayerWallSlidingState : IPlayerState
{
    private PlayerStateManager stateManager;
    private float wallSlideStartTime;

    public PlayerWallSlidingState(PlayerStateManager stateManager)
    {
        this.stateManager = stateManager;
    }

    public void Enter()
    {
        // 벽 슬라이딩 상태 설정
        stateManager.SetWallSliding(true);
        wallSlideStartTime = Time.time;
        Debug.Log("벽 슬라이딩 상태 시작 - 시간: " + wallSlideStartTime);
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
            Debug.Log($"벽에서 반대 방향 입력 감지: FacingDirection={facingDirection}, LeftPressed={inputHandler.IsLeftPressed}, RightPressed={inputHandler.IsRightPressed}");
            // 벽에서 떨어지도록 처리할 수 있음
            // 여기서는 그냥 자연스럽게 떨어지도록 둠
        }
    }

    public void Update()
    {
        var collisionDetector = stateManager.GetCollisionDetector();
        
        Debug.Log($"벽 슬라이딩 상태 Update - IsGrounded: {collisionDetector.IsGrounded}, IsTouchingWall: {collisionDetector.IsTouchingWall}");

        // 땅에 닿으면 Idle 또는 Running 상태로 전환
        if (collisionDetector.IsGrounded)
        {
            var inputHandler = stateManager.GetInputHandler();
            Debug.Log("벽 슬라이딩 중 땅에 닿아 상태 전환");
            stateManager.ChangeState(inputHandler.IsMoving() ?
                                    PlayerStateType.Running :
                                    PlayerStateType.Idle);
            return;
        }

        // 벽에서 떨어지면 Falling 상태로 전환
        if (!collisionDetector.IsTouchingWall)
        {
            Debug.Log("벽에서 떨어져 Falling 상태로 전환");
            stateManager.ChangeState(PlayerStateType.Falling);
            return;
        }
    }

    public void FixedUpdate()
    {
        // 벽 슬라이딩 처리
        var inputHandler = stateManager.GetInputHandler();
        var movement = stateManager.GetMovement();
        var velocity = movement.Velocity;

        // 디버그 정보 출력
        Debug.Log($"벽 슬라이딩 FixedUpdate - 현재 속도: {velocity.x}, {velocity.y}, 아래키: {inputHandler.IsDownPressed}");

        // 아래 방향키를 누르고 있으면 빠르게 슬라이딩
        bool fastSlide = inputHandler.IsDownPressed;
        movement.WallSlide(stateManager.GetSettings().wallSlideSpeed, fastSlide);
    }

    public void Exit()
    {
        // 벽 슬라이딩 상태 종료
        stateManager.SetWallSliding(false);
        float duration = Time.time - wallSlideStartTime;
        Debug.Log($"벽 슬라이딩 상태 종료 - 지속 시간: {duration:F2}초");
    }
}