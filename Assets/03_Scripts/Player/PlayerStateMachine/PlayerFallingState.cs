using UnityEngine;

public class PlayerFallingState : PlayerStateBase
{
    private float originalGravity;

    public PlayerFallingState(PlayerController playerController) : base(playerController) { }

    public override void Enter()
    {
        // 애니메이션 설정
        player.UpdateAnimations(null);

        // 원래 중력값 저장 후 중력 배율 증가
        originalGravity = player.Rb.gravityScale;
        player.Rb.gravityScale = player.FallGravityMultiplier;
    }

    public override void Exit()
    {
        // 중력 배율 원래대로
        player.Rb.gravityScale = originalGravity;
    }

    public override void HandleInput()
    {
        // 대시 처리
        if (player.DashPressed && player.CanDash)
        {
            player.DashPressed = false;
            player.ChangeState(PlayerStateType.Dashing);
            return;
        }

        // 점프 버퍼 처리 (지면에 닿기 직전 점프 입력)
        if (player.JumpPressed)
        {
            player.LastJumpTime = player.JumpBufferTime;
            player.JumpPressed = false;
        }
    }

    public override void Update()
    {
        // 지면에 닿으면 상태 전환
        if (player.IsGrounded())
        {
            // 버퍼된 점프 입력이 있으면 점프
            if (player.LastJumpTime > 0)
            {
                player.ChangeState(PlayerStateType.Jumping);
                return;
            }

            // 이동 중이면 달리기, 아니면 대기 상태로
            if (player.IsMoving())
            {
                if (player.IsSprinting)
                    player.ChangeState(PlayerStateType.Sprinting);
                else
                    player.ChangeState(PlayerStateType.Running);
            }
            else
                player.ChangeState(PlayerStateType.Idle);

            return;
        }

        // 벽에 붙었을 때 벽 슬라이딩 상태로 전환
        if (player.IsTouchingWall() && player.IsMovingInFacingDirection())
        {
            player.ChangeState(PlayerStateType.WallSliding);
            return;
        }
    }

    public override void FixedUpdate()
    {
        // 이동 처리
        player.Move();
    }
}