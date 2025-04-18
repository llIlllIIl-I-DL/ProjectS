using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossIdleState : IEnemyState
{

    BossStateMachine BossStateMachine;
    private Transform bossTransform;
    private Transform playerTransform;

    [SerializeField] private float detectionRange = 10f; // 보스가 플레이어를 감지할 수 있는 거리
    [SerializeField] private float attackRange = 5f;     // 보스 근거리 공격 기준 거리

    public BossIdleState(BossStateMachine stateMachine)
    {
        BossStateMachine = stateMachine;

        //보스와 플레이어의 Transform을 생성자에서 가져와서 초기화
        bossTransform = stateMachine.transform;
        playerTransform = stateMachine.playerTransform;
    }

    public void Enter()
    {
        Debug.Log("Boss Idle 상태 진입");
        // 보스 대기 또는 등장 연출 등 처리 가능
    }

    public void Exit()
    {
        Debug.Log("Idle 상태 종료");
    }

    public void FixedUpdate()
    {
        // Idle 상태에서는 별도 움직임 없으므로 생략 가능
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        // 필요 시 구현 (예: 공격 판정 등)
    }

    public void Update()
    {
        if (playerTransform == null || bossTransform == null) return;

        //플레이어와 보스 사이 거리 측정
        float distance = Vector3.Distance(playerTransform.position, bossTransform.position);
        Debug.Log("플레이어와 보스 사이 거리: " + distance);

        // 거리 기반 상태 전환 로직
        if (distance >= detectionRange)
        {
            Debug.Log("Move 상태로 전환");
            BossStateMachine.ChangeState(BossState.Move);
        }
        else if (distance < detectionRange && distance >= attackRange)
        {
            Debug.Log("ProjectileAttack 상태로 전환");
            BossStateMachine.ChangeState(BossState.ProjectileAttack);
        }
        else
        {
            Debug.Log("SmashAttack 상태로 전환");
            BossStateMachine.ChangeState(BossState.SmashAttack);
        }
    }
}
