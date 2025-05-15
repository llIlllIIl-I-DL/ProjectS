using UnityEngine;

namespace Enemy.States
{
    /// <summary>
    /// 플레이어를 추격하는 적의 상태 - 플레이어가 범위 내에서 도망치면 따라감
    /// </summary>
    public class ChaseState : BaseEnemyState
    {
        #region Variables

        // 추격 관련 변수
        protected float chaseSpeed;        // 추격 속도
        protected float losePlayerTime = 3f; // 플레이어를 놓친 후 추격 지속 시간
        protected float losePlayerTimer = 0; // 플레이어를 놓친 후 타이머
        protected bool isPlayerLost = false; // 플레이어를 놓쳤는지 여부
        protected bool moveInYAxis;        // Y축 이동 허용 여부

        // 부드러운 이동을 위한 변수들
        private float currentSpeed = 0f;   // 현재 속도
        private float acceleration = 5f;   // 가속도
        private float deceleration = 5f;   // 감속도
        private float rotationSpeed = 1f;  // 회전 속도
        private float minDistanceToTarget = 0.5f; // 목표 지점과의 최소 거리
        private Vector2 currentDirection;  // 현재 이동 방향

        private float lastKnownPositionThreshold = 0.3f; // 마지막 알려진 위치에 도달했다고 간주할 거리
        private bool isWaitingAtLastPosition = false; // 마지막 위치에서 대기 중인지 여부
        private float waitAtLastPositionTime = 1.0f; // 마지막 위치에서 대기할 시간
        private float waitAtLastPositionTimer = 0f; // 대기 타이머

        #endregion

        #region Constructor

        /// <summary>
        /// 추격 상태 생성자
        /// </summary>
        /// <param name="enemy">적 객체 참조</param>
        /// <param name="stateMachine">상태 머신 참조</param>
        /// <param name="chaseSpeed">추격 속도</param>
        /// <param name="moveInYAxis">Y축 이동 허용 여부</param>
        public ChaseState(BaseEnemy enemy, EnemyStateMachine stateMachine, float chaseSpeed, bool moveInYAxis = false)
            : base(enemy, stateMachine)
        {
            this.chaseSpeed = chaseSpeed;  // 추격 속도
            this.moveInYAxis = moveInYAxis; // Y축 이동 설정
        }

        #endregion

        #region State Methods

        /// <summary>
        /// 추격 상태 진입 시 호출 - 타이머 초기화 및 애니메이션 설정
        /// </summary>
        public override void Enter()
        {
            isPlayerLost = false;
            losePlayerTimer = 0;
            isWaitingAtLastPosition = false;
            waitAtLastPositionTimer = 0f;
            currentSpeed = 0f;
            currentDirection = Vector2.zero;
        }

        /// <summary>
        /// 추격 상태 업데이트 - 플레이어 감지 및 상태 전환 처리
        /// </summary>
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
                    Vector2 lastKnownPos = enemy.LastKnownPlayerPosition;
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
                // 플레이어를 다시 발견
                isPlayerLost = false;
                losePlayerTimer = 0;
                isWaitingAtLastPosition = false;

                // 공격 범위 안에 있으면 공격 상태로 전환
                if (enemy.IsInAttackRange())
                {
                    enemy.SwitchToAttackState();
                    return;
                }
            }
        }

        /// <summary>
        /// 물리 업데이트 - 플레이어를 향한 실제 추적 이동
        /// </summary>
        public override void FixedUpdate()
        {
            // 마지막 위치에서 대기 중이면 움직이지 않음
            if (isWaitingAtLastPosition)
            {
                enemy.StopMoving();
                return;
            }

            Vector2 targetPosition = isPlayerLost ? 
                enemy.LastKnownPlayerPosition : 
                enemy.PlayerPosition;

            // 목표 지점까지의 거리 계산
            float distanceToTarget = Vector2.Distance(targetPosition, enemy.transform.position);

            // 방향 계산
            Vector2 targetDirection = ((Vector3)targetPosition - enemy.transform.position).normalized;
            
            // Y축 이동 제한 (지상 적의 경우)
            if (!moveInYAxis)
            {
                targetDirection.y = 0;
                targetDirection.Normalize();
            }

            // 부드러운 방향 전환
            currentDirection = Vector2.Lerp(currentDirection, targetDirection, rotationSpeed * Time.fixedDeltaTime);

            // 속도 조절
            if (distanceToTarget > minDistanceToTarget)
            {
                // 목표 지점이 멀리 있으면 가속
                currentSpeed = Mathf.MoveTowards(currentSpeed, chaseSpeed, acceleration * Time.fixedDeltaTime);
            }
            else
            {
                // 목표 지점에 가까워지면 감속
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.fixedDeltaTime);
            }

            // 최종 이동 방향과 속도 적용
            Vector2 movement = currentDirection * currentSpeed;
            enemy.MoveInDirection(movement.normalized, currentSpeed);

            // 스프라이트 방향 설정
            if (currentDirection.x != 0)
            {
                enemy.SetFacingDirection(new Vector2(currentDirection.x, 0));
            }
        }

        /// <summary>
        /// 추격 상태 종료 시 호출 - 애니메이션 리셋 및 이동 정지
        /// </summary>
        public override void Exit()
        {
            enemy.StopMoving();
            currentSpeed = 0f;
            currentDirection = Vector2.zero;
        }

        #endregion
    }
}