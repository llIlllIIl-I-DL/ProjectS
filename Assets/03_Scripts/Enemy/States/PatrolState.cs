using UnityEngine;

namespace Enemy.States
{
    public class PatrolState : BaseEnemyState
    {
        // 순찰 경로
        protected Vector2[] waypoints; // 순찰 경로
        protected int currentWaypoint = 0; // 현재 목적지 인덱스
        protected float waypointReachDistance = 0.1f; // 목적지 도달 거리

        // 대기 시간
        protected float waitAtWaypoint;
        protected float waitTimer = 0f;
        protected bool isWaiting = false;

        public PatrolState(BaseEnemy enemy, EnemyStateMachine stateMachine, Vector2[] waypoints, float waitTime)
            : base(enemy, stateMachine)
        {
            this.waypoints = waypoints;
            this.waitAtWaypoint = waitTime;
        }

        public override void Enter()
        {
            // 순찰 애니메이션 재생
            // enemy.GetComponent<Animator>()?.SetBool("IsPatrol", true);
        }

        public override void Update()
        {
            // 플레이어 감지되었으면 추격 상태로 전환
            if (enemy.IsPlayerDetected())
            {
                // ChaseState로 전환
                return;
            }

            // 대기 중이면 타이머 체크
            if (isWaiting)
            {
                waitTimer += Time.deltaTime;
                //대기중이라면 대기 애니메이션 재생
                // enemy.GetComponent<Animator>()?.SetBool("IsPatrol", false);
                // enemy.GetComponent<Animator>()?.SetBool("IsWating", true);
                if (waitTimer >= waitAtWaypoint)
                {
                    isWaiting = false;
                    // 다음 목적지로 인덱스 변경
                    currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
                }
            }
        }

        public override void FixedUpdate()
        {
            if (isWaiting || waypoints.Length == 0) return;

            // 현재 목적지
            Vector2 targetPosition = waypoints[currentWaypoint];

            // 방향 설정
            Vector2 direction = ((Vector3)targetPosition - enemy.transform.position).normalized;
            enemy.SetFacingDirection(direction);

            // 이동
            enemy.MoveInDirection(direction);

            // 목적지에 도달했는지 확인
            if (Vector2.Distance(enemy.transform.position, targetPosition) < waypointReachDistance)
            {
                // 목적지 도달, 대기 시작
                isWaiting = true;
                waitTimer = 0;

                // 이동 정지
                enemy.StopMoving();
            }
        }

        public override void Exit()
        {
            // 순찰 애니메이션 종료
            enemy.GetComponent<Animator>()?.SetBool("IsWalking", false);

            // 이동 정지
            enemy.StopMoving();
        }
    }
}