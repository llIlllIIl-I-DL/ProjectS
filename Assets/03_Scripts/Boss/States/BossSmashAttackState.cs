using System.Collections;
using UnityEngine;

public class BossSmashAttackState : IEnemyState
{
    private BossStateMachine BossStateMachine;

    private Transform boss;
    private Transform player;
    private Animator animator;

    private bool hasAttacked = false;
    private float smashDelay = 0.5f;
    private float returnToIdleDelay = 1.2f;

    [SerializeField] private float smashRange = 2f;
    [SerializeField] private int smashDamage = 20;
    [SerializeField] private int kickDamage = 10;
    [SerializeField] private float knockbackForce = 10f;

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
        BossStateMachine.StartCoroutine(SmashAttackCoroutine());

        if (animator != null)
        {
            animator.SetBool("IsSlashing", true); // 슬래시 애니메이션 활성화
            animator.SetBool("IsKicking", false); // 혹시 킥이 켜져 있으면 끔
        }
    }

    public void Exit()
    {
        Debug.Log("Boss SmashAttack 상태 종료");
        if (animator != null)
        {
            animator.SetBool("IsSlashing", false); // 슬래시 끝나면 비활성화
            animator.SetBool("IsKicking", false);  // 킥도 혹시 모르니 비활성화
        }
    }

    public void FixedUpdate() { }
    public void Update() { }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (hasAttacked) return;

            Debug.Log("플레이어와 충돌 - 킥 공격 실행");
            hasAttacked = true;

            if (animator != null)
            {
                animator.SetBool("IsKicking", true);  // 킥 애니메이션 활성화
                animator.SetBool("IsSlashing", false); // 슬래시 비활성화
            }

            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(kickDamage);
                Debug.Log("Kick 데미지: " + kickDamage);
            }

            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = (other.transform.position - boss.position).normalized;
                rb.velocity = Vector2.zero;
                rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
                Debug.Log("킥 넉백 적용");
            }

            if (BossStateMachine.kickEffectPrefab != null)
            {
                GameObject effect = Object.Instantiate(
                    BossStateMachine.kickEffectPrefab,
                    other.transform.position,
                    Quaternion.identity
                );
                Object.Destroy(effect, 1f);
            }
        }
    }

    private IEnumerator SmashAttackCoroutine()
    {
        yield return new WaitForSeconds(smashDelay);

        if (!hasAttacked && player != null)
        {
            PerformSlashAttack();
            hasAttacked = true;
        }

        yield return new WaitForSeconds(returnToIdleDelay);
        BossStateMachine.ChangeState(BossState.Idle);
    }

    private void PerformSlashAttack()
    {
        float distance = Vector2.Distance(boss.position, player.position);
        if (distance <= smashRange)
        {
            Debug.Log("휘두르기 적중 - 데미지: " + smashDamage);

            var damageable = player.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(smashDamage);
            }
        }
        else
        {
            Debug.Log("플레이어가 휘두르기 범위 밖에 있음");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (boss != null)
            Gizmos.DrawWireSphere(boss.position, smashRange);
    }
}
