using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDashingState : PlayerStateBase
{
    public PlayerDashingState(PlayerController playerController) : base(playerController) { }

    public override void Enter()
    {
        player.UpdateAnimations("Dash");
        player.StartCoroutine(player.Dash());
    }

    public override void Exit()
    {
        // 대시 종료 시 처리
        // 대시 코루틴에서 이미 처리하므로 여기서는 추가 작업 없음
    }

    public override void HandleInput()
    {
        // 대시 중에는 입력 처리 없음 (대시 코루틴에서 처리)
    }

    public override void Update()
    {
        // 대시 중에는 다른 상태 검사 없음 (대시 코루틴에서 상태 전환 처리)
    }

    public override void FixedUpdate()
    {
        // 대시 중에는 추가적인 물리 처리 없음
    }
}
