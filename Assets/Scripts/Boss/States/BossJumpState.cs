
using UnityEngine;

namespace BossFSM
{
    public class BossJumpState : BossState
    {
        private Transform player;
        private float jumpTimer;
        private float gravity = 1f; //초기 중력값 설정
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
            gravity = 1f; // 점프 시작 시 중력 초기화
            // 플레이어가 존재하는지 확인 후 점프 방향 결정
            if (player != null)
            {
                Vector2 direction = (player.position - boss.transform.position).normalized;
                Vector2 jumpDirection = (direction + Vector2.up).normalized;

                if (!hasJumped)
                {
                    if (Random.value < 0.1f) // 10% 확률로 수직 점프
                    {
                        boss.Rb.AddForce(Vector2.up * boss.JumpForce, ForceMode2D.Impulse);
                    }
                    else // 90% 확률로 플레이어 방향으로 점프
                    {
                        boss.Rb.AddForce(jumpDirection * boss.JumpForce, ForceMode2D.Impulse);
                    }
                    hasJumped = true;
                }
            }
            else // 플레이어가 없는 경우 기본 수직 점프
            {
                boss.Rb.AddForce(Vector2.up * boss.JumpForce, ForceMode2D.Impulse);
                hasJumped = true;
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
            boss.Rb.gravityScale = 1f; // 상태 종료 시 반드시 중력 복구
        }
    }
}
