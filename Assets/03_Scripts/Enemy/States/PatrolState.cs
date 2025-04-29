using UnityEngine;

namespace Enemy.States
{
    public class PatrolState : BaseEnemyState
    {
        // 기본 변수
        protected Vector2[] waypoints;
        protected int currentWaypoint = 0;
        protected float waypointReachDistance = 0.1f;
        protected float waitAtWaypoint;
        protected float waitTimer = 0f;
        protected bool isWaiting = false;

        // 디버깅용
        protected bool debugMode = true;

        // 저장된 위치 변수
        private Vector3 currentStopPosition;

        public PatrolState(BaseEnemy enemy, EnemyStateMachine stateMachine, Vector2[] waypoints, float waitTime)
            : base(enemy, stateMachine)
        {
            this.waypoints = waypoints;
            this.waitAtWaypoint = waitTime;
            
            if (debugMode) Debug.Log($"PatrolState 생성: 웨이포인트 {waypoints.Length}개, 대기시간 {waitTime}초");
        }

        public override void Enter()
        {
            if (debugMode) Debug.Log("PatrolState 진입");
            isWaiting = false;
        }

        public override void Update()
        {
            // 대기 중이면 타이머 체크
            if (isWaiting)
            {
                // 대기 중에는 위치를 고정
                if (currentStopPosition != Vector3.zero)
                {
                    enemy.transform.position = currentStopPosition;
                }
                
                waitTimer += Time.deltaTime;
                if (waitTimer >= waitAtWaypoint)
                {
                    // 대기 종료, 다시 물리 효과 활성화
                    Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                    }
                    
                    isWaiting = false;
                    currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
                    currentStopPosition = Vector3.zero; // 위치 저장값 초기화
                    
                    if (debugMode) Debug.Log($"다음 웨이포인트로 이동: {currentWaypoint}");
                }
            }
        }

        public override void FixedUpdate()
        {
            // 대기 중이거나 웨이포인트가 없으면 이동하지 않음
            if (isWaiting || waypoints.Length == 0) 
            {
                if (debugMode && Time.frameCount % 60 == 0) Debug.Log("대기 중 또는 웨이포인트 없음");
                return;
            }

            // 1. 현재 목적지
            Vector2 targetPosition = waypoints[currentWaypoint];
            
            // 2. 방향 계산 (단순화)
            float directionX = Mathf.Sign(targetPosition.x - enemy.transform.position.x);
            Vector2 moveDirection = new Vector2(directionX, 0);
            
            // 3. 방향 설정 (기존 코드)
            enemy.SetFacingDirection(moveDirection);

            // 4. 이동 (가장 단순한 방식)
            float moveSpeed = 2.0f; // 기본값 - BaseEnemy에 GetMoveSpeed 없을 경우를 위해
            
            try 
            {
                // 가능하면 실제 속도 가져오기
                moveSpeed = enemy.GetMoveSpeed();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"GetMoveSpeed 호출 실패: {e.Message}");
            }
            
            // 단순 이동 적용
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = new Vector2(directionX * moveSpeed, rb.velocity.y);
                if (debugMode && Time.frameCount % 30 == 0) 
                    Debug.Log($"이동 중: 속도={rb.velocity}, 방향={directionX}");
            }

            // 5. 목적지 도달 확인
            if (Mathf.Abs(enemy.transform.position.x - targetPosition.x) < waypointReachDistance)
            {
                // 목적지 도달, 대기 시작
                isWaiting = true;
                waitTimer = 0;
                
                // 이동 정지 및 경사면에서 미끄러짐 방지 (중요 변경)
                StopMovementCompletely();
                
                if (debugMode) Debug.Log($"웨이포인트 {currentWaypoint} 도달, 대기 시작");
            }
        }

        public override void Exit()
        {
            if (debugMode) Debug.Log("PatrolState 종료");
            enemy.StopMoving();
        }

        // 완전히 멈추는 새로운 메서드 추가
        private void StopMovementCompletely()
        {
            // 기본 이동 중지
            enemy.StopMoving();
            
            // 추가: Rigidbody 속도 완전히 0으로 설정
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 속도 0으로 설정
                rb.velocity = Vector2.zero;
                
                // 대기 중에는 운동학적(Kinematic) 상태로 전환하여 물리 영향 제거
                rb.isKinematic = true;
                
                // 현재 위치 저장 (미끄러짐 방지용)
                currentStopPosition = enemy.transform.position;
                
                if (debugMode) Debug.Log("완전 정지 적용 (Kinematic)");
            }
        }
    }
}