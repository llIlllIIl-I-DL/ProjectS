using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossProjectileAttackState : IEnemyState
{
    BossStateMachine BossStateMachine;

    protected float attackCoolTime; //공격 쿨타임
    protected bool canAttack = false; //공격 할 수 있는지
    private bool hasAttacked = false; //투사체가 연사 되지 않도록 하기 위함

    private Transform boss;
    private Transform player;

    [SerializeField] private float detectionRange = 10f; //보스가 플레이어를 감지할 수 있는 거리
    [SerializeField] private float attackRange = 5f; //보스공격 기준 거리

    [SerializeField] private float projectileDelay = 0.01f; //투사체 발사 딜레이
    [SerializeField] private float returnToIdleDelay = 0.5f; //

    private Animator animator;

    public BossProjectileAttackState(BossStateMachine stateMachine) //상태 생성자
    {
        BossStateMachine = stateMachine;
        boss = BossStateMachine.transform;
        player = BossStateMachine.playerTransform;
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter()
    {
        Debug.Log("Boss 원거리 공격 상태 진입");
        attackCoolTime = 0;
        canAttack = true;

        // 애니메이션 트리거 설정 이후 추가
        //if (animator != null)
        //{
        //    animator.SetTrigger("ProjectileAttack");
        //}

        BossStateMachine.StartCoroutine(FireProjectileCoroutine());
    }

    public void Exit()
    {
        Debug.Log("BossProjectileAttackState 상태 종료");
    }

    public void FixedUpdate()
    {
        // 필요시 구현
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        //필요시 구현
    }

    public void Update()
    {

    }

    // 투사체 발사를 코루틴으로 타이밍 조절
    private IEnumerator FireProjectileCoroutine()
    {
        float attackDuration = 3f; // 총 공격 지속 시간
        float fireRate = 0.8f;     // 발사 간격
        float timer = 0f;

        while (timer < attackDuration)
        {
            if (player != null)
            {
                if (animator != null)
                {
                    animator.SetTrigger("FireProjectile");
                }

                FireProjectile();
            }

            yield return new WaitForSeconds(fireRate);
            timer += fireRate;
        }

        // 애니메이션 리셋
        if (animator != null)
        {
            animator.ResetTrigger("ProjectileAttack");
            animator.ResetTrigger("FireProjectile");
        }

        BossStateMachine.ChangeState(BossState.Idle);
    }


    // 실제 투사체를 발사하는 로직
    private void FireProjectile()
    {
        Debug.Log("Boss 투사체 발사!");

        // 발사에 필요한 프리팹과 발사 위치가 설정되지 않았다면 실행하지 않음
        if (BossStateMachine.projectilePrefab == null || BossStateMachine.firePoint == null) return;

        // 투사체 인스턴스 생성
        // firePoint 위치에서 projectilePrefab을 생성 (회전은 기본값 Quaternion.identity)
        GameObject projectile = Object.Instantiate(
            BossStateMachine.projectilePrefab,
            BossStateMachine.firePoint.position,
            Quaternion.identity
        );

        // 방향 계산
        Vector2 direction = (player.position - BossStateMachine.firePoint.position).normalized;

        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float projectileSpeed = 10f; // 투사체의 속도를 설정 (값은 상황에 맞게 조정 가능)

            // 계산한 방향 벡터에 속도를 곱해 velocity에 적용 → 투사체가 해당 방향으로 날아감
            rb.velocity = direction * projectileSpeed;
        }
    }
}
