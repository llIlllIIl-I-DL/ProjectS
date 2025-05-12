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
        Debug.Log("Boss Kick 상태 진입####");
        animator.SetBool("IsKicking", true);
        BossStateMachine.StartCoroutine(PerformKickAfterDelay());
        }
    }

    public void Exit()// 상태에서 나갈 때
    {
        Debug.Log("Boss Kick 상태 종료####");
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

    // 공격 딜레이 후 데미지 판정만
    private IEnumerator PerformKickAfterDelay()
    {
        yield return new WaitForSeconds(attackDelay);

        if (player != null && Vector2.Distance(boss.position, player.position) <= kickRange)
        {
            var damageable = player.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(kickDamage);
                Debug.Log("Kick 데미지: " + kickDamage + "###");
            }
        }
    }

    // 이 메서드는 애니메이션 이벤트에서 호출됨
    public void OnKickAnimationEnd()
    {
        if (BossStateMachine != null && BossStateMachine.currentState is BossKickAttackState)
        {
            BossStateMachine.ChangeState(BossState.Idle);
        }
    }

}