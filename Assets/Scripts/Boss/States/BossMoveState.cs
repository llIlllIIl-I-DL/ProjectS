using UnityEngine;

namespace BossFSM
{
    public class BossMoveState : BossState
    {
        private Transform player;
        private float moveSpeed = 5f;
        private float attackRange = 3f;

        public BossMoveState(BossStateMachine stateMachine, Boss boss) : base(stateMachine, boss)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        public override void EnterState()
        {
            boss.Animator.SetBool("IsMoving", true);
        }

        public override void UpdateState()
        {
            if (player == null) return;

            Vector3 direction = (player.position - boss.transform.position).normalized;
            boss.transform.position += direction * moveSpeed * Time.deltaTime;

            // 플레이어를 향해 회전
            boss.transform.rotation = Quaternion.LookRotation(direction);

            // 플레이어가 공격 범위 안에 들어오면 공격 상태로 전환
            float distanceToPlayer = Vector3.Distance(boss.transform.position, player.position);
            if (distanceToPlayer <= attackRange)
            {
                stateMachine.ChangeState(new BossAttackState(stateMachine, boss));
            }
        }

        public override void ExitState()
        {
            boss.Animator.SetBool("IsMoving", false);
        }
    }
} 