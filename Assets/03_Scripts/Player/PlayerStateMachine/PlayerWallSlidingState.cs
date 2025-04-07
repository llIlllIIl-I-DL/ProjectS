using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 벽 슬라이딩 상태
public class PlayerWallSlidingState : PlayerStateBase
{
    private float wallSlideSpeed = 3f;

    public PlayerWallSlidingState(PlayerController playerController) : base(playerController) { }

    public override void Enter()
    {
        player.IsWallSliding = true;
        player.UpdateAnimations("WallSlide");
    }

    public override void Exit()
    {
        player.IsWallSliding = false;
    }

    public override void HandleInput()
    {
        // 점프 입력 처리 (벽 점프)
        if (player.JumpPressed)
        {
            player.JumpPressed = false;
            player.WallJump();
            player.ChangeState(PlayerStateType.Jumping);
            return;
        }

        // 대시 처리
        if (player.DashPressed && player.CanDash)
        {
            player.DashPressed = false;
            player.ChangeState(PlayerStateType.Dashing);
            return;
        }
    }

    public override void Update()
    {
        // 벽에서 떨어지거나, 지면에 닿으면 상태 전환
        if (!player.IsTouchingWall() || player.IsGrounded() || player.MoveInput.x == 0)
        {
            if (player.IsGrounded())
            {
                if (Mathf.Abs(player.MoveInput.x) > 0.1f)
                    player.ChangeState(PlayerStateType.Running);
                else
                    player.ChangeState(PlayerStateType.Idle);
            }
            else
            {
                player.ChangeState(PlayerStateType.Falling);
            }
            return;
        }
    }

    public override void FixedUpdate()
    {
        // 벽 슬라이딩 속도 제한
        player.Rb.velocity = new Vector2(player.Rb.velocity.x, Mathf.Max(player.Rb.velocity.y, -wallSlideSpeed));
    }
}