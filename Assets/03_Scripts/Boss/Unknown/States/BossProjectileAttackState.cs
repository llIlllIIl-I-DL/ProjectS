using System.Collections;
using UnityEngine;

public class BossProjectileAttackState : IEnemyState
{
    private readonly BossStateMachine stateMachine;
    private readonly Transform bossTransform;
    private readonly Transform playerTransform;
    private readonly Animator animator;
    private readonly Rigidbody2D rb;

    private bool isCoroutineRunning = false;
    private bool canUseChargedAttack = true;
    private int projectileCount = 0;

    public BossProjectileAttackState(BossStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        bossTransform = stateMachine.transform;
        playerTransform = stateMachine.playerTransform;
        animator = stateMachine.GetComponent<Animator>();
        rb = stateMachine.GetComponent<Rigidbody2D>();
    }

    public void Enter()
    {
        Debug.Log("Boss ProjectileAttack 상태 진입");

        // 이동 중지
        StopMovement();

        // 이미 코루틴이 실행 중이면 중복 실행 방지
        if (isCoroutineRunning) return;
        isCoroutineRunning = true;

        // 확률적으로 차지 공격 선택 (40% 확률)
        bool useCharged = Random.value <= 0.4f;

        // 차지 공격이 가능하면 차지 공격, 아니면 일반 공격
        if (useCharged && canUseChargedAttack)
        {
            stateMachine.StartCoroutine(FireChargedCoroutine());
        }
        else
        {
            stateMachine.StartCoroutine(FireNormalCoroutine());
        }
    }

    public void Exit()
    {
        isCoroutineRunning = false;
        projectileCount = 0;
        Debug.Log("Boss ProjectileAttack 상태 종료");
    }

    public void Update()
    {
        // 플레이어가 null이면 상태 전환
        if (playerTransform == null)
        {
            if (!isCoroutineRunning)
            {
                Debug.LogWarning("Update - 플레이어 트랜스폼이 null입니다. 대기 상태로 전환합니다.");
                stateMachine.ChangeState(BossState.Idle);
            }
            return;
        }

        // 플레이어 방향으로 보스 방향 설정
        UpdateBossDirection();

        // 거리에 따른 상태 전환 체크
        CheckDistanceForStateChange();
    }

    public void FixedUpdate() { }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(GameConstants.Tags.PLAYER)) return;

        if (stateMachine.CanKick)
        {
            Debug.Log("Kick 공격 진입");
            stateMachine.ChangeState(BossState.KickAttack);
        }
        else
        {
            float remainingTime = stateMachine.KickCooldown - (Time.time - stateMachine.LastKickTime);
            Debug.Log($"Kick 쿨다운 중... 남은 시간: {remainingTime:F1}초");
        }
    }

    private void UpdateBossDirection()
    {
        if (playerTransform == null)
        {
            return; // 플레이어가 없으면 방향 업데이트 중단
        }

        float directionX = playerTransform.position.x - bossTransform.position.x;
        if (directionX != 0)
            bossTransform.localScale = new Vector3(Mathf.Sign(directionX), 1f, 1f);
    }

    private void CheckDistanceForStateChange()
    {
        if (playerTransform == null || isCoroutineRunning) return;

        float distance = Vector2.Distance(playerTransform.position, bossTransform.position);

        // 근접 공격 범위에 들어오면 슬래시 공격으로 전환
        if (distance <= GameConstants.Boss.ATTACK_RANGE)
        {
            stateMachine.ChangeState(BossState.SlashAttack);
        }
        // 감지 범위를 벗어나면 이동 상태로 전환
        else if (distance > GameConstants.Boss.DETECTION_RANGE)
        {
            stateMachine.ChangeState(BossState.Move);
        }
    }

    private void StopMovement()
    {
        if (rb != null) rb.velocity = Vector2.zero;
    }

    private IEnumerator FireNormalCoroutine()
    {
        projectileCount = 0;
        float attackInterval = 0.8f; // 일반 공격 간격

        while (projectileCount < GameConstants.Boss.MAX_PROJECTILE_COUNT)
        {
            if (playerTransform == null)
            {
                Debug.LogWarning("플레이어 트랜스폼이 null입니다. 일반 공격을 중단합니다.");
                EndProjectilePhase();
                yield break;
            }

            // 애니메이션 트리거
            animator?.SetTrigger(GameConstants.AnimParams.FIRE_PROJECTILE);

            // 투사체 발사
            FireProjectile(isCharged: false);
            projectileCount++;

            // 공격 간격 대기
            yield return new WaitForSeconds(attackInterval);

            // 거리 체크하여 너무 가까워졌으면 중단
            float distance = Vector2.Distance(playerTransform.position, bossTransform.position);
            if (distance <= GameConstants.Boss.ATTACK_RANGE)
            {
                break;
            }
        }

        EndProjectilePhase();
    }

    private IEnumerator FireChargedCoroutine()
    {
        Debug.Log("차지 공격 준비...");

        // 차지 애니메이션 트리거
        animator?.SetTrigger(GameConstants.AnimParams.CHARGE_ATTACK);

        // 차지 시간 대기
        yield return new WaitForSeconds(2f);

        // 플레이어 트랜스폼 확인
        if (playerTransform == null)
        {
            Debug.LogWarning("플레이어 트랜스폼이 null입니다. 차지 공격을 중단하고 상태를 전환합니다.");
            EndProjectilePhase();
            yield break;
        }

        // 차지 공격 발사
        FireProjectile(isCharged: true);

        // 차지 공격 쿨다운 시작
        stateMachine.StartCoroutine(ChargedAttackCooldown());

        // 공격 후 잠시 대기
        yield return new WaitForSeconds(0.5f);

        // 상태 전환
        EndProjectilePhase();
    }

    private void FireProjectile(bool isCharged)
    {
        if (stateMachine.projectilePrefab == null || stateMachine.firePoint == null)
        {
            Debug.LogWarning("투사체 프리팹 또는 발사 지점이 설정되지 않았습니다.");
            return;
        }

        // 플레이어 트랜스폼 확인
        if (playerTransform == null)
        {
            Debug.LogWarning("플레이어 트랜스폼이 null입니다. 투사체 발사를 중단합니다.");
            return;
        }

        // 투사체 생성
        GameObject projObj = Object.Instantiate(
            stateMachine.projectilePrefab,
            stateMachine.firePoint.position,
            Quaternion.identity
        );

        // 플레이어 방향으로 투사체 발사
        Vector2 direction = (playerTransform.position - stateMachine.firePoint.position).normalized;
        Rigidbody2D projRb = projObj.GetComponent<Rigidbody2D>();

        if (projRb != null)
        {
            float speed = isCharged
                ? GameConstants.Boss.CHARGED_PROJECTILE_SPEED
                : GameConstants.Boss.NORMAL_PROJECTILE_SPEED;

            projRb.velocity = direction * speed;
        }

        // 투사체 속성 설정
        var projectile = projObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.damage = isCharged
                ? GameConstants.Boss.CHARGED_PROJECTILE_DAMAGE
                : GameConstants.Boss.NORMAL_PROJECTILE_DAMAGE;

            // 차지 공격은 크기가 더 큼
            projObj.transform.localScale = isCharged ? Vector3.one * 2.5f : Vector3.one;

            // 차지 공격은 관통 가능
            projectile.isPiercing = isCharged;
            
            Debug.Log($"{(isCharged ? "차지" : "일반")} 투사체 발사: 데미지 {projectile.damage}");
        }
    }

    private IEnumerator ChargedAttackCooldown()
    {
        canUseChargedAttack = false;
        Debug.Log($"차지 공격 쿨다운 시작: {GameConstants.Boss.CHARGED_ATTACK_COOLDOWN}초");
        yield return new WaitForSeconds(GameConstants.Boss.CHARGED_ATTACK_COOLDOWN);
        canUseChargedAttack = true;
        Debug.Log("차지 공격 쿨다운 완료");
    }

    private void EndProjectilePhase()
    {
        isCoroutineRunning = false;

        // 공격 후 빠른 추격 모드 활성화
        stateMachine.isFastChasingAfterProjectile = true;

        // 플레이어가 감지 범위 내에 있으면 이동 상태로, 아니면 대기 상태로
        if (playerTransform != null)
        {
            float distance = Vector2.Distance(playerTransform.position, bossTransform.position);

            if (distance <= GameConstants.Boss.ATTACK_RANGE)
            {
                stateMachine.ChangeState(BossState.SlashAttack);
            }
            else
            {
                stateMachine.ChangeState(BossState.Move);
            }
        }
        else
        {
            Debug.LogWarning("EndProjectilePhase - 플레이어 트랜스폼이 null입니다. 대기 상태로 전환합니다.");
            stateMachine.ChangeState(BossState.Idle);
        }
    }
}