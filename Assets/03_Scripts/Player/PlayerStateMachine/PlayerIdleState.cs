using UnityEngine;

public class PlayerIdleState : IPlayerState
{
    private PlayerStateManager stateManager;

    public PlayerIdleState(PlayerStateManager stateManager)
    {
        this.stateManager = stateManager;
    }

    public void Enter()
    {
        // Idle 상태 진입 시 초기화
        stateManager.SetJumping(false);
        stateManager.SetWallSliding(false);
        stateManager.SetSprinting(false);
    }

    public void HandleInput()
    {
        // 입력 처리
        var inputHandler = stateManager.GetInputHandler();

        // 입력에 따른 상태 전환 로직은 주로 PlayerStateManager에서 처리
    }

    public void Update()
    {
        // 상태 로직 업데이트
    }

    public void FixedUpdate()
    {
        // 입력이 없으면 마찰 적용
        var movement = stateManager.GetMovement();
        movement.Move(Vector2.zero); // 제로 벡터를 전달하여 마찰 적용
    }

    public void Exit()
    {
        // 상태 종료 시 정리 작업
    }
}