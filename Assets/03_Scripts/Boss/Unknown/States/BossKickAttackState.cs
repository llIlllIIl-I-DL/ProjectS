using UnityEngine;
using System.Collections;

public class BossKickAttackState : IEnemyState
{
    private readonly BossStateMachine stateMachine;
    private readonly Transform bossTransform;
    private readonly Transform playerTransform;
    private readonly Animator animator;

    public BossKickAttackState(BossStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        bossTransform = stateMachine.transform;
        playerTransform = stateMachine.playerTransform;
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter()
    {
        if (animator == null) return;
        
        Debug.Log("Boss Kick 상태 진입");
        animator.SetBool(GameConstants.AnimParams.IS_KICKING, true);
        
        // 킥 쿨다운 갱신
        stateMachine.UpdateKickCooldown();
        
        // 킥 공격 수행
        stateMachine.StartCoroutine(PerformKickAfterDelay());
    }

    public void Exit()
    {
        Debug.Log("Boss Kick 상태 종료");
        animator.SetBool(GameConstants.AnimParams.IS_KICKING, false);
    }

    public void Update() { }

    public void FixedUpdate() { }

    public void OnTriggerEnter2D(Collider2D other) { }

    // 공격 딜레이 후 데미지 판정
    private IEnumerator PerformKickAfterDelay()
    {
        yield return new WaitForSeconds(GameConstants.Boss.KICK_ATTACK_DELAY);

        if (playerTransform != null && 
            Vector2.Distance(bossTransform.position, playerTransform.position) <= GameConstants.Boss.KICK_RANGE)
        {
            var damageable = playerTransform.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(GameConstants.Boss.KICK_DAMAGE);
                Debug.Log($"Kick 데미지: {GameConstants.Boss.KICK_DAMAGE}");
            }
        }
    }
}