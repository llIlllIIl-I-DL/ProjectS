using System.Collections;
using UnityEngine;
public interface IPlayerState
{
    void Enter();
    void Exit();
    void Update();
    void FixedUpdate();
    void HandleInput();
}
// 기본 상태 추상 클래스
public abstract class PlayerStateBase : IPlayerState
{
    protected PlayerController player;

    public PlayerStateBase(PlayerController playerController)
    {
        player = playerController;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void HandleInput() { }
}