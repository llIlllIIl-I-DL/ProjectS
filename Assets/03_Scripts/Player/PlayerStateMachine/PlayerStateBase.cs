using System.Collections;
using UnityEngine;
// 기본 상태 추상 클래스
public abstract class PlayerStateBase : IPlayerState
{
    protected PlayerStateManager player;

    public PlayerStateBase(PlayerStateManager stateManager)
    {
        player = stateManager;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void HandleInput() { }
}