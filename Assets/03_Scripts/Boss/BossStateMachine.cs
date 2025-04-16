using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum BossState
{
    Idle,
    Attack,
    Move,
}
public abstract class BossStateMachine : MonoBehaviour
{
    protected IEnemyState currentState;

    private Dictionary<BossState, IEnemyState> states = new Dictionary<BossState, IEnemyState>();

    public void Start()
    {
        states.Add(BossState.Idle, new BossIdleState(this));
        states.Add(BossState.Attack, new BossAttackState(this));
        states.Add(BossState.Move, new BossMoveState(this));

        ChangeState(BossState.Idle);
    }

    public void ChangeState(BossState state)
    {
        currentState?.Exit();
        currentState = states[state];
        currentState?.Enter();
    }

    public void Update()
    {
        currentState?.Update();
    }

    public void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }
}
