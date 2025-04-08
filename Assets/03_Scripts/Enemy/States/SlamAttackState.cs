using UnityEngine;

namespace Enemy.States
{
    public class SlamAttackState : BaseEnemyState
    {
        // 매개변수화된 속성들
        protected float slamSpeed;        // 내려찍기 속도
        protected float slamDistance;     // 내려찍기 영향 범위
        protected float damageAmount;       // 데미지 양
        protected string animationTrigger; // 애니메이션 트리거 이름
        protected bool moveToPlayerX; // X축으로 플레이어 위치로 이동할지 여부
        
        // 내려찍기 상태 변수
        protected Vector2 startPosition;  // 시작 위치
        protected Vector2 targetPosition;  // 플레이어 위치
        protected float jumpHeight;  // 최대 점프 높이
        protected float jumpDuration = 0.5f; // 점프 단계 시간
        protected float hangTime = 0.3f;  // 공중에 머무는 시간
        protected float slamTime = 0.3f;  // 내려찍는데 걸리는 시간
        
        // 상태 관리
        protected enum SlamPhase { Jump, Hang, Slam, End }
        protected SlamPhase currentPhase;
        protected float phaseTimer = 0f;
        
        public SlamAttackState(BaseEnemy enemy, EnemyStateMachine stateMachine, 
                            float slamSpeed, float slamDistance, 
                            float damageAmount, string animationTrigger = "Slam",
                            float jumpHeight = 0f,
                            bool moveToPlayerX = true)
            : base(enemy, stateMachine)
        {
            this.slamSpeed = slamSpeed;
            this.slamDistance = slamDistance;
            this.damageAmount = damageAmount;
            this.animationTrigger = animationTrigger;
            this.jumpHeight = jumpHeight;
            this.moveToPlayerX = moveToPlayerX; // 이동 여부 설정
        }
        
        public override void Enter()
        {
            Debug.Log("내려찍기 공격 시작");
            
            // 초기 위치 저장
            startPosition = enemy.transform.position;
            
            // 플레이어 위치 저장 (내려찍기 목표 지점)
            targetPosition = enemy.GetPlayerPosition();
            
            // 초기화
            currentPhase = SlamPhase.Jump;
            phaseTimer = 0f;
            
            // 애니메이션 트리거 (점프)
            // enemy.GetComponent<Animator>()?.SetTrigger("Jump");
        }
        
        public override void Update()
        {
            phaseTimer += Time.deltaTime;
            
            switch (currentPhase)
            {
                case SlamPhase.Jump:
                    if (phaseTimer >= jumpDuration)
                    {
                        // 점프 단계 완료, 공중에 머무는 단계로
                        currentPhase = SlamPhase.Hang;
                        phaseTimer = 0f;
                        
                        // 애니메이션 전환 (선택 사항)
                        // enemy.GetComponent<Animator>()?.SetTrigger("Hang");
                    }
                    break;
                    
                case SlamPhase.Hang:
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
        
        public override void FixedUpdate()
        {
            // 각 단계별 이동 처리
            switch (currentPhase)
            {
                case SlamPhase.Jump:
                    // 점프 단계 - 위로 상승
                    float jumpProgress = phaseTimer / jumpDuration;
                    float height = Mathf.Sin(jumpProgress * Mathf.PI / 2) * jumpHeight;
                    
                    Vector3 newPosition = startPosition;
                    newPosition.y = startPosition.y + height;
                    enemy.transform.position = newPosition;
                    break;
                    
                case SlamPhase.Hang:
                    break;
                    
                case SlamPhase.Slam:
                    break;
            }
        }
        
        public override void Exit()
        {
            // 내려찍기 종료 - 이동 정지
            enemy.StopMoving();
            
            // 애니메이션 리셋
            // enemy.GetComponent<Animator>()?.ResetTrigger(animationTrigger);
        }
        
        // 지면 충돌 확인
        private bool IsGrounded()
        {
            // 간단한 레이캐스트로 지면 확인
            return Physics2D.Raycast(
                enemy.transform.position, 
                Vector2.down, 
                0.2f, 
                LayerMask.GetMask("Ground"));
        }
        
        // 충격파 생성 및 데미지 처리
        private void CreateShockwave()
        {
            Debug.Log("충격파 생성!");
            
            // 시각 효과 (선택 사항)
            // GameObject effect = Object.Instantiate(shockwaveEffect, enemy.transform.position, Quaternion.identity);
            
            // 영역 내 플레이어 감지 및 데미지 처리
            Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(
                enemy.transform.position, 
                slamDistance, 
                LayerMask.GetMask("Player"));
                
            foreach (Collider2D player in hitPlayers)
            {
                Idamageable damageable = player.GetComponent<Idamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damageAmount);
                    
                    // 넉백 효과 테스트해보고 이상하거나 필요없으면 삭제
                    Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        Vector2 knockbackDir = (player.transform.position - enemy.transform.position).normalized;
                        playerRb.AddForce(knockbackDir * slamSpeed * 100f);
                    }
                }
            }
            
            // 화면 흔들림 효과 추후에? 필요할지도 모르니까??
            // CameraShake.Instance?.ShakeCamera(0.3f, 0.3f);
        }
    }
}