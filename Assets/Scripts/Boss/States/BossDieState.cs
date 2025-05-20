using UnityEngine;


namespace BossFSM
{
    public class BossDieState : BossState
    {
        private float deathAnimationDuration = 3f;
        private float deathTimer;

        public BossDieState(BossStateMachine stateMachine, Boss boss) : base(stateMachine, boss)
        {
        }

        public override void EnterState()
        {
            deathTimer = 0f;
            boss.Animator.SetTrigger("Die");
            // 보스의 콜라이더 비활성화
            Collider2D bossCollider = boss.GetComponent<Collider2D>();
            if (bossCollider != null)
            {
                bossCollider.enabled = false;
            }

            // 리지드바디 동작 중지 (선택적)
            if (boss.Rb != null)
            {
                boss.Rb.velocity = Vector2.zero;
                boss.Rb.isKinematic = true; // 물리 영향을 받지 않도록 설정
            }
        }

        public override void UpdateState()
        {
            deathTimer += Time.deltaTime;

            if (deathTimer >= deathAnimationDuration)
            {
                // 보스 오브젝트 파괴
                Object.Destroy(boss.gameObject);
            }
        }

        public override void ExitState()
        {
            // Die 상태는 마지막 상태이므로 ExitState는 비워둡니다.
        }
    }
}