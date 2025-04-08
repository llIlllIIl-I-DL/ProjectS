using UnityEngine;

namespace Enemy.States
{
    /// <summary>
    /// 적의 대기 상태 - 일정 시간 동안 정지하고 대기하는 상태
    /// </summary>
    public class IdleState : BaseEnemyState
    {
        #region Variables
        
        // 대기 관련 변수
        private float idleDuration; // 대기 지속 시간
        private float idleTimer = 0f; // 현재 대기 시간 타이머
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// 대기 상태 생성자
        /// </summary>
        /// <param name="enemy">적 객체 참조</param>
        /// <param name="stateMachine">상태 머신 참조</param>
        /// <param name="idleDuration">대기 시간 (초)</param>
        public IdleState(BaseEnemy enemy, EnemyStateMachine stateMachine, float idleDuration = 2f)
            : base(enemy, stateMachine)
        {
            this.idleDuration = idleDuration;
        }
        
        #endregion
        
        #region State Methods
        
        /// <summary>
        /// 대기 상태 진입 시 호출 - 타이머 초기화 및 애니메이션 설정
        /// </summary>
        public override void Enter()
        {
            idleTimer = 0f;
            // 대기 애니메이션 재생
            // enemy.GetComponent<Animator>()?.SetBool("IsIdle", true);
            
            // 이동 중지
            enemy.StopMoving();
        }

        /// <summary>
        /// 대기 상태 업데이트 - 시간 경과 및 플레이어 감지 확인
        /// </summary>
        public override void Update()
        {
            // 대기 시간 체크
            idleTimer += Time.deltaTime;

            // 대기 시간이 끝났으면 순찰로 전환
            if (idleTimer >= idleDuration)
            {
                // 순찰 상태로 전환
                enemy.SwitchToPatrolState();
                return;
            }

            // 플레이어 감지되었으면 추격 상태로 전환
            if (enemy.IsPlayerDetected())
            {
                // 추격 상태로 전환
                enemy.SwitchToChaseState();
            }
        }

        /// <summary>
        /// 대기 상태 종료 시 호출 - 애니메이션 리셋
        /// </summary>
        public override void Exit()
        {
            // 대기 애니메이션 종료
            // enemy.GetComponent<Animator>()?.SetBool("IsIdle", false);
        }
        
        #endregion
    }
}