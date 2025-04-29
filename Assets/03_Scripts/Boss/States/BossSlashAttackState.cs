using System.Collections;
using UnityEngine;

public class BossSlashAttackState : IEnemyState
{
    private BossStateMachine BossStateMachine;
    private Transform boss;
    private Transform player;
    private Animator animator;

    private float slashRange = 2f;
    private int slashDamage = 20;
    private float attackDelay = 0.5f;
    private float returnToIdleDelay = 1.0f;

    private bool isAttackFinished = false;

    public BossSlashAttackState(BossStateMachine stateMachine)
    {
        BossStateMachine = stateMachine;
        boss = stateMachine.transform;
        player = stateMachine.playerTransform;
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter()// 상태에 진입했을 때
    {
        Debug.Log("Boss Slash 상태 진입###");
        animator.SetBool("IsSlashing", true);
    }
    public void Exit()// 상태에서 나갈 때
    {
        Debug.Log("Boss Slash 상태 종료###");
        animator.SetBool("IsSlashing", false);
    }
    public void Update()// 매 프레임 업데이트
    {

    }
    public void FixedUpdate()// 물리 업데이트
    {

    }
    public void OnTriggerEnter2D(Collider2D other)// 트리거 충돌 감지
    {
            
    }

}