using Unity.VisualScripting;
using UnityEngine;

namespace Enemy.States
{
    public class FlyingPatrolState : BaseEnemyState
    {
        private float patrolDistance;
        private float waitTime;
        private float waitCounter;
        private Vector2 patrolStart;
        private Vector2 patrolEnd;
        private bool movingRight = true;
        
        public FlyingPatrolState(BaseEnemy enemy, EnemyStateMachine stateMachine, float patrolDistance, float waitTime)
            : base(enemy, stateMachine)
        {
            this.patrolDistance = patrolDistance;
            this.waitTime = waitTime;
            
            // 순찰 범위 설정
            patrolStart = enemy.transform.position;
            patrolEnd = new Vector2(patrolStart.x + patrolDistance, patrolStart.y);
        }
        
        public override void Enter()
        {
            waitCounter = 0;
        }
        
        public override void Exit()
        {
            enemy.StopMoving();
        }
        
        public override void Update()
        {
            // 대기 중이면 카운터 감소
            if (waitCounter > 0)
            {
                waitCounter -= Time.deltaTime;
                return;
            }

            // 플레이어 감지 확인 (필요시 구현)
            if (enemy.IsPlayerDetected())
            {
                // 플레이어 발견 시 추격 상태로 전환
                enemy.SwitchToChaseState();
                return;
            }
            
            // 이동 방향 결정
            Vector2 targetPosition = movingRight ? patrolEnd : patrolStart;
            
            // 목표 지점에 도달했는지 확인
            if (Vector2.Distance(new Vector2(enemy.transform.position.x, 0), 
                                new Vector2(targetPosition.x, 0)) < 0.1f)
            {
                // 방향 전환 및 대기
                movingRight = !movingRight;
                waitCounter = waitTime;
                enemy.StopMoving();
            }
        }
        
        public override void FixedUpdate()
        {
            if (waitCounter <= 0)
            {
                // 이동 방향 설정
                Vector2 direction = movingRight ? Vector2.right : Vector2.left;
                enemy.SetFacingDirection(direction);
                enemy.MoveInDirection(direction);
            }
        }
        
        public override void OnTriggerEnter2D(Collider2D other) { }
    }
}