using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 벽 슬라이딩 상태
public class PlayerWallSlidingState : PlayerStateBase
{
    private float originalGravity;

    public PlayerWallSlidingState(PlayerController playerController) : base(playerController) { }

    public override void Enter()
    {
        player.IsWallSliding = true;
        player.UpdateAnimations();
        // 원래 중력값 저장
        originalGravity = player.Rb.gravityScale;
    }

    public override void Exit()
    {
        player.IsWallSliding = false;
        // 원래 중력값 복원
        player.Rb.gravityScale = originalGravity;
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
        // 벽에서 떨어졌는지 확인
        if (!player.IsTouchingWall() || player.IsGrounded())
        {
            player.ChangeState(PlayerStateType.Falling);
            return;
        }

        // 현재 방향과 반대 방향키가 눌렸는지 확인
        if (!player.IsMovingInFacingDirection())
        {
            player.ChangeState(PlayerStateType.Falling);
            return;
        }
    }

    public override void FixedUpdate()
    {
        // 벽 슬라이딩 처리
        player.WallSlide();
    }
}