using UnityEngine;

namespace Enemy.States
{
    /// <summary>
    /// 점프 상태 - 적이 점프하는 상태
    /// </summary>
    public class JumpState : BaseEnemyState
    {
        #region Variables

        private float jumpPower; // 점프 힘
        private float jumpDistance; // 점프 거리
        private float jumpCooldown; // 점프 쿨타임
        private float jumpTimer = 0f;
        private bool isGrounded = true; // 땅에 있는지 여부
        private bool jumpPerformed = false; // 점프가 수행되었는지 여부

        #endregion

        #region Constructor

        /// <summary>
        /// 점프 상태 생성자
        /// </summary>
        /// <param name="enemy">적 객체 참조</param>
        /// <param name="stateMachine">상태 머신 참조</param>
        /// <param name="jumpPower">점프 힘</param>
        /// <param name="jumpDistance">점프 거리</param>
        /// <param name="jumpCooldown">점프 쿨타임</param>
        public JumpState(BaseEnemy enemy, EnemyStateMachine stateMachine, float jumpPower, float jumpDistance, float jumpCooldown)
            : base(enemy, stateMachine)
        {
            this.jumpPower = jumpPower;
            this.jumpDistance = jumpDistance;
            this.jumpCooldown = jumpCooldown;
        }
        #endregion

        public override void Enter()
        {
            // 초기화
            jumpTimer = 0f;
            isGrounded = true;
            jumpPerformed = false;
            
            Debug.Log("점프 상태 시작: 값 초기화 완료");
            
            // 즉시 점프 실행
            PerformJump();
        }

        public override void Update()
        {
            // 점프를 수행했고, 땅에 닿았다면
            if (jumpPerformed && isGrounded)
            {
                jumpTimer += Time.deltaTime;
                
                // 쿨다운이 지났으면 순찰 상태로 전환
                if (jumpTimer >= jumpCooldown)
                {
                    Debug.Log("점프 쿨다운 완료, 순찰 상태로 전환");
                    enemy.SwitchToState<PatrolState>();
                }
            }
            
            // 땅에 있는지 확인 (아래 방향으로 레이캐스트)
            CheckGrounded();
        }

        public override void FixedUpdate()
        {
            // 점프 중에는 아무 것도 하지 않음
        }

        public override void Exit()
        {
            enemy.StopMoving();
            Debug.Log("점프 상태 종료");
        }
        
        // 점프 수행
        private void PerformJump()
        {
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 모든 힘과 속도 초기화
                rb.velocity = Vector2.zero;
                
                // 순수하게 위로만 강하게 점프
                float actualJumpPower = jumpPower * 2f; // 점프력 2배 증가
                rb.AddForce(Vector2.up * actualJumpPower, ForceMode2D.Impulse);
                
                // 점프 수행 표시
                jumpPerformed = true;
                isGrounded = false;
                
                Debug.Log($"점프 실행! 점프력: {actualJumpPower}");
                
                // 시각적 확인을 위해 크기 변화 애니메이션 추가 (선택적)
                StartJumpAnimation();
            }
            else
            {
                Debug.LogError("Rigidbody2D 컴포넌트를 찾을 수 없습니다!");
            }
        }
        
        // 땅에 있는지 확인
        private void CheckGrounded()
        {
            // 레이캐스트로 땅 확인
            RaycastHit2D hit = Physics2D.Raycast(
                enemy.transform.position,
                Vector2.down,
                0.1f,
                LayerMask.GetMask("Ground")
            );
            
            // 이전에 공중에 있다가 지금 땅에 닿았으면
            if (!isGrounded && hit.collider != null)
            {
                isGrounded = true;
                Debug.Log("땅에 착지했습니다!");
            }
            // 이전에 땅에 있다가 지금 공중에 있으면
            else if (isGrounded && hit.collider == null)
            {
                isGrounded = false;
            }
        }
        
        // 점프 애니메이션 (선택적)
        private void StartJumpAnimation()
        {
            // 점프 시 스프라이트를 약간 늘리는 효과 (선택적)
            // 단순한 시각적 피드백을 위한 것이므로 실제 구현에 따라 달라질 수 있음
        }
    }
}