using UnityEngine;

namespace Enemy.States
{
    public class IdleState : BaseEnemyState
    {
        // 대기 시간
        private float idleDuration;
        private float idleTimer = 0f;

        public IdleState(BaseEnemy enemy, EnemyStateMachine stateMachine, float idleDuration = 2f)
            : base(enemy, stateMachine)
        {
            this.idleDuration = idleDuration;
        }

        public override void Enter()
        {
            idleTimer = 0f;
            // 대기 애니메이션 재생
            enemy.GetComponent<Animator>()?.SetBool("IsIdle", true);
        }

        public override void Update()
        {
            // 대기 시간 체크
            idleTimer += Time.deltaTime;

            // 대기 시간이 끝났으면 순찰로 전환
            if (idleTimer >= idleDuration)
            {
                // PatrolState로 전환할 때는 해당 적의 구체적인 패트롤 상태로 전환해야 함
                // 이 부분은 상속받은 클래스에서 구현하거나 외부에서 처리
            }

            // 플레이어 감지되었으면 추격 상태로 전환
            if (enemy.IsPlayerDetected())
            {
                // ChaseState로 전환할 때는 해당 적의 구체적인 추격 상태로 전환해야 함
                // 이 부분은 상속받은 클래스에서 구현하거나 외부에서 처리
            }
        }

        public override void Exit()
        {
            // 대기 애니메이션 종료
            enemy.GetComponent<Animator>()?.SetBool("IsIdle", false);
        }
    }
}