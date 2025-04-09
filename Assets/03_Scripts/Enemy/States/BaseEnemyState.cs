using UnityEngine;

namespace Enemy.States
{
    /// <summary>
    /// 적 상태의 기본 추상 클래스 - 모든 적 상태의 공통 기능 제공
    /// </summary>
    public abstract class BaseEnemyState : IEnemyState
    {
        #region Fields
        
        // 상태가 소속된 적 참조
        protected BaseEnemy enemy;
        
        // 상태 머신 참조
        protected EnemyStateMachine stateMachine;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// 기본 상태 생성자
        /// </summary>
        /// <param name="enemy">상태가 속한 적 객체</param>
        /// <param name="stateMachine">상태 관리 머신</param>
        protected BaseEnemyState(BaseEnemy enemy, EnemyStateMachine stateMachine)
        {
            this.enemy = enemy;
            this.stateMachine = stateMachine;
        }
        
        #endregion
        
        #region State Interface Methods
        
        /// <summary>
        /// 상태 진입 시 호출됨 - 초기화 작업 수행
        /// </summary>
        public virtual void Enter() { }
        
        /// <summary>
        /// 상태 종료 시 호출됨 - 정리 작업 수행
        /// </summary>
        public virtual void Exit() { }
        
        /// <summary>
        /// 매 프레임 호출됨 - 주요 상태 로직 처리
        /// </summary>
        public virtual void Update() { }
        
        /// <summary>
        /// 물리 업데이트마다 호출됨 - 물리 기반 로직 처리
        /// </summary>
        public virtual void FixedUpdate() { }
        
        /// <summary>
        /// 트리거 충돌 감지 시 호출됨 - 충돌 반응 처리
        /// </summary>
        /// <param name="other">충돌한 콜라이더</param>
        public virtual void OnTriggerEnter2D(Collider2D other) { }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// 상태 전환 헬퍼 메서드
        /// </summary>
        /// <param name="newState">전환할 새 상태</param>
        protected void ChangeState(IEnemyState newState)
        {
            stateMachine.ChangeState(newState);
        }
        
        #endregion
    }
}