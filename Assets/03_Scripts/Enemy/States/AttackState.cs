using UnityEngine;

namespace Enemy.States
{
    /// <summary>
    /// 적의 공격 상태 - 플레이어를 향해 주기적으로 공격을 실행함
    /// </summary>
    public class AttackState : BaseEnemyState
    {
        #region Variables
        
        // 공격 관련 변수
        protected float attackCooldown; // 공격 간 쿨다운 시간
        protected float attackTimer = 0; // 현재 쿨다운 타이머
        protected bool canAttack = true; // 공격 가능 여부
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// 공격 상태 생성자
        /// </summary>
        /// <param name="enemy">적 객체 참조</param>
        /// <param name="stateMachine">상태 머신 참조</param>
        /// <param name="attackCooldown">공격 쿨다운 시간</param>
        public AttackState(BaseEnemy enemy, EnemyStateMachine stateMachine, float attackCooldown = 1f)
            : base(enemy, stateMachine)
        {
            this.attackCooldown = attackCooldown;
        }
        
        #endregion
        
        #region State Methods
        
        /// <summary>
        /// 공격 상태 진입 시 호출
        /// </summary>
        public override void Enter()
        {
            attackTimer = 0;
            canAttack = true;
        }

        /// <summary>
        /// 공격 상태 업데이트 - 공격 범위 확인 및 공격 실행
        /// </summary>
        public override void Update()
        {
            // 플레이어가 공격 범위를 벗어났는지 확인
            if (!enemy.IsInAttackRange())
            {
                enemy.SwitchToChaseState();
                return;
            }

            // 공격 쿨다운 관리
            if (!canAttack)
            {
                attackTimer += Time.deltaTime;
                if (attackTimer >= attackCooldown)
                {
                    canAttack = true;
                }
            }

            // 공격 가능하면 공격 실행
            if (canAttack)
            {
                // 적 방향 설정 (플레이어 방향으로 회전)
                Vector2 direction = enemy.GetPlayerPosition() - (Vector2)enemy.transform.position;
                enemy.SetFacingDirection(direction);

                // 공격 실행
                PerformAttack();
                Debug.Log("Attack performed!");

                // 쿨다운 설정
                canAttack = false;
                attackTimer = 0;
            }
        }

        /// <summary>
        /// 실제 공격 실행 로직 - 자식 클래스에서 오버라이드 가능
        /// </summary>
        protected virtual void PerformAttack()
        {
            // 공격 애니메이션 실행 (나중에 구현)
            // enemy.GetComponent<Animator>()?.SetTrigger("Attack");

            // 공격 로직 실행 - BaseEnemy의 PerformAttack 호출
            enemy.PerformAttack();
        }

        /// <summary>
        /// 공격 상태 종료 시 호출
        /// </summary>
        public override void Exit()
        {
            // 추가 정리 작업 (필요시 구현)
        }
        
        #endregion
    }
}