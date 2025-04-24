using System.Collections;
using UnityEngine;

public class BossProjectileAttackState : IEnemyState
{
    BossStateMachine BossStateMachine;

    private Transform boss;
    private Transform player;
    private Animator animator;

    private bool canUseChargedAttack = true; // 차징 공격 쿨타임 중 여부
    private float chargedAttackCooldown = 20f; // 차징 공격 쿨타임 (초)

    public BossProjectileAttackState(BossStateMachine stateMachine)
    {
        BossStateMachine = stateMachine;
        boss = stateMachine.transform;
        player = stateMachine.playerTransform;
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter()
    {
        Debug.Log("Boss 원거리 공격 상태 진입");

        // 30% 확률로 차징 공격 시도 (쿨타임 가능할 때만)
        float randomValue = Random.value;
        bool tryChargeAttack = randomValue <= 0.4f;

        if (tryChargeAttack && canUseChargedAttack)
        {
            BossStateMachine.StartCoroutine(FireChargedProjectileCoroutine());
        }
        else
        {
            BossStateMachine.StartCoroutine(FireNormalProjectileCoroutine());
        }
    }

    public void Exit()
    {
        Debug.Log("BossProjectileAttackState 상태 종료");
    }

    public void FixedUpdate() { }

    public void Update() { }

    public void OnTriggerEnter2D(Collider2D other) { }

    /// <summary>
    /// 일반 투사체를 일정 간격으로 발사하는 코루틴
    /// </summary>
    private IEnumerator FireNormalProjectileCoroutine()
    {
        float attackDuration = 3f;
        float fireRate = 0.8f;
        float timer = 0f;

        while (timer < attackDuration)
        {
            if (player != null)
            {
                if (animator != null)
                    animator.SetTrigger("FireProjectile");

                FireProjectile(isCharged: false);
            }

            yield return new WaitForSeconds(fireRate);
            timer += fireRate;
        }

        if (animator != null)
        {
            animator.ResetTrigger("ProjectileAttack");
            animator.ResetTrigger("FireProjectile");
        }

        BossStateMachine.ChangeState(BossState.Idle);
    }

    /// <summary>
    /// 차징 투사체를 한 번 발사하고 쿨타임을 시작하는 코루틴
    /// </summary>
    private IEnumerator FireChargedProjectileCoroutine()
    {
        Debug.Log("차징 공격 준비 중...");
        if (animator != null)
            animator.SetTrigger("ChargeAttack");

        yield return new WaitForSeconds(2f); // 차징 시간

        if (player != null)
        {
            FireProjectile(isCharged: true);
        }

        BossStateMachine.StartCoroutine(StartChargedAttackCooldown());
        BossStateMachine.ChangeState(BossState.Idle);
    }

    /// <summary>
    /// 실제 투사체를 발사하는 함수. 일반/차징 여부에 따라 속성 변경
    /// </summary>
    private void FireProjectile(bool isCharged)
    {
        Debug.Log(isCharged ? "차징 투사체 발사!" : "일반 투사체 발사!");

        if (BossStateMachine.projectilePrefab == null || BossStateMachine.firePoint == null)
            return;

        GameObject projectile = Object.Instantiate(
            BossStateMachine.projectilePrefab,
            BossStateMachine.firePoint.position,
            Quaternion.identity
        );

        Vector2 direction = (player.position - BossStateMachine.firePoint.position).normalized;
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            float speed = isCharged ? 8f : 10f;
            rb.velocity = direction * speed;
        }

        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.damage = isCharged ? 50f : 10f; // 차징 데미지 증가
            projectile.transform.localScale = isCharged ? Vector3.one * 2.5f : Vector3.one; // 차징 크기 증가
        }
    }

    /// <summary>
    /// 차징 공격 쿨타임을 관리하는 코루틴
    /// </summary>
    private IEnumerator StartChargedAttackCooldown()
    {
        canUseChargedAttack = false;
        yield return new WaitForSeconds(chargedAttackCooldown);
        canUseChargedAttack = true;
    }
}
