using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class PlayerSprintingState : PlayerStateBase
{
    private float sprintStartTime;
    private PlayerStateManager stateManager;

    public PlayerSprintingState(PlayerStateManager stateManager) : base (stateManager){}
    public override void Enter()
    {
        sprintStartTime = Time.time;
        player.SetSprinting(true);
        Debug.Log("스프린트 상태 시작");
    }
    
    public override void HandleInput()
    {
        // 방향키 입력이 없거나 방향이 바뀌면 스프린트 종료
        var inputHandler = player.GetInputHandler();
        int facingDirection = player.GetMovement().FacingDirection;
        
        if (!inputHandler.IsMoving() || 
            (facingDirection > 0 && inputHandler.IsLeftPressed) ||
            (facingDirection < 0 && inputHandler.IsRightPressed))
        {
            stateManager.ChangeState(PlayerStateType.Running);
        }
    }
    
    public override void Update()
    {
        // 지면에서 떨어지면 스프린트 종료
        if (!player.GetCollisionDetector().IsGrounded)
        {
            stateManager.ChangeState(PlayerStateType.Falling);
        }
    }
    
    public override void FixedUpdate()
    {
        // 스프린트 이동 (가속도와 최대 속도 증가)
        var inputHandler = player.GetInputHandler();
        var movement = player.GetMovement();
        
        // true 파라미터로 스프린트 상태 전달
        movement.Move(inputHandler.MoveDirection, true);
    }
    
    public override void Exit()
    {
        player.SetSprinting(false);
        Debug.Log("스프린트 상태 종료");
    }
}