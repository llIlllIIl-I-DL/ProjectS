using UnityEngine;

public class PlayerAttackingAttackState : PlayerAttackStateBase
{
    private float attackStartTime;
    private float attackDuration = 0.25f; // 공격 모션 지속 시간
    private float attackCooldown = 0.2f;  // 공격 쿨다운
    private bool canAttackAgain = true;

    private Vector2 lastAimDirection;

    public PlayerAttackingAttackState(PlayerAttackStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        attackStartTime = Time.time;

        // 플레이어가 바라보는 방향 저장
        lastAimDirection = new Vector2(stateMachine.GetMovement().FacingDirection, 0).normalized;

        // 총알 발사
        //FireWeapon();

        Debug.Log("공격 상태 시작: 총 발사");
        
        // 공격 상태 설정
        stateMachine.SetAttacking(true);
    }

    public override void Update()
    {
        // 공격 모션 종료 체크
        if (Time.time >= attackStartTime + attackDuration)
        {
            // 공격 끝난 후 상태 전환
            var inputHandler = stateMachine.GetInputHandler();

            if (inputHandler.IsMoving())
            {
                // 이동중이면서 계속 공격키 누르고 있으면 MoveAttacking으로 전환
                if (inputHandler.IsAttackPressed)
                {
                    stateMachine.ChangeState(AttackStateType.MoveAttacking);
                }
                else
                {
                    stateMachine.ChangeState(AttackStateType.None);
                }
            }
            else
            {
                // 정지 상태에서 계속 공격키 누르고 있으면 다시 Attacking 상태로
                if (inputHandler.IsAttackPressed)
                {
                    stateMachine.ChangeState(AttackStateType.Attacking);
                }
                else
                {
                    stateMachine.ChangeState(AttackStateType.None);
                }
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
        Debug.Log("공격 상태 종료");
        stateMachine.SetAttacking(false);
    }

    private void FireWeapon()
    {
        // 총알 생성 및 발사
        var weaponManager = stateMachine.GetWeaponManager();
        if (weaponManager != null)
        {
            weaponManager.FireNormalBullet();
        }
        else
        {
            // 임시 총알 생성 로직 (WeaponManager가 없을 경우)
            WeaponManager.Instance.FireNormalBullet();
        }

        // 공격 쿨다운 시작
        stateMachine.StartCoroutine(AttackCooldown());
    }

    private System.Collections.IEnumerator AttackCooldown()
    {
        canAttackAgain = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttackAgain = true;
    }

    // 연속 공격 가능 여부 체크
    public bool CanAttack()
    {
        return canAttackAgain;
    }
}