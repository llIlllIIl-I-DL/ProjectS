using UnityEngine;

namespace Enemy.States
{
    public class AttackState : BaseEnemyState
    {
        // 공격 관련 변수
        protected float attackCooldown;
        protected float attackTimer = 0;
        protected bool canAttack = true;

        public AttackState(BaseEnemy enemy, EnemyStateMachine stateMachine, float attackCooldown = 1.5f)
            : base(enemy, stateMachine)
        {
            this.attackCooldown = attackCooldown;
        }

        public override void Enter()
        {
            attackTimer = 0;
            canAttack = true;
        }

        public override void Update()
        {
            // 플레이어가 공격 범위를 벗어났는지 확인
            if (!enemy.IsInAttackRange())
            {
                // 추격 상태로 전환
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
                // 적 방향 설정
                Vector2 direction = enemy.GetPlayerPosition() - (Vector2)enemy.transform.position;
                enemy.SetFacingDirection(direction);

                // 공격 실행
                PerformAttack();

                // 쿨다운 설정
                canAttack = false;
                attackTimer = 0;
            }
        }

        protected virtual void PerformAttack()
        {
            // 공격 애니메이션 실행
            // enemy.GetComponent<Animator>()?.SetTrigger("Attack");

            // 공격 로직 실행
            enemy.PerformAttack();
        }

        public override void Exit()
        {
            // 추가 정리 작업
        }
    }
}