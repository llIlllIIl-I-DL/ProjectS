using UnityEngine;
using System.Collections;

public class BossKickAttackState : IEnemyState
{
    private BossStateMachine BossStateMachine;
    private Transform boss;
    private Transform player;
    private Animator animator;

    private float kickRange = 2f;
    private int kickDamage = 30;
    private float attackDelay = 0.3f;
    private float returnToIdleDelay = 1.0f;

    private GameObject kickEffectPrefab;

    public BossKickAttackState(BossStateMachine bossStateMachine)
    {
        BossStateMachine = bossStateMachine;
        boss = bossStateMachine.transform;
        player = bossStateMachine.playerTransform;
        animator = bossStateMachine.GetComponent<Animator>();
    }

    public void Enter() // 상태에 진입했을 때
    {
        if (animator != null)
        {
        Debug.Log("Boss Kick 상태 진입@@@@");
        animator.SetBool("IsKicking", true);
        BossStateMachine.StartCoroutine(KickAttackCoroutine());
        }
    }

    public void Exit()// 상태에서 나갈 때
    {
        Debug.Log("Boss Kick 상태 종료@@@@");
        animator.SetBool("IsKicking", false);
    }

    public void Update()// 매 프레임 업데이트
    {

    }

    public void FixedUpdate()// 물리 업데이트
    {

    }

    public void OnTriggerEnter2D(Collider2D other)// 트리거 충돌 감지
    {

    }

    private IEnumerator KickAttackCoroutine()//상태 진입은 되는데 코루틴 실행이 안 됨 왜인지는 모르겠음
    {
        yield return new WaitForSeconds(attackDelay);

        if (player != null && Vector2.Distance(boss.position, player.position) <= kickRange)
        {
            var damageable = player.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(kickDamage);
                Debug.Log("Kick 데미지: " + kickDamage + "@@@");
            }

            if (kickEffectPrefab != null)
            {
                GameObject effect = Object.Instantiate(kickEffectPrefab, player.position, Quaternion.identity);
                Object.Destroy(effect, 1f);
            }
        }

        yield return new WaitForSeconds(returnToIdleDelay);

        if (BossStateMachine != null && BossStateMachine.currentState is BossKickAttackState)
        {
            BossStateMachine.ChangeState(BossState.Idle);
        }
    }

}