using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossIdleState : IEnemyState
{

    BossStateMachine BossStateMachine;

    public BossIdleState(BossStateMachine stateMachine)
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
        //아이들 상태가 됐을때 어떤 작업이 계속 될지
        //ex)거리 체크

        BossStateMachine.ChangeState(BossState.Move);
    }
}
