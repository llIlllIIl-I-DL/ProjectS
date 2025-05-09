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
        private float initialPatrolY; // 초기 순찰 높이
        
        public FlyingPatrolState(BaseEnemy enemy, EnemyStateMachine stateMachine, float patrolDistance, float waitTime)
            : base(enemy, stateMachine)
        {
            this.patrolDistance = patrolDistance;
            this.waitTime = waitTime;
            
            // 순찰 범위 설정
            patrolStart = enemy.transform.position;
            initialPatrolY = patrolStart.y; // 초기 순찰 높이 저장
            patrolEnd = new Vector2(patrolStart.x + patrolDistance, patrolStart.y);
        }
        
        public override void Enter()
        {
            waitCounter = 0;

            // 상태 진입 시 현재 위치를 기준으로 순찰 방향 재설정
            Vector2 currentPosition = enemy.transform.position;

            // 기존 순찰 영역의 중앙점 계산
            float centerX = (patrolStart.x + patrolEnd.x) / 2f;

            // 현재 위치가 중앙점보다 오른쪽에 있으면 왼쪽으로 이동하도록 설정
            movingRight = currentPosition.x < centerX;

            // 높이 차이가 있으면 원래 높이로 복귀 명령
            if (Mathf.Abs(currentPosition.y - initialPatrolY) > 0.1f)
            {
                // EnemyScrap인 경우 높이 복귀 메서드 호출
                EnemyScrap scrapEnemy = enemy as EnemyScrap;
                if (scrapEnemy != null)
                {
                    scrapEnemy.ReturnToHeight(initialPatrolY);
                }
            }
            else
            {
                // 이미 올바른 높이에 있으면 현재 위치를 원래 위치로 설정
                EnemyScrap scrapEnemy = enemy as EnemyScrap;
                if (scrapEnemy != null)
            {
                    scrapEnemy.SetCurrentPositionAsOriginalY();
                }
            }
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
            
            // 플레이어 감지 확인
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
                // 일반적인 이동
                enemy.MoveInDirection(direction);
            }
        }
        
        public override void OnTriggerEnter2D(Collider2D other) { }
    }
}