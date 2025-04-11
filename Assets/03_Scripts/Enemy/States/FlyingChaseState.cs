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
                    enemy.SwitchToPatrolState();
                    return;
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
             // 목표 위치 결정
            Vector2 targetPosition;

            if (isPlayerLost)
            {
                // 플레이어를 놓친 경우, 마지막으로 알려진 위치로 이동
                targetPosition = enemy.GetLastKnownPlayerPosition();
            }
            else
            {
                // 플레이어를 쫓기
                targetPosition = enemy.GetPlayerPosition();
            }

            // 원시 방향 계산
            Vector2 rawDirection = ((Vector3)targetPosition - enemy.transform.position);
            Vector2 direction;
            
            // 추격은 X축 및 Y축 모두 사용
            if (rawDirection.x > 0)
            {
                direction = Vector2.right;
            }
            else if (rawDirection.x < 0)
            {
                direction = Vector2.left;
            }
            else
            {
                direction = Vector2.zero;
            }

            // 방향 설정 (스프라이트 플립 등)
            enemy.SetFacingDirection(direction);
            // 이동 실행
            enemy.MoveInDirection(direction, chaseSpeed);
        }

        public override void OnTriggerEnter2D(Collider2D other) { }
    }
}