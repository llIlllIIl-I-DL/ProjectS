using UnityEngine;

// 공격 없음 상태 구현
public class PlayerNoneAttackState : PlayerAttackStateBase
{
    public PlayerNoneAttackState(PlayerAttackStateMachine stateMachine) : base(stateMachine) { }

    public override void HandleInput()
    {
        var inputHandler = stateMachine.GetInputHandler();
        
        // 공격 버튼 누르면 공격 상태로 전환
        if (inputHandler.IsAttackPressed)
        {
            // 이동 중이면 이동+공격 상태로
            if (inputHandler.IsMoving())
            {
                stateMachine.ChangeState(AttackStateType.MoveAttacking);
            }
            else
            {
                stateMachine.ChangeState(AttackStateType.Attacking);
            }
        }
        
        // 차징 버튼 누르면 차징 상태로 전환
        if (inputHandler.IsChargingAttack)
        {
            stateMachine.ChangeState(AttackStateType.Charging);
        }
    }
} 