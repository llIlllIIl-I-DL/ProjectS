using UnityEngine;

public class PlayerWallSlidingState : IPlayerState
{
    private PlayerStateManager stateManager;
    private float wallSlideStartTime;
    private int wallDirection; // 벽의 방향을 저장

    public PlayerWallSlidingState(PlayerStateManager stateManager)
    {
        this.stateManager = stateManager;
    }

    public void Enter()
    {
        // 벽 슬라이딩 상태 설정
        stateManager.SetWallSliding(true);
        wallSlideStartTime = Time.time;
        
        // 벽 방향 감지
        var collisionDetector = stateManager.GetCollisionDetector();
        wallDirection = collisionDetector.WallDirection;
        
        Debug.Log($"벽 슬라이딩 상태 시작 - 벽 방향: {wallDirection}");
    }

    public void HandleInput()
    {
        var inputHandler = stateManager.GetInputHandler();

        // 벽에서 반대 방향으로 입력하면 벽에서 떨어짐
        if ((wallDirection < 0 && inputHandler.IsRightPressed) ||
            (wallDirection > 0 && inputHandler.IsLeftPressed))
        {
           
            stateManager.ChangeState(PlayerStateType.Falling);
            return;
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
        
        
    }
}