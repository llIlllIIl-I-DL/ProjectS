using UnityEngine;

public class PlayerIdleState : PlayerStateBase
{
    public PlayerIdleState(PlayerController playerController) : base(playerController) { }

    public override void Enter()
    {
        // 애니메이션 설정 (상태 문자열 전달하지 않음)
        player.UpdateAnimations(null);
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
        if (player.IsMoving())
        {
            player.ChangeState(PlayerStateType.Running);
            return;
        }

        // 지면에서 벗어나면 낙하 상태로 전환
        if (!player.IsGrounded())
        {
            player.ChangeState(PlayerStateType.Falling);
            return;
        }
    }

    public override void FixedUpdate()
    {
        // 이동 입력이 있으면 Move 호출 (상태 전환 전에도 움직임 처리)
        if (player.IsMoving())
        {
            player.Move();
            return;
        }
        
        // Idle 상태에서는 강제로 수평 속도를 0으로 설정
        if (Mathf.Abs(player.Rb.velocity.x) < 0.1f)
        {
            player.Rb.velocity = new Vector2(0f, player.Rb.velocity.y);
        }
        // 작은 속도가 있다면 마찰력 적용
        else if (player.IsGrounded())
        {
            // 속도가 매우 작으면 완전히 멈춤
            if (Mathf.Abs(player.Rb.velocity.x) < 0.2f)
            {
                player.Rb.velocity = new Vector2(0f, player.Rb.velocity.y);
            }
            // 그 외에는 강한 마찰력 적용
            else
            {
                float friction = Mathf.Min(Mathf.Abs(player.Rb.velocity.x), 0.5f);
                friction *= Mathf.Sign(player.Rb.velocity.x);
                player.Rb.AddForce(Vector2.right * -friction, ForceMode2D.Impulse);
            }
        }
    }
}