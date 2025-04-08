using UnityEngine;

public class PlayerRunningState : PlayerStateBase
{
    public PlayerRunningState(PlayerController playerController) : base(playerController) { }

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
        // 이동 입력이 없으면 대기 상태로 전환
        if (!player.IsMoving())
        {
            player.ChangeState(PlayerStateType.Idle);
            return;
        }

        // 스프린트 상태로 전환 확인
        if (player.IsSprinting)
        {
            player.ChangeState(PlayerStateType.Sprinting);
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