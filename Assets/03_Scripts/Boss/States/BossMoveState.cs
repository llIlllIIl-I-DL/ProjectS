using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossMoveState : IEnemyState
{
    private BossStateMachine BossStateMachine;

    public Transform player;  // 플레이어 Transform 연결 필요
    private Transform boss;   // 보스 Transform
    private Rigidbody2D rb;

    [SerializeField] private float moveSpeed = 3f;         // 이동 속도
    [SerializeField] private float detectionRange = 10f;   // 감지 거리
    [SerializeField] private float attackRange = 5f;       // 공격 거리

    [SerializeField] private bool isGroggy = false;        // 그로기 상태 (사용 여부는 미정)

    private Animator animator;

    public BossMoveState(BossStateMachine stateMachine)
    {
        BossStateMachine = stateMachine;
        boss = BossStateMachine.transform;
        player = BossStateMachine.playerTransform;
        rb = stateMachine.GetComponent<Rigidbody2D>();
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter()
    {
        Debug.Log("Boss Move 상태 진입");
        if (animator != null)
        {
            animator.SetBool("IsMoving", true);
        }
    }

    public void Exit()
    {
        Debug.Log("Boss Move 상태 종료");
        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
        }
    }

    public void FixedUpdate()
    {
        if (player == null) return;

        float distance = Vector3.Distance(player.position, boss.position);

        // 공격 범위 안에 들어오면 이동 중지
        if (distance <= attackRange)
        {
            rb.velocity = Vector2.zero;

            if (animator != null)
            {
                animator.SetFloat("MoveSpeed", 0f);
            }
            return;
        }

        // 이동 로직
        Vector2 direction = (player.position - boss.position).normalized;
        rb.velocity = direction * moveSpeed;

        // 좌우 반전 처리
        if (direction.x != 0)
        {
            boss.localScale = new Vector3(Mathf.Sign(direction.x), 1, 1);
        }

        // 이동 애니메이션 속도 설정
        if (animator != null)
        {
            animator.SetFloat("MoveSpeed", rb.velocity.magnitude);
        }
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        // 필요 시 처리
    }

    public void Update()
    {
        if (player == null || boss == null) return;

        float distance = Vector3.Distance(boss.position, player.position);

        // 공격 범위 진입 시 상태 전환
        if (distance < attackRange)
        {
            BossStateMachine.ChangeState(BossState.SmashAttack);
        }
        else if (distance < detectionRange)
        {
            // 현재 상태 유지
        }
        else
        {
            // 탐지 범위를 벗어나면 Idle 상태로 전환
            BossStateMachine.ChangeState(BossState.Idle);
        }
    }

    //플레이어와 보스 콜라이더 충돌시 넉백 공격
    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            Rigidbody2D playerRb = collision.collider.GetComponent<Rigidbody2D>();

            if (playerRb != null)
            {
                // 보스 -> 플레이어 방향
                Vector2 knockbackDir = (collision.transform.position - boss.position).normalized;
                float knockbackForce = 10f; // 넉백 세기

                playerRb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);

                Debug.Log("플레이어 넉백 (충돌)");
            }
        }
    }
}
