using UnityEngine;

namespace Enemy.States
{
    /// <summary>
    /// 적의 순찰 상태 - 지정된 경로를 따라 이동하며 주기적으로 대기하는 상태
    /// </summary>
    public class PatrolState : BaseEnemyState
    {
        #region Variables
        
        // 순찰 경로
        protected Vector2[] waypoints;             // 순찰 경로 지점들
        protected int currentWaypoint = 0;         // 현재 목적지 인덱스
        protected float waypointReachDistance = 0.1f; // 목적지 도달 판정 거리

        // 대기 관련
        protected float waitAtWaypoint;            // 웨이포인트 도달 후 대기 시간
        protected float waitTimer = 0f;            // 현재 대기 시간 타이머
        protected bool isWaiting = false;          // 대기 중인지 여부
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// 순찰 상태 생성자
        /// </summary>
        /// <param name="enemy">적 객체 참조</param>
        /// <param name="stateMachine">상태 머신 참조</param>
        /// <param name="waypoints">순찰 경로 지점들</param>
        /// <param name="waitTime">각 지점에서 대기하는 시간</param>
        public PatrolState(BaseEnemy enemy, EnemyStateMachine stateMachine, Vector2[] waypoints, float waitTime)
            : base(enemy, stateMachine)
        {
            this.waypoints = waypoints;
            this.waitAtWaypoint = waitTime;
        }
        
        #endregion
        
        #region State Methods
        
        /// <summary>
        /// 순찰 상태 진입 시 호출 - 애니메이션 설정
        /// </summary>
        public override void Enter()
        {
            // 순찰 애니메이션 재생
            // enemy.GetComponent<Animator>()?.SetBool("IsPatrol", true);
        }

        /// <summary>
        /// 순찰 상태 업데이트 - 대기 시간 관리 및 플레이어 감지 처리
        /// </summary>
        public override void Update()
        {
            // 플레이어 감지 확인 (필요시 구현)
            if (enemy.IsPlayerDetected())
            {
                // 플레이어 발견 시 추격 상태로 전환
                enemy.SwitchToChaseState();
                return;
            }

            // 대기 중이면 타이머 체크
            if (isWaiting)
            {
                waitTimer += Time.deltaTime;
                
                // 대기 중이라면 대기 애니메이션 재생
                // enemy.GetComponent<Animator>()?.SetBool("IsPatrol", false);
                // enemy.GetComponent<Animator>()?.SetBool("IsWaiting", true);
                
                // 대기 시간이 끝나면 이동 재개
                if (waitTimer >= waitAtWaypoint)
                {
                    isWaiting = false;
                    
                    // 다음 목적지로 인덱스 변경
                    currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
                    
                    // 이동 애니메이션으로 전환
                    // enemy.GetComponent<Animator>()?.SetBool("IsWaiting", false);
                    // enemy.GetComponent<Animator>()?.SetBool("IsPatrol", true);
                }
            }
        }

        /// <summary>
        /// 물리 업데이트 - 실제 이동 처리
        /// </summary>
        public override void FixedUpdate()
        {
            // 대기 중이거나 웨이포인트가 없으면 이동하지 않음
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

        /// <summary>
        /// 순찰 상태 종료 시 호출 - 애니메이션 리셋 및 이동 정지
        /// </summary>
        public override void Exit()
        {
            // 순찰 애니메이션 종료
            // enemy.GetComponent<Animator>()?.SetBool("IsPatrol", false);
            // enemy.GetComponent<Animator>()?.SetBool("IsWaiting", false);

            // 이동 정지
            enemy.StopMoving();
        }
        
        #endregion
    }
}