using UnityEngine;

public class PlayerChargingAttackState : PlayerAttackStateBase
{
    private float chargeStartTime;
    private float maxChargeTime = 2.0f; // 최대 충전 시간 (초)
    private bool isFullyCharged = false;
    
    public PlayerChargingAttackState(PlayerAttackStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        chargeStartTime = Time.time;
        isFullyCharged = false;
        

        // 차징 상태 설정
        stateMachine.SetCharging(true);
        
        Debug.Log("충전 상태 시작");
    }

    public override void Update()
    {
        // 충전 시간 체크
        float chargeTime = Time.time - chargeStartTime;
        
        // 충전 완료 체크 (ChargeManager 사용 권장)
        var chargeManager = WeaponManager.Instance.ChargeManager;
        if (chargeManager != null && chargeManager.CurrentChargeLevel == 2 && !isFullyCharged)
        {
            isFullyCharged = true;
            Debug.Log("충전 완료! (ChargeManager 기준)");
            // 과충전 상태로 전환은 이동 중에만 하도록 HandleInput에서 처리
        }
    }

    public override void HandleInput()
    {
        var inputHandler = stateMachine.GetInputHandler();
        var chargeManager = WeaponManager.Instance.ChargeManager;

        // 차징 버튼을 놓으면 차징 상태 종료
        if (!inputHandler.IsChargingAttack)
        {
            // 충전된 상태에 따라 공격 실행
            FireChargedWeapon();
            
            // 공격 후 None 상태로 돌아감
            stateMachine.ChangeState(AttackStateType.None);
        }
        
        // 차징 중에 이동하면 이동 + 차징 체크
        if (inputHandler.IsMoving())
        {
            var movementStateMachine = stateMachine.GetMovementStateMachine();
            if (movementStateMachine != null && chargeManager != null)
            {
                // ChargeManager의 레벨이 2(풀차지)일 때만 Overcharging 상태로 전환
                if (chargeManager.CurrentChargeLevel == 2 && movementStateMachine.CurrentMovementState == MovementStateType.Running)
                {
                    Debug.Log("이동 중 풀차지 상태에서만 과충전 진입! (ChargeManager 기준)");
                    stateMachine.ChangeState(AttackStateType.Overcharging);
                }
            }
        }
    }

    public override void Exit()
    {
        stateMachine.SetCharging(false);
        Debug.Log("충전 상태 종료");
    }
    
    // 차징 공격 발사
    private void FireChargedWeapon()
    {
        var weaponManager = stateMachine.GetWeaponManager();
        if (weaponManager != null)
        {
            float chargeTime = Time.time - chargeStartTime;
            float chargePower = Mathf.Clamp01(chargeTime / maxChargeTime);
            
            Debug.Log($"차지 공격 발사! 파워: {chargePower}");
            
            
            weaponManager.StopCharging();
        }
        else
        {
            Debug.LogWarning("WeaponManager를 찾을 수 없습니다.");
        }
    }
} 