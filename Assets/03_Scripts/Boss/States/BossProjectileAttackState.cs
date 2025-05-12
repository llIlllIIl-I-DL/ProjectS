using System.Collections;
using UnityEngine;

public class BossProjectileAttackState : IEnemyState
{
    private BossStateMachine bossStateMachine;

    private Transform bossTransform;
    private Transform playerTransform;
    private Animator animator;

    [SerializeField] private float detectionRange = 10f; //플레이어를 감지하는 거리
    [SerializeField] private float attackRange = 5f; //근접 공격 거리

    private bool canUseChargedAttack = true;
    private float chargedAttackCooldown = 20f;
    private bool isAttacking = false; // 중복 공격 방지용
    private bool isCoroutineRunning = false; // 코루틴 중복 실행 방지용

    private int projectileAttackCount = 0; // 발사한 투사체 횟수 추적용

    public BossProjectileAttackState(BossStateMachine stateMachine)
    {
        bossStateMachine = stateMachine;
        bossTransform = stateMachine.transform;
        playerTransform = stateMachine.playerTransform;
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter()
    {
        Debug.Log("Boss 원거리 공격 상태 진입");

        // [추가] 이동 정지 (투사체 공격 중엔 이동 X)
        var rb = bossStateMachine.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;

        // 코루틴이 실행 중이면 Enter를 종료
        if (isCoroutineRunning)
            return;

        // 코루틴이 실행 중이 아니면 시작
        isCoroutineRunning = true;

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
        isCoroutineRunning = false; // 상태 종료 시 코루틴 실행 상태 리셋
        isAttacking = false;
    }

    public void FixedUpdate()
    {
        // [추가] 지속적으로 이동 차단 유지
        var rb = bossStateMachine.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = Vector2.zero;
    }

    public void Update() { }

    public void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"{other.name} {other.tag} @@@");

        if (other.CompareTag("Player"))
        {
            Debug.Log($"{bossStateMachine.CanKick} @@@");
            if (bossStateMachine.CanKick)
            {
                Debug.Log("Kick 공격 진입!@@@");
                bossStateMachine.ChangeState(BossState.KickAttack);
            }
            else
            {
                float remain = bossStateMachine.KickCooldown - (Time.time - bossStateMachine.LastKickTime);
                Debug.Log($"Kick 쿨다운 중... 남은 시간: {remain:F1}초@@@");
            }
        }
    }

    private IEnumerator FireNormalProjectileCoroutine()
    {
        float attackDuration = 3f;
        float fireRate = 0.8f;
        float timer = 0f;

        while (timer < attackDuration)
        {
            if (playerTransform != null)
            {
                animator?.SetTrigger("FireProjectile");
                FireProjectile(false);
                projectileAttackCount++; // 투사체 발사 횟수 증가
            }

            yield return new WaitForSeconds(fireRate);
            timer += fireRate;

            if (projectileAttackCount >= 3) // 투사체 3번 발사 후 추적 상태로 전환
            {
                // [수정] 빠른 추적 활성화 및 상태 변경
                bossStateMachine.isFastChasingAfterProjectile = true;
                bossStateMachine.ChangeState(BossState.Move);  // Move 상태로 전환
                break; // 반복문 종료
            }
        }

        animator?.ResetTrigger("FireProjectile");

        // 코루틴 종료 후 상태 전환
        if (projectileAttackCount < 3)
        {
            bossStateMachine.isFastChasingAfterProjectile = true; // 빠른 추적 모드 활성화
            Debug.Log("빠른 추적###");
            bossStateMachine.ChangeState(BossState.Move); // Move 상태로 전환
        }
        isCoroutineRunning = false; // 코루틴 종료 후 중복 실행 방지
    }


    private IEnumerator FireChargedProjectileCoroutine()
    {
        Debug.Log("차징 공격 준비 중...");
        animator?.SetTrigger("ChargeAttack");

        yield return new WaitForSeconds(2f);

        if (playerTransform != null)
        {
            FireProjectile(true);
        }

        bossStateMachine.StartCoroutine(StartChargedAttackCooldown());
        bossStateMachine.ChangeState(BossState.Idle);

        isCoroutineRunning = false; // 코루틴 종료 후 중복 실행 방지
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

        Vector2 direction = (playerTransform.position - bossStateMachine.firePoint.position).normalized;
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
