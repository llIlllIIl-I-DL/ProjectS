using UnityEngine;

namespace BossFSM
{
    public class BossMoveState : BossState
    {
        private Transform player;
        private float moveTimer = 0f; // 이동 상태 지속 시간 타이머

        public BossMoveState(BossStateMachine stateMachine, Boss boss) : base(stateMachine, boss)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            Debug.Log("FindPlayer");
        }

        public override void EnterState()
        {
            boss.Animator.SetBool("IsMoving", true);
            moveTimer = 0f; // 타이머 초기화
        }

        public override void UpdateState()
        {
            if (player == null) return;

            moveTimer += Time.deltaTime;

            Vector2 direction = (player.position - boss.transform.position).normalized;
            boss.Rb.AddForce(direction * boss.MoveSpeed); // 힘을 가함

            float distanceToPlayer = Vector3.Distance(boss.transform.position, player.position); // 플레이어와의 거리 계산


            // 2초 이내에 플레이어가 공격 범위에 들어오면 공격 상태로 전환
            if (distanceToPlayer <= boss.AttackRange)
            {
                Debug.Log("Attack");
                stateMachine.ChangeState(new BossAttackState(stateMachine, boss));
                return;
            }

            // 2초가 지났고, 아직 공격 범위에 들어오지 않았다면 점프
            if (moveTimer >= 4f)
            {
                stateMachine.ChangeState(new BossJumpState(stateMachine, boss));
            }
        }

        public override void ExitState()
        {
            boss.Animator.SetBool("IsMoving", false);
            boss.Animator.SetBool("IsJump", false); // 상태 나갈 때 점프 해제(필요시)
        }
    }
} 