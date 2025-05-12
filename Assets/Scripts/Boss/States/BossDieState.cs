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
            Collider bossCollider = boss.GetComponent<Collider>();
            if (bossCollider != null)
            {
                bossCollider.enabled = false;
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