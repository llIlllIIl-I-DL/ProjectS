using UnityEngine;

namespace Enemy.States
{
    public class FlyingChaseState : BaseEnemyState
    {
        private float chaseSpeed; // 추격 속도
        protected float losePlayerTime = 3f; // 플레이어를 놓친 후 추격 지속 시간
        protected float losePlayerTimer = 0; // 플레이어를 놓친 후 타이머
        protected bool isPlayerLost = false; // 플레이어를 놓쳤는지 여부
        
        private float lastKnownPositionThreshold = 0.3f; // 마지막 알려진 위치에 도달했다고 간주할 거리
        private bool isWaitingAtLastPosition = false; // 마지막 위치에서 대기 중인지 여부
        private float waitAtLastPositionTime = 1.0f; // 마지막 위치에서 대기할 시간
        private float waitAtLastPositionTimer = 0f; // 대기 타이머

        public FlyingChaseState(BaseEnemy enemy, EnemyStateMachine stateMachine, float chaseSpeed)
            : base(enemy, stateMachine)
        {
            this.chaseSpeed = chaseSpeed;
        }

        public override void Enter()
        {
            isPlayerLost = false;
            losePlayerTimer = 0;
            isWaitingAtLastPosition = false;
            waitAtLastPositionTimer = 0f;
            
            // 추격 상태 진입 시 현재 위치를 기준으로 설정
            EnemyScrap scrapEnemy = enemy as EnemyScrap;
            if (scrapEnemy != null)
            {
                scrapEnemy.SetCurrentPositionAsOriginalY();
            }
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

                // 마지막 알려진 위치에 도달했는지 확인
                if (!isWaitingAtLastPosition)
                {
                    Vector2 lastKnownPos = enemy.GetLastKnownPlayerPosition();
                    float distanceToLastKnownPos = Vector2.Distance(lastKnownPos, enemy.transform.position);
                    
                    if (distanceToLastKnownPos < lastKnownPositionThreshold)
                    {
                        // 마지막 알려진 위치에 도달했으면 대기 모드로 전환
                        isWaitingAtLastPosition = true;
                        waitAtLastPositionTimer = 0f;
                        enemy.StopMoving(); // 움직임 중지
                    }
                }
                else
                {
                    // 마지막 위치에서 대기 중
                    waitAtLastPositionTimer += Time.deltaTime;
                    
                    // 대기 시간이 지나면 방황 효과 (선택 사항)
                    if (waitAtLastPositionTimer >= waitAtLastPositionTime)
                    {
                        // 방황 효과는 여기서 구현 가능
                    }
                }

                if (losePlayerTimer >= losePlayerTime)
                {
                    // 플레이어를 놓친 후 시간이 지나면 상태 전환
                    enemy.SwitchToPatrolState();
                    return;
                }
            }
            else
            {
                // 플레이어를 감지했을 때
                isPlayerLost = false;
                losePlayerTimer = 0;
                isWaitingAtLastPosition = false;

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
            // 마지막 위치에서 대기 중이면 움직이지 않음
            if (isWaitingAtLastPosition)
            {
                enemy.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                return;
            }
            
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

            // 현재 위치에서 목표 위치까지의 방향 벡터 계산
            Vector2 direction = (targetPosition - (Vector2)enemy.transform.position).normalized;

            // X 방향에 따라 스프라이트 방향 설정
            enemy.SetFacingDirection(direction.x > 0 ? Vector2.right : Vector2.left);

            // 실제 이동은 Rigidbody2D를 직접 조작
            enemy.GetComponent<Rigidbody2D>().velocity = direction * enemy.GetMoveSpeed() * chaseSpeed;
        }

        public override void OnTriggerEnter2D(Collider2D other) { }
    }
}