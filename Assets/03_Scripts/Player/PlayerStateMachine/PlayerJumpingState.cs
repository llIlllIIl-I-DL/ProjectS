using UnityEngine;
public class PlayerJumpingState : PlayerStateBase
{
    public PlayerJumpingState(PlayerController playerController) : base(playerController) { }

    public override void Enter()
    {
        // 점프 실행
        player.Jump();

        // 애니메이션 설정
        player.UpdateAnimations("Jump");
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

        // 점프 버튼 뗌 처리 (점프 높이 조절)
        if (player.JumpReleased && player.Rb.velocity.y > 0 && player.IsJumping)
        {
            player.Rb.velocity = new Vector2(player.Rb.velocity.x, player.Rb.velocity.y * 0.5f);
            player.IsJumping = false;
            player.JumpReleased = false;
        }
    }

    public override void Update()
    {
        // 벽에 붙었을 때 벽 슬라이딩 상태로 전환
        if (player.IsTouchingWall() && player.MoveInput.x != 0)
        {
            player.ChangeState(PlayerStateType.WallSliding);
            return;
        }

        // 상승 속도가 음수가 되면 낙하 상태로 전환
        if (player.Rb.velocity.y <= 0)
        {
            player.ChangeState(PlayerStateType.Falling);
            return;
        }
    }

    public override void FixedUpdate()
    {
        // 이동 처리
        player.Move();
    }
}