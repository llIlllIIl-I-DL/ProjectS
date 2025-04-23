using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSmashAttackState : IEnemyState
{

    private BossStateMachine BossStateMachine;

    private Transform boss;
    private Transform player;
    private Animator animator;

    private bool hasAttacked = false; // 한 번만 공격하도록 방지
    private float smashDelay = 0.5f; // 공격 전 딜레이
    private float returnToIdleDelay = 1.2f; // 공격 후 대기 시간

    [SerializeField] private float smashRange = 2f; // 타격 판정 범위
    [SerializeField] private int smashDamage = 20; // 타격 데미지

    private enum SmashType { Slash, Kick } // 근접 공격 타입 정의
    private SmashType currentAttackType;

    public BossSmashAttackState(BossStateMachine stateMachine)
    {
        BossStateMachine = stateMachine;
        boss = BossStateMachine.transform;
        player = BossStateMachine.playerTransform;
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter()
    {
        Debug.Log("Boss SmashAttack 상태 진입");
        hasAttacked = false;

        // 여기서 애니메이션 트리거 호출 가능: ex) animator.SetTrigger("Smash")
        BossStateMachine.StartCoroutine(SmashAttackCoroutine());

        // 애니메이션 트리거 설정
        if (animator != null)
        {
            animator.SetTrigger("ProjectileAttack");
        }
    }

    public void Exit()
    {
        Debug.Log("Boss SmashAttack 상태 종료");
    }

    public void FixedUpdate()
    {
        // 근접 공격은 이동하지 않음
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와의 충돌 감지
        if (other.CompareTag("Player") && !hasAttacked)
        {
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(smashDamage);
                hasAttacked = true;
                Debug.Log("플레이어와 충돌! 데미지: " + smashDamage);
            }
        }
    }

    public void Update()
    {
        // 공격 로직은 코루틴에서 처리하므로 별도 필요 없음
    }

    // 근접 공격 처리 코루틴
    private IEnumerator SmashAttackCoroutine()
    {
        yield return new WaitForSeconds(smashDelay);

        if (!hasAttacked && player != null)
        {
            PerformSmashAttack();
            hasAttacked = true;
        }

        yield return new WaitForSeconds(returnToIdleDelay);

        BossStateMachine.ChangeState(BossState.Idle);
    }

    // 실제 공격 판정 및 데미지 처리
    private void PerformSmashAttack()
    {
        Debug.Log("Boss Kick 공격 시도");

        // 플레이어와의 거리 체크 (정밀 판정용)
        float distance = Vector2.Distance(boss.position, player.position);
        if (distance <= smashRange)
        {
            // 예: 플레이어에게 데미지 전달
            Debug.Log("Kick 적중! 플레이어에게 데미지: " + smashDamage);

            // 예시로 플레이어에 IDamageable 인터페이스가 있다고 가정
            var damageable = player.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(smashDamage);
            }
        }
        else
        {
            Debug.Log("Boss 근접 공격 범위 밖입니다.");
        }
    }

    private void OnDrawGizmos()
    {
        // 공격 범위 시각화 (에디터에서만 보임)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(boss.position, smashRange);
    }
}
