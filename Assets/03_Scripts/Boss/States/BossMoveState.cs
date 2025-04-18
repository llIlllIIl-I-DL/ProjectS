using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossMoveState : IEnemyState
{
    BossStateMachine BossStateMachine;
    private Transform bossTransform;
    private Transform playerTransform;

    public Transform player;  // 플레이어 Transform 연결 필요
    private Transform boss;   // 이 스크립트가 붙은 오브젝트가 보스라고 가정

    [SerializeField] private float moveSpeed = 3f;        //이동 속도
    [SerializeField] private float detectionRange = 10f; //보스가 플레이어를 감지할 수 있는 거리
    [SerializeField] private float attackRange = 5f; //보스공격 기준 거리

    [SerializeField] private bool isGroggy = false;


    public BossMoveState(BossStateMachine stateMachine)
    {
        BossStateMachine = stateMachine;
        boss = BossStateMachine.transform;
        player = BossStateMachine.playerTransform;
    }
    public void Enter()
    {
        Debug.Log("Move 상태 진입");
    }

    public void Exit()
    {
        Debug.Log("Move 상태 종료");
    }

    public void FixedUpdate()
    {
        if (player == null || boss == null) return;

        //이동 방향 계산
        Vector2 direction = (player.position - boss.position).normalized;
        boss.position += (Vector3)direction * moveSpeed * Time.fixedDeltaTime;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        throw new System.NotImplementedException();
    }

    public void Update()
    {
        if (player == null || boss == null) return;

        float distance = Vector3.Distance(boss.position, player.position);

        //이동 중에도 거리 체크해서 공격 상태로 전환
        if (distance < attackRange)
        {
            BossStateMachine.ChangeState(BossState.SmashAttack);
            return;
        }
        else if (distance >= attackRange)
        {
            BossStateMachine.ChangeState(BossState.ProjectileAttack);
            return;
        }
    }
}
