using UnityEngine;

public class PlayerIdleState : PlayerStateBase
{
    public PlayerIdleState(PlayerController playerController) : base(playerController) { }

    public override void Enter()
    {
        // 애니메이션 설정
        player.UpdateAnimations("Idle");
    }

    public override void HandleInput()
    {
        // 점프 처리
        if (player.JumpPressed && player.LastGroundedTime > 0)
        {
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
        // 이동 감지하여 달리기 상태로 전환
        if (Mathf.Abs(player.MoveInput.x) > 0.1f)
        {
            player.ChangeState(PlayerStateType.Running);
            return;
        }

        // 지면에서 벗어나면 낙하 상태로 전환
        if (player.LastGroundedTime <= 0)
        {
            player.ChangeState(PlayerStateType.Falling);
            return;
        }
    }

    public override void FixedUpdate()
    {
        // 마찰력 적용 (더 강하게)
        if (player.IsGrounded())
        {
            // 속도가 매우 작으면 완전히 멈춤
            if (Mathf.Abs(player.Rb.velocity.x) < 0.2f)
            {
                player.Rb.velocity = new Vector2(0f, player.Rb.velocity.y);
            }
            // 그 외에는 강한 마찰력 적용
            else if (Mathf.Abs(player.MoveInput.x) < 0.1f)
            {
                float friction = Mathf.Min(Mathf.Abs(player.Rb.velocity.x), 0.5f);
                friction *= Mathf.Sign(player.Rb.velocity.x);
                player.Rb.AddForce(Vector2.right * -friction, ForceMode2D.Impulse);
            }
        }
    }
}