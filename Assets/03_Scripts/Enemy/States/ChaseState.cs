using UnityEngine;

namespace Enemy.States
{
    public class ChaseState : BaseEnemyState
    {
        // 추격 관련 변수
        protected float chaseSpeed;
        protected float losePlayerTime = 3f; // 플레이어를 놓친 후 추격 지속 시간
        protected float losePlayerTimer = 0;
        protected bool isPlayerLost = false;

        public ChaseState(BaseEnemy enemy, EnemyStateMachine stateMachine, float chaseSpeed)
            : base(enemy, stateMachine)
        {
            this.chaseSpeed = chaseSpeed;
        }

        public override void Enter()
        {
            // 추격 애니메이션 재생
            enemy.GetComponent<Animator>()?.SetBool("IsChasing", true);
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

            // 방향 계산
            Vector2 direction = ((Vector3)targetPosition - enemy.transform.position).normalized;

            // 방향 설정
            enemy.SetFacingDirection(direction);

            // 추격 이동
            enemy.MoveInDirection(direction, chaseSpeed);
        }

        public override void Exit()
        {
            // 추격 애니메이션 종료
            enemy.GetComponent<Animator>()?.SetBool("IsChasing", false);

            // 이동 정지
            enemy.StopMoving();
        }
    }
}