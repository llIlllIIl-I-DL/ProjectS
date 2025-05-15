using UnityEngine;

public class PlayerMoveAttackingAttackState : PlayerAttackStateBase
{
    private float attackStartTime;
    private float attackDuration = 0.25f; // 공격 모션 지속 시간
    private float attackCooldown = 0.2f;  // 공격 쿨다운
    private bool canAttackAgain = true;

    private Vector2 lastAimDirection;

    public PlayerMoveAttackingAttackState(PlayerAttackStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        attackStartTime = Time.time;
        lastAimDirection = new Vector2(stateMachine.GetMovement().FacingDirection, 0).normalized;
        //FireWeapon(); // 필요시 무기 발사
        Debug.Log("이동+공격 상태 시작: 총 발사");
        
        // 공격 상태 설정
        stateMachine.SetAttacking(true);
    }

    public override void Update()
    {
        if (Time.time >= attackStartTime + attackDuration)
        {
            var inputHandler = stateMachine.GetInputHandler();

            // 여전히 이동 중이면서 계속 공격키를 누르고 있으면 유지
            if (inputHandler.IsMoving() && inputHandler.IsAttackPressed)
            {
                stateMachine.ChangeState(AttackStateType.MoveAttacking);
            }
            // 이동만 하고 있으면 None 상태로 전환
            else if (inputHandler.IsMoving())
            {
                stateMachine.ChangeState(AttackStateType.None);
            }
            // 정지 상태에서 공격키만 누르고 있으면 일반 공격으로 전환
            else if (inputHandler.IsAttackPressed)
            {
                stateMachine.ChangeState(AttackStateType.Attacking);
            }
            // 아무 입력도 없으면 None 상태로 전환
            else
            {
                stateMachine.ChangeState(AttackStateType.None);
            }
        }
    }

    public override void HandleInput()
    {
        var inputHandler = stateMachine.GetInputHandler();
        
        // 차징 버튼을 누르면 차징 상태로 전환
        if (inputHandler.IsChargingAttack)
        {
            stateMachine.ChangeState(AttackStateType.Charging);
        }
    }

    public override void Exit()
    {
        Debug.Log("이동+공격 상태 종료");
        stateMachine.SetAttacking(false);
    }

    // 연속 공격 가능 여부 체크
    public bool CanAttack()
    {
        return canAttackAgain;
    }
} 