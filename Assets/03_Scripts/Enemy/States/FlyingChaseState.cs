using UnityEngine;

namespace Enemy.States
{
    public class FlyingChaseState : BaseEnemyState
    {
        private float chaseSpeed; // 추격 속도
        protected float losePlayerTime = 3f; // 플레이어를 놓친 후 추격 지속 시간
        protected float losePlayerTimer = 0; // 플레이어를 놓친 후 타이머
        protected bool isPlayerLost = false; // 플레이어를 놓쳤는지 여부
        private float originalY; // 비행 높이 유지

        public FlyingChaseState(BaseEnemy enemy, EnemyStateMachine stateMachine, float chaseSpeed)
            : base(enemy, stateMachine)
        {
            this.chaseSpeed = chaseSpeed;
        }

        public override void Enter()
        {
            isPlayerLost = false;
            losePlayerTimer = 0;
            originalY = enemy.transform.position.y; // 현재 높이 저장
        }

        public override void Exit()
        {
            enemy.StopMoving();
        }

        public override void Update()
        {
            if (!enemy.IsPlayerDetected())
            {
                // 플레이어를 놓쳤을 때
                isPlayerLost = true;
                losePlayerTimer += Time.deltaTime;

                if (losePlayerTimer >= losePlayerTime)
                {
                    // 플레이어를 놓친 후 상태 전환
                    stateMachine.ChangeState(enemy.GetComponent<FlyingPatrolState>());
                }
            }
            else
            {
                // 플레이어를 감지했을 때
                isPlayerLost = false;
                losePlayerTimer = 0;

                // 공격 범위 안에 있으면 공격 상태로 전환
                if (enemy.IsInAttackRange())
                {
                    // 공격 상태로 전환
                    enemy.SwitchToAttackState();
                    return;
                }
            }
        }

        public override void FixedUpdate()
        {

        }

        public override void OnTriggerEnter2D(Collider2D other) { }
    }
}