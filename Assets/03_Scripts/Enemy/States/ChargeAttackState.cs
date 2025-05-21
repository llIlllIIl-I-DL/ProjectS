using UnityEngine;

namespace Enemy.States
{
    /// <summary>
    /// 적의 돌진 공격 상태 - 플레이어 방향으로 빠르게 돌진하는 특수 공격 패턴
    /// </summary>
    public class ChargeAttackState : BaseEnemyState
    {
        #region Variables
        
        // 매개변수화된 속성들
        protected float chargeSpeed;      // 돌진 속도
        protected float chargeDistance;   // 돌진 거리
        protected float damageAmount;     // 데미지 양
        protected string animationTrigger; // 애니메이션 트리거 이름
        
        // 돌진 관련 상태 변수
        protected Vector2 chargeDirection; // 돌진 방향
        protected Vector2 startPosition;   // 시작 위치
        protected bool hasReachedTarget;   // 목표 도달 여부
        protected float distanceTraveled;  // 이동 거리
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// 돌진 공격 상태 생성자
        /// </summary>
        /// <param name="enemy">적 객체 참조</param>
        /// <param name="stateMachine">상태 머신 참조</param>
        /// <param name="chargeSpeed">돌진 속도</param>
        /// <param name="chargeDistance">돌진 거리</param>
        /// <param name="damageAmount">공격력</param>
        /// <param name="animationTrigger">애니메이션 트리거 이름</param>
        public ChargeAttackState(BaseEnemy enemy, EnemyStateMachine stateMachine, 
                              float chargeSpeed, float chargeDistance, 
                              float damageAmount, string animationTrigger = "Charge") 
            : base(enemy, stateMachine)
        {
            this.chargeSpeed = chargeSpeed;
            this.chargeDistance = chargeDistance;
            this.damageAmount = damageAmount;
            this.animationTrigger = animationTrigger;
        }
        
        #endregion
        
        #region State Methods
        
        /// <summary>
        /// 돌진 상태 진입 시 호출 - 돌진 방향 설정 및 초기화
        /// </summary>
        public override void Enter()
        {
            Debug.Log("돌진 공격 시작");
            
            // 돌진 준비 단계
            startPosition = enemy.transform.position;
            
            // 플레이어 방향 계산
            Vector2 rawDirection = enemy.PlayerPosition - (Vector2)enemy.transform.position;
            
            // X축으로만 돌진하도록 Y값을 0으로 설정 
            chargeDirection = new Vector2(Mathf.Sign(rawDirection.x), 0);
            
            // 방향 설정 (스프라이트 플립)
            enemy.SetFacingDirection(new Vector2(chargeDirection.x, 0));
            
            // 초기화
            hasReachedTarget = false;
            distanceTraveled = 0f;
            
            // 애니메이션 트리거
            // enemy.GetComponent<Animator>()?.SetTrigger(animationTrigger);
        }
        
        /// <summary>
        /// 돌진 상태 업데이트 - 이동 거리 확인 및 종료 조건 체크
        /// </summary>
        public override void Update()
        {
            // 목표 도달 확인
            distanceTraveled = Vector2.Distance(startPosition, enemy.transform.position);
            
            // 일정 거리 이동 후 돌진 종료
            if (distanceTraveled >= chargeDistance || hasReachedTarget)
            {
                // 디버깅용 로그 추가
                Debug.Log($"IsPlayerDetected: {enemy.IsPlayerDetected()}, IsInAttackRange: {enemy.IsInAttackRange()}");
                
                // 먼저 플레이어 감지 상태 업데이트
                bool playerDetected = enemy.IsPlayerDetected();
                bool inAttackRange = enemy.IsInAttackRange();

                // 상태 전환 로직
                if (inAttackRange) // 공격 범위 체크를 먼저
                {
                    Debug.Log("공격 범위 안에 있어 AttackState로 전환");
                    enemy.SwitchToState<AttackState>();
                    enemy.Animator.SetBool("IsIdle", true);
                    enemy.Animator.SetBool("IsWalking", false);
                }
                else if (playerDetected) // 그 다음 감지 범위 체크
                {
                    Debug.Log("추격 범위 안에 있어 ChaseState로 전환");
                    enemy.SwitchToState<ChaseState>();
                }
                else // 둘 다 아닌 경우
                {
                    Debug.Log("감지되지 않아 PatrolState로 전환");
                    enemy.SwitchToState<PatrolState>();
                }
                
                return;
            }
        }
        
        /// <summary>
        /// 물리 업데이트 - 실제 돌진 이동 실행
        /// </summary>
        public override void FixedUpdate()
        {
            // 돌진 이동
            enemy.MoveInDirection(chargeDirection, chargeSpeed);
        }
        
        /// <summary>
        /// 돌진 상태 종료 시 호출 - 이동 정지
        /// </summary>
        public override void Exit()
        {
            // 돌진 종료 - 이동 정지
            enemy.StopMoving();
            
            // 애니메이션 리셋
            // enemy.GetComponent<Animator>()?.ResetTrigger(animationTrigger);
        }
        
        #endregion
        
        #region Collision Handling
        
        /// <summary>
        /// 충돌 처리 - 플레이어 또는 벽과 충돌 시 동작
        /// </summary>
        /// <param name="collision">충돌 정보</param>
        public virtual void OnCollision(Collision2D collision)
        {
            // 플레이어와 충돌
            if (collision.gameObject.CompareTag("Player"))
            {
                // 플레이어에게 데미지
                IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damageAmount);
                }
                
                // 충돌 시 바로 돌진 종료
                hasReachedTarget = true;
                
                // TODO: 추후 구현 항목 
                // - 충돌 애니메이션 재생: enemy.GetComponent<Animator>()?.SetTrigger("Hit");
                // - 효과음 재생: AudioManager.PlaySFX("HitSound");
                
                // 플레이어와 충돌한 후에 내려찍기 상태로 전환
                enemy.SwitchToState<SlamAttackState>();
            }
            // 벽과 충돌
            else if (collision.gameObject.CompareTag("Wall"))
            {
                // 벽에 부딪히면 돌진 종료
                hasReachedTarget = true;
            }
        }
        
        #endregion
    }
}