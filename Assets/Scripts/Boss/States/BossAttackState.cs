using UnityEngine;

namespace BossFSM
{
    public class BossAttackState : BossState
    {
    private float attackCooldown = 2f;
    private float attackTimer;
    private float attackRange = 3f;
    private Transform player;

    public BossAttackState(BossStateMachine stateMachine, Boss boss) : base(stateMachine, boss)
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    public override void EnterState()
    {
        attackTimer = 0f;
        boss.Animator.SetTrigger("Attack");
    }

    public override void UpdateState()
    {
        if (player == null) return;

        attackTimer += Time.deltaTime;

        // 플레이어와의 거리 확인
        float distanceToPlayer = Vector3.Distance(boss.transform.position, player.position);

        if (distanceToPlayer > attackRange)
        {
            stateMachine.ChangeState(new BossMoveState(stateMachine, boss));
            return;
        }

        // 공격 쿨다운이 끝나면 다시 공격
        if (attackTimer >= attackCooldown)
        {
            boss.Animator.SetTrigger("Attack");
            attackTimer = 0f;
        }
    }

    public override void ExitState()
    {
        boss.Animator.ResetTrigger("Attack");
    }
} 
}