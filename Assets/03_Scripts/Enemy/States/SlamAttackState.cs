using UnityEngine;

namespace Enemy.States
{
    /// <summary>
    /// 적의 내려찍기 공격 상태 - 점프 후 내려찍어 범위 데미지를 주는 공격 패턴
    /// </summary>
    public class SlamAttackState : BaseEnemyState
    {
        #region Variables
        
        // 매개변수화된 속성들
        protected float slamSpeed;        // 내려찍기 속도
        protected float slamDistance;     // 내려찍기 영향 범위 (충격파 반경)
        protected float damageAmount;     // 데미지 양
        protected string animationTrigger; // 애니메이션 트리거 이름
        protected bool moveToPlayerX;     // X축으로 플레이어 위치로 이동할지 여부
        
        // 내려찍기 상태 변수
        protected Vector2 startPosition;  // 시작 위치
        protected Vector2 targetPosition;  // 플레이어 위치
        protected float jumpHeight;       // 최대 점프 높이
        protected float jumpDuration = 0.5f; // 점프 단계 시간
        protected float hangTime = 0.3f;  // 공중에 머무는 시간
        protected float slamTime = 0.3f;  // 내려찍는데 걸리는 시간
        
        // 상태 관리
        protected enum SlamPhase { Jump, Hang, Slam, End }
        protected SlamPhase currentPhase;
        protected float phaseTimer = 0f;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// 내려찍기 공격 상태 생성자
        /// </summary>
        /// <param name="enemy">적 객체 참조</param>
        /// <param name="stateMachine">상태 머신 참조</param>
        /// <param name="slamSpeed">내려찍기 속도</param>
        /// <param name="slamDistance">충격파 범위</param>
        /// <param name="damageAmount">데미지 양</param>
        /// <param name="animationTrigger">애니메이션 트리거</param>
        /// <param name="jumpHeight">점프 높이</param>
        /// <param name="moveToPlayerX">플레이어 X위치로 이동 여부</param>
        public SlamAttackState(BaseEnemy enemy, EnemyStateMachine stateMachine, 
                            float slamSpeed, float slamDistance, 
                            float damageAmount, string animationTrigger = "Slam",
                            float jumpHeight = 3f,
                            bool moveToPlayerX = true)
            : base(enemy, stateMachine)
        {
            this.slamSpeed = slamSpeed;
            this.slamDistance = slamDistance;
            this.damageAmount = damageAmount;
            this.animationTrigger = animationTrigger;
            this.jumpHeight = jumpHeight;
            this.moveToPlayerX = moveToPlayerX;
        }
        
        #endregion
        
        #region State Methods
        
        /// <summary>
        /// 내려찍기 상태 진입 시 호출 - 초기 위치 저장 및 초기화
        /// </summary>
        public override void Enter()
        {
            Debug.Log("내려찍기 공격 시작");
            
            // 초기 위치 저장
            startPosition = enemy.transform.position;
            
            // 플레이어 위치 저장 (내려찍기 목표 지점)
            targetPosition = enemy.PlayerPosition;
            
            // 초기화
            currentPhase = SlamPhase.Jump;
            phaseTimer = 0f;
            
            // 애니메이션 트리거 (점프)
            // enemy.GetComponent<Animator>()?.SetTrigger("Jump");
        }
        
        /// <summary>
        /// 내려찍기 상태 업데이트 - 단계별 진행 관리
        /// </summary>
        public override void Update()
        {
            phaseTimer += Time.deltaTime;
            
            switch (currentPhase)
            {
                case SlamPhase.Jump:
                    // 점프 단계 - 상승 후 정점에 도달
                    if (phaseTimer >= jumpDuration)
                    {
                        // 점프 단계 완료, 공중에 머무는 단계로
                        currentPhase = SlamPhase.Hang;
                        phaseTimer = 0f;
                        
                        // 애니메이션 전환
                        // enemy.GetComponent<Animator>()?.SetTrigger("Hang");
                    }
                    break;
                    
                case SlamPhase.Hang:
                    // 공중 정지 단계 - 목표물 위에서 잠시 대기
                    if (phaseTimer >= hangTime)
                    {
                        // 머무는 단계 완료, 내려찍기 단계로
                        currentPhase = SlamPhase.Slam;
                        phaseTimer = 0f;
                        
                        // 애니메이션 트리거 (내려찍기)
                        // enemy.GetComponent<Animator>()?.SetTrigger("SlamDown");
                        
                        // moveToPlayerX가 true일 때만 X 이동 실행
                        if (moveToPlayerX)
                        {
                            // X 위치를 플레이어 위치로 조정 (Y는 유지)
                            Vector3 newPosition = enemy.transform.position;
                            newPosition.x = targetPosition.x;
                            enemy.transform.position = newPosition;
                        }
                    }
                    break;
                    
                case SlamPhase.Slam:
                    // 내려찍기 단계 - 빠르게 하강하여 지면에 충격파 생성
                    if (phaseTimer >= slamTime || IsGrounded())
                    {
                        // 내려찍기 완료
                        currentPhase = SlamPhase.End;
                        
                        // 지면에 도달시 충격파 생성
                        CreateShockwave();
                        
                        // 공격 상태로 전환
                        enemy.SwitchToAttackState();
                    }
                    break;
            }
        }
        
        /// <summary>
        /// 물리 업데이트 - 각 단계별 이동 처리
        /// </summary>
        public override void FixedUpdate()
        {
            // 각 단계별 이동 처리
            switch (currentPhase)
            {
                case SlamPhase.Jump:
                    // 점프 단계 - 위로 상승 (사인 곡선으로 부드러운 움직임)
                    float jumpProgress = phaseTimer / jumpDuration;
                    float height = Mathf.Sin(jumpProgress * Mathf.PI / 2) * jumpHeight;
                    
                    Vector3 newPosition = startPosition;
                    newPosition.y = startPosition.y + height;
                    enemy.transform.position = newPosition;
                    break;
                    
                case SlamPhase.Hang:
                    // 공중에 정지 상태 - 위치 유지
                    break;
                    
                case SlamPhase.Slam:
                    // 빠르게 아래로 내려찍기
                    // enemy.rb.velocity = new Vector2(0, -slamSpeed);
                    break;
            }
        }
        
        /// <summary>
        /// 내려찍기 상태 종료 시 호출 - 애니메이션 리셋 및 이동 정지
        /// </summary>
        public override void Exit()
        {
            // 내려찍기 종료 - 이동 정지
            enemy.StopMoving();
            
            // 애니메이션 리셋
            // enemy.GetComponent<Animator>()?.ResetTrigger(animationTrigger);
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// 지면과 충돌했는지 확인
        /// </summary>
        /// <returns>지면에 닿았는지 여부</returns>
        private bool IsGrounded()
        {
            // 간단한 레이캐스트로 지면 확인
            return Physics2D.Raycast(
                enemy.transform.position, 
                Vector2.down, 
                0.2f, 
                LayerMask.GetMask("Ground"));
        }
        
        /// <summary>
        /// 충격파 생성 및 플레이어에게 데미지 처리
        /// </summary>
        private void CreateShockwave()
        {
            Debug.Log("충격파 생성!");
            
            // 시각 효과 (추후 구현)
            // GameObject effect = Object.Instantiate(shockwaveEffect, enemy.transform.position, Quaternion.identity);
            
            // 영역 내 플레이어 감지 및 데미지 처리
            Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(
                enemy.transform.position, 
                slamDistance, 
                LayerMask.GetMask("Player"));
                
            foreach (Collider2D player in hitPlayers)
            {
                IDamageable damageable = player.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // 데미지 적용
                    damageable.TakeDamage(damageAmount);
                    
                    // 넉백 효과 적용
                    Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        Vector2 knockbackDir = (player.transform.position - enemy.transform.position).normalized;
                        playerRb.AddForce(knockbackDir * slamSpeed * 100f);
                    }
                }
            }
            
            // 화면 흔들림 효과 (추후 구현)
            // CameraShake.Instance?.ShakeCamera(0.3f, 0.3f);
        }
        
        #endregion
    }
}