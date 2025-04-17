using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossIdleState : IEnemyState
{

    BossStateMachine BossStateMachine;

    public Transform player;  // 플레이어 Transform 연결 필요
    private Transform boss;   // 이 스크립트가 붙은 오브젝트가 보스라고 가정

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

        float distance = Vector3.Distance(player.position, boss.position);
        Debug.Log("플레이어와 보스 사이 거리: " + distance);

        // 예시: 거리가 5 이하면 공격 시작
        if (distance < 5f)
        {
            Debug.Log("플레이어가 가까움! 공격 시작!");
        }

        BossStateMachine.ChangeState(BossState.Move);
    }
}
