using System.Collections;
using UnityEngine;

public class BossProjectileAttackState : IEnemyState
{
    BossStateMachine bossStateMachine;

    private Transform boss;
    private Transform player;
    private Animator animator;

    private bool canUseChargedAttack = true;
    private float chargedAttackCooldown = 20f;
    private bool isAttacking = false; // 중복 공격 방지용

    public BossProjectileAttackState(BossStateMachine stateMachine)
    {
        bossStateMachine = stateMachine;
        boss = stateMachine.transform;
        player = stateMachine.playerTransform;
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter()
    {
        Debug.Log("Boss 원거리 공격 상태 진입");

        if (isAttacking)
            return;

        isAttacking = true;

        bool tryChargeAttack = Random.value <= 0.4f;

        if (tryChargeAttack && canUseChargedAttack)
        {
            bossStateMachine.StartCoroutine(FireChargedProjectileCoroutine());
        }
        else
        {
            bossStateMachine.StartCoroutine(FireNormalProjectileCoroutine());
        }
    }

    public void Exit()
    {
        Debug.Log("BossProjectileAttackState 상태 종료");
        isAttacking = false;
    }

    public void FixedUpdate() { }
    public void Update() { }
    public void OnTriggerEnter2D(Collider2D other) { }

    private IEnumerator FireNormalProjectileCoroutine()
    {
        float attackDuration = 3f;
        float fireRate = 0.8f;
        float timer = 0f;

        while (timer < attackDuration)
        {
            if (player != null)
            {
                animator?.SetTrigger("FireProjectile");
                FireProjectile(false);
            }

            yield return new WaitForSeconds(fireRate);
            timer += fireRate;
        }

        animator?.ResetTrigger("FireProjectile");

        bossStateMachine.ChangeState(BossState.Idle);
    }

    private IEnumerator FireChargedProjectileCoroutine()
    {
        Debug.Log("차징 공격 준비 중...");
        animator?.SetTrigger("ChargeAttack");

        yield return new WaitForSeconds(2f);

        if (player != null)
        {
            FireProjectile(true);
        }

        bossStateMachine.StartCoroutine(StartChargedAttackCooldown());
        bossStateMachine.ChangeState(BossState.Idle);
    }

    private void FireProjectile(bool isCharged)
    {
        if (bossStateMachine.projectilePrefab == null || bossStateMachine.firePoint == null)
        {
            Debug.LogWarning("ProjectilePrefab 또는 FirePoint가 연결되지 않았습니다.");
            return;
        }

        GameObject projectile = Object.Instantiate(
            bossStateMachine.projectilePrefab,
            bossStateMachine.firePoint.position,
            Quaternion.identity
        );

        Vector2 direction = (player.position - bossStateMachine.firePoint.position).normalized;
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            float speed = isCharged ? 8f : 10f;
            rb.velocity = direction * speed;
        }
        else
        {
            Debug.LogWarning("Projectile에 Rigidbody2D가 없습니다.");
        }

        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.damage = isCharged ? 50f : 10f;
            projectile.transform.localScale = isCharged ? Vector3.one * 2.5f : Vector3.one;
        }
    }

    private IEnumerator StartChargedAttackCooldown()
    {
        canUseChargedAttack = false;
        yield return new WaitForSeconds(chargedAttackCooldown);
        canUseChargedAttack = true;
    }
}
