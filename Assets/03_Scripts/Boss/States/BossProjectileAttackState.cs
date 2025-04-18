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

    [SerializeField] private float projectileDelay = 0.5f; //투사체 발사 딜레이
    [SerializeField] private float returnToIdleDelay = 1.5f; //

    public BossProjectileAttackState(BossStateMachine stateMachine) //상태 생성자
    {
        BossStateMachine = stateMachine;
        boss = BossStateMachine.transform;
        player = BossStateMachine.playerTransform;
    }

    public void Enter()
    {
        attackCoolTime = 0;
        canAttack = true;

        BossStateMachine.StartCoroutine(FireProjectileCoroutine());
    }

    public void Exit()
    {
        throw new System.NotImplementedException();
    }

    public void FixedUpdate()
    {
        throw new System.NotImplementedException();
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        throw new System.NotImplementedException();
    }

    public void Update()
    {
        throw new System.NotImplementedException();
    }

    // 투사체 발사를 코루틴으로 타이밍 조절
    private IEnumerator FireProjectileCoroutine()
    {
        // 투사체를 발사하기 전 약간 대기 (애니메이션 고려 가능)
        yield return new WaitForSeconds(projectileDelay);

        if (!hasAttacked && player != null)
        {
            FireProjectile();    // 투사체 발사
            hasAttacked = true;  // 중복 발사 방지
        }

        // 공격 후 다음 상태로 넘어가기 전까지 대기
        yield return new WaitForSeconds(returnToIdleDelay);

        // Idle 또는 Move 상태로 전환
        BossStateMachine.ChangeState(BossState.Idle);
    }


    // 실제 투사체를 발사하는 로직
    private void FireProjectile()
    {
        Debug.Log("보스가 투사체를 발사!");

        //투사체 발사 로직
        if (BossStateMachine.projectilePrefab == null || BossStateMachine.firePoint == null) return;

        // 투사체 인스턴스 생성
        GameObject projectile = Object.Instantiate(
            BossStateMachine.projectilePrefab,
            BossStateMachine.firePoint.position,
            Quaternion.identity
        );

        // 방향 계산
        Vector2 direction = (player.position - boss.position).normalized;

        // Rigidbody2D 컴포넌트 가져오기
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            float projectileSpeed = 10f; // 원하는 속도로 설정
            rb.velocity = direction * projectileSpeed;
        }
    }
}
