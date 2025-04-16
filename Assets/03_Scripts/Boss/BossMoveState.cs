using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossMoveState : IEnemyState
{
    BossStateMachine BossStateMachine;

    public BossMoveState(BossStateMachine stateMachine)
    {
        BossStateMachine = stateMachine;
    }
    public void Enter()
    {
        throw new System.NotImplementedException();
    }

    public void Exit()
    {
        throw new System.NotImplementedException();
    }

    public void FixedUpdate()
    {
        throw new System.NotImplementedException();
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        throw new System.NotImplementedException();
    }

    public void Update()
    {
        throw new System.NotImplementedException();
    }
}
