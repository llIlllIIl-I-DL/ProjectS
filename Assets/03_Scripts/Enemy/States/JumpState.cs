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
        private float jumpTimer; // 점프 타이머

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
            this.jumpTimer = 0f;
        }
        #endregion
    }
}