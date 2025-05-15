using UnityEngine;

// 기본 상태 추상 클래스
public abstract class PlayerMovementStateBase : IPlayerMovementState
{
    protected PlayerMovementStateMachine stateMachine;

    public PlayerMovementStateBase(PlayerMovementStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void HandleInput() { }
}
