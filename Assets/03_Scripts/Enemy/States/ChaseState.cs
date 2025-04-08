using UnityEngine;

namespace Enemy.States
{
    public class ChaseState : BaseEnemyState
    {
        // 추격 관련 변수
        protected float chaseSpeed; // 추격 속도
        protected float losePlayerTime = 3f; // 플레이어를 놓친 후 추격 지속 시간
        protected float losePlayerTimer = 0; // 플레이어를 놓친 후 타이머
        protected bool isPlayerLost = false; // 플레이어를 놓쳤는지 여부
        protected bool moveInYAxis; // Y축 이동 허용 여부

        public ChaseState(BaseEnemy enemy, EnemyStateMachine stateMachine, float chaseSpeed, bool moveInYAxis = false)
            : base(enemy, stateMachine)
        {
            this.chaseSpeed = chaseSpeed; // 추격 속도
            this.moveInYAxis = moveInYAxis; // Y축 이동 설정
        }

        public override void Enter()
        {
            // 추격 애니메이션 재생
            // enemy.GetComponent<Animator>()?.SetBool("IsChasing", true);
            isPlayerLost = false;
            losePlayerTimer = 0;
        }

        public override void Update()
        {
            if (!enemy.IsPlayerDetected())
            {
                // 플레이어를 놓쳤을 때
                isPlayerLost = true;
                losePlayerTimer += Time.deltaTime;

                // 일정 시간이 지나면 순찰로 돌아감
                if (losePlayerTimer >= losePlayerTime)
                {
                    // 순찰 상태로 돌아가기
                    // enemy.GetComponent<Animator>()?.SetBool("IsChasing", false);
                    enemy.SwitchToPatrolState();
                    return;
                }
            }
            else
            {
                // 플레이어를 다시 발견
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
            
            if (moveInYAxis)
            {
                // X, Y 모두 사용 (공중 적)
                direction = rawDirection.normalized;
            }
            else
            {
                // X 방향으로만 이동 (지상 적)
                direction = new Vector2(Mathf.Sign(rawDirection.x), 0);
            }

            // 방향 설정 (스프라이트 플립 등)
            enemy.SetFacingDirection(direction);

            // 이동 실행
            enemy.MoveInDirection(direction, chaseSpeed);
        }

        public override void Exit()
        {
            // 추격 애니메이션 종료
            // enemy.GetComponent<Animator>()?.SetBool("IsChasing", false);

            // 이동 정지
            enemy.StopMoving();
        }
    }
}