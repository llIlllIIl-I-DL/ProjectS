using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace BossFSM
{
    public class BossJumpState : BossState
    {
        private Transform player;
        private float jumpTimer;
        private float gravity;
        private bool hasJumped = false;

        public BossJumpState(BossStateMachine stateMachine, Boss boss) : base(stateMachine, boss)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        public override void EnterState()
        {
            boss.Animator.SetBool("IsJump", true);
            jumpTimer = 0f;
            hasJumped = false;
            Vector2 direction = (player.position - boss.transform.position).normalized;
            Vector2 jumpDirection = (direction + Vector2.up).normalized;

            if (!hasJumped)
            {
                if(Random.value < 0.1f)
                {
                    boss.Rb.AddForce(Vector2.up * boss.JumpForce, ForceMode2D.Impulse);
                    hasJumped = true;
                }
                else
                {
                    boss.Rb.AddForce(jumpDirection * boss.JumpForce, ForceMode2D.Impulse);
                    hasJumped = true;
                }
            }
        }

        public override void UpdateState()
        {
            jumpTimer += Time.deltaTime;
            if (gravity < 5f)
            {
                gravity += Time.deltaTime * 10f;
                boss.Rb.gravityScale = gravity;
            }
            if (jumpTimer >= boss.JumpDuration)
            {
                boss.Rb.gravityScale = 1f;
                boss.Animator.SetBool("IsJump", false);
                stateMachine.ChangeState(new BossIdleState(stateMachine, boss)); // 점프 후 Idle 등으로 전환
            }
        }

        public override void ExitState()
        {
            boss.Animator.SetBool("IsJump", false);
        }
    }
}
