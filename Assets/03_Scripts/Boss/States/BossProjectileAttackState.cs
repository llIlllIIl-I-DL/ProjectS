using System.Collections;
using UnityEngine;

public class BossProjectileAttackState : IEnemyState
{
    BossStateMachine BossStateMachine;

    private Transform boss;
    private Transform player;
    private Animator animator;

    private bool canChargeAttack = true;  // 차징 공격 가능 여부
    [SerializeField] private float chargeAttackCooldown = 5f; // 차징 공격 후 쿨타임
    [SerializeField] private float chargeTime = 1.5f;          // 차징 공격을 위해 기다릴 시간

    // 생성자
    public BossProjectileAttackState(BossStateMachine stateMachine)
    {
        BossStateMachine = stateMachine;
        boss = BossStateMachine.transform;
        player = BossStateMachine.playerTransform;
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter()
    {
        Debug.Log("Boss 원거리 공격 상태 진입");
        // 원거리 공격을 시작하는 코루틴 실행
        BossStateMachine.StartCoroutine(FireProjectileCoroutine());
    }

    public void Exit()
    {
        Debug.Log("BossProjectileAttackState 상태 종료");
    }

    public void FixedUpdate() { }

    public void Update() { }

    public void OnTriggerEnter2D(Collider2D other) { }

    /// <summary>
    /// 일반 투사체를 일정 시간 동안 반복 발사하는 코루틴
    /// </summary>
    private IEnumerator FireProjectileCoroutine()
    {
        float attackDuration = 3f;   // 공격 지속 시간
        float fireRate = 0.8f;       // 발사 간격
        float timer = 0f;            // 타이머

        while (timer < attackDuration)
        {
            if (player != null)
            {
                // 투사체 발사 애니메이션 실행
                animator?.SetTrigger("FireProjectile");
                FireProjectile(strong: false);  // 일반 투사체 발사
            }

            yield return new WaitForSeconds(fireRate);  // 지정된 간격으로 대기
            timer += fireRate;
        }

        // 일정 시간 후 차징 공격을 시도
        TryFireChargedProjectile();
    }

    /// <summary>
    /// 차징 공격을 시도하는 함수 (쿨타임 체크)
    /// </summary>
    private void TryFireChargedProjectile()
    {
        if (!canChargeAttack)  // 차징 공격이 가능하면
        {
            Debug.Log("차징 공격 쿨타임 중입니다.");
            BossStateMachine.ChangeState(BossState.Idle);  // 원래 상태로 복귀
            return;
        }

        // 차징 공격을 위한 코루틴 실행
        BossStateMachine.StartCoroutine(FireChargedProjectileCoroutine());
    }

    /// <summary>
    /// 차징 공격을 실행하는 코루틴
    /// </summary>
    private IEnumerator FireChargedProjectileCoroutine()
    {
        canChargeAttack = false;  // 차징 공격 시작 후 쿨타임 진행 중

        Debug.Log("차징 시작...");
        yield return new WaitForSeconds(chargeTime);  // 차징 시간 동안 대기

        if (player != null)
        {
            // 차징 투사체 발사 애니메이션 실행
            animator?.SetTrigger("FireChargedProjectile");
            FireProjectile(strong: true);  // 강력한 차징 투사체 발사
        }

        // 차징 공격 쿨타임 후 차징 공격을 다시 시도할 수 있게 설정
        yield return new WaitForSeconds(chargeAttackCooldown);
        canChargeAttack = true;
        Debug.Log("차징 공격 쿨타임 종료");

        BossStateMachine.ChangeState(BossState.Idle);  // 상태를 Idle로 전환
    }

    /// <summary>
    /// 투사체 발사 함수 (일반/강력 구분)
    /// </summary>
    /// <param name="strong">강력한 투사체인지 여부</param>
    private void FireProjectile(bool strong)
    {
        if (BossStateMachine.projectilePrefab == null || BossStateMachine.firePoint == null) return;

        // 투사체 프리팹을 생성
        GameObject projectile = Object.Instantiate(
            BossStateMachine.projectilePrefab,
            BossStateMachine.firePoint.position,
            Quaternion.identity
        );

        // 플레이어 방향 계산
        Vector2 direction = (player.position - BossStateMachine.firePoint.position).normalized;
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            // 일반/차징 공격에 따라 속도 설정
            float speed = strong ? 15f : 10f;
            rb.velocity = direction * speed;  // 속도 적용
        }

        // 투사체 스크립트에서 데미지 및 관통 여부 설정
        Projectile projectileScript = projectile.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.damage = strong ? 50f : 10f;  // 차징 공격은 데미지 증가
            projectileScript.isPiercing = strong;  // 차징 공격은 관통 가능

            // 차징 공격일 경우 크기 증가
            if (strong)
            {
                projectile.transform.localScale = new Vector3(3f, 3f, 1f);  // 크기 증가시킴
            }
        }

        Debug.Log(strong ? "강력한 투사체 발사!" : "일반 투사체 발사!");  // 로그 출력
    }
}
