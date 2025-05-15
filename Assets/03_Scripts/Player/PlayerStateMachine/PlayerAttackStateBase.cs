using UnityEngine;

// 기본 공격 상태 추상 클래스
public abstract class PlayerAttackStateBase : IPlayerAttackState
{
    protected PlayerAttackStateMachine stateMachine;

    public PlayerAttackStateBase(PlayerAttackStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void HandleInput() { }
}

