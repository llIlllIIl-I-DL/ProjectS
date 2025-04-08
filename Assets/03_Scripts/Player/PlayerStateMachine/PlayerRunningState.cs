using UnityEngine;

public class PlayerRunningState : IPlayerState
{
    private PlayerStateManager stateManager;

    public PlayerRunningState(PlayerStateManager stateManager)
    {
        this.stateManager = stateManager;
    }

    public void Enter()
    {
        // Running 상태 진입 시 초기화
        stateManager.SetJumping(false);
        stateManager.SetWallSliding(false);
    }

    public void HandleInput()
    {
        // 입력 처리
    }

    public void Update()
    {
        // 이동 방향 갱신
    }

    public void FixedUpdate()
    {
        // 이동 처리
        var inputHandler = stateManager.GetInputHandler();
        var movement = stateManager.GetMovement();
        movement.Move(inputHandler.MoveDirection);
    }

    public void Exit()
    {
        // 상태 종료 시 정리 작업
    }
}