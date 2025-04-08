using UnityEngine;

namespace Enemy.States
{
    public class ChargeAttackState : BaseEnemyState
    {
        // 매개변수화된 속성들
        protected float chargeSpeed;      // 돌진 속도
        protected float chargeDistance;   // 돌진 거리
        protected float damageAmount;       // 데미지 양
        protected string animationTrigger; // 애니메이션 트리거 이름
        
        // 돌진 관련 상태 변수
        protected Vector2 chargeDirection; // 돌진 방향
        protected Vector2 startPosition;   // 시작 위치
        protected bool hasReachedTarget;   // 목표 도달 여부
        protected float distanceTraveled;  // 이동 거리
        
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
        
        public override void Enter()
        {
            Debug.Log("돌진 공격 시작");
            
            // 돌진 준비 단계
            startPosition = enemy.transform.position;
            
            // 플레이어 방향 계산
            Vector2 rawDirection = enemy.GetPlayerPosition() - (Vector2)enemy.transform.position;
            
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
        
        public override void Update()
        {
            // 목표 도달 확인
            distanceTraveled = Vector2.Distance(startPosition, enemy.transform.position);
            
            // 일정 거리 이동 후 돌진 종료
            if (distanceTraveled >= chargeDistance || hasReachedTarget)
            {
                // 돌진 완료, 이전 상태로 돌아가기
                Debug.Log("돌진 공격 완료, 공격 상태로 전환");
                enemy.SwitchToAttackState();
            }
        }
        
        public override void FixedUpdate()
        {
            // 돌진 이동
            enemy.MoveInDirection(chargeDirection, chargeSpeed);
        }
        
        public override void Exit()
        {
            // 돌진 종료 - 이동 정지
            enemy.StopMoving();
            
            // 애니메이션 리셋
            // enemy.GetComponent<Animator>()?.ResetTrigger(animationTrigger);
        }
        
        // 충돌 감지는 BaseEnemy에서 처리하되, 이 상태일 때 특별 처리할 수 있도록 메서드 제공
        public virtual void OnCollision(Collision2D collision)
        {
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
                // 충돌 애니메이션 추후에 있다면 재생
                // enemy.GetComponent<Animator>()?.SetTrigger("Hit");
                // 맞네 효과음도 생각해야하네..오디오 매니저 만들어서 추후에 붙여야겠다
                // AudioManager.PlaySFX?("HitSound");
                // 플레이어와 충돌한 후에 SlamAttackState로 전환
                enemy.SwitchToSlamAttackState();
            }
            else if (collision.gameObject.CompareTag("Wall"))
            {
                // 벽에 부딪히면 돌진 종료
                hasReachedTarget = true;
            }
        }
    }
}