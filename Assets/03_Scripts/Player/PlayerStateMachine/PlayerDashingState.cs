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

}
