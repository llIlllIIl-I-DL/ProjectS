using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDieState : IEnemyState
{
    BossStateMachine BossStateMachine;

    public Transform player;  // 플레이어 Transform 연결 필요
    private Transform boss;   // 이 스크립트가 붙은 오브젝트가 보스라고 가정
    private Rigidbody2D rb;

    private Animator animator;

    public BossDieState(BossStateMachine stateMachine)
    {
        BossStateMachine = stateMachine;
        boss = BossStateMachine.transform;
        player = BossStateMachine.playerTransform;
        rb = stateMachine.GetComponent<Rigidbody2D>();
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter()
    {
        Debug.Log("BossDie");
    }

    public void Exit()
    {

    }

    public void FixedUpdate()
    {

    }

    public void OnTriggerEnter2D(Collider2D other)
    {

    }

    public void Update()
    {

    }
}
