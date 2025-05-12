using UnityEngine;

namespace BossFSM
{
    public class BossIdleState : BossState
    {
        private float idleTimer;
        private float idleDuration = 2f;

        public BossIdleState(BossStateMachine stateMachine, Boss boss) : base(stateMachine, boss)
        {
        }

        public override void EnterState()
        {
            idleTimer = 0f;
            boss.Animator.SetBool("IsIdle", true);
        }

        public override void UpdateState()
        {
            idleTimer += Time.deltaTime;

            if (idleTimer >= idleDuration)
            {
                stateMachine.ChangeState(new BossMoveState(stateMachine, boss));
            }
        }

        public override void ExitState()
        {
            boss.Animator.SetBool("IsIdle", false);
        }
    }
} 