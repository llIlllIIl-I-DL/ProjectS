using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDieState : IEnemyState
{
    private BossStateMachine BossStateMachine;
    private Transform boss;
    private Animator animator;
    private Rigidbody2D rb;

    public BossDieState(BossStateMachine stateMachine)
    {
        BossStateMachine = stateMachine;
        boss = BossStateMachine.transform;
        animator = stateMachine.GetComponent<Animator>();
        rb = stateMachine.GetComponent<Rigidbody2D>();
    }

    public void Enter()
    {
        Debug.Log("Boss 사망 상태 진입");
        
        // 물리 효과 비활성화
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }

        // 사망 애니메이션 재생
        if (animator != null)
        {
            animator.SetTrigger("setDie");
        }

        // 보스 오브젝트 비활성화 (또는 파괴)
        //BossStateMachine.gameObject.SetActive(false);
    }

    public void Exit()
    {
        Debug.Log("Boss 사망 상태 종료");
    }

    public void Update()
    {
        // 사망 상태에서는 아무것도 하지 않음
    }

    public void FixedUpdate()
    {
        // 사망 상태에서는 아무것도 하지 않음
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        // 사망 상태에서는 충돌 무시
    }
}
