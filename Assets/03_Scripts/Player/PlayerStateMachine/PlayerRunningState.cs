using UnityEngine;
public class PlayerRunningState : PlayerStateBase
{
    public PlayerRunningState(PlayerController playerController) : base(playerController) { }

    public override void Enter()
    {
        // 애니메이션 설정
        player.UpdateAnimations("Run");
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
        // 이동 입력이 없으면 대기 상태로 전환
        if (Mathf.Abs(player.MoveInput.x) < 0.1f)
        {
            player.ChangeState(PlayerStateType.Idle);
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
        // 이동 처리
        player.Move();
    }
}