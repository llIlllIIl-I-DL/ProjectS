using UnityEngine;

public class PlayerOverchargingAttackState : PlayerAttackStateBase
{
    private float overchargeStartTime;
    private bool hasTakenDamage = false;
    private float overchargeDamage = 5f; // 과충전 데미지 양
    private float maxOverchargeDuration = 3.0f; // 최대 과충전 지속 시간
    
    public PlayerOverchargingAttackState(PlayerAttackStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        overchargeStartTime = Time.time;
        hasTakenDamage = false;
        Debug.Log("과충전 상태 시작!");

    }

    public override void Update()
    {
        // 최대 과충전 시간 초과 시 강제 종료
        if (Time.time >= overchargeStartTime + maxOverchargeDuration)
        {
            Debug.Log("최대 과충전 시간 초과!");
            stateMachine.ChangeState(AttackStateType.None);
        }
    }

    public override void HandleInput()
    {
        var inputHandler = stateMachine.GetInputHandler();
        if (!inputHandler.IsChargingAttack)
        {
            // 과충전 공격 발사 시점에 데미지 적용
            FireOverchargedWeapon();
            stateMachine.ChangeState(AttackStateType.None);
        }
    }

    public override void Exit()
    {
        Debug.Log("과충전 상태 종료");
        // 애니메이터 파라미터 해제 (IsOvercharging false)
        var animator = stateMachine.GetAnimator();
        if (animator != null)
        {
            //animator.SetBool("IsOvercharging", false);
        }
        // 필요한 경우 과충전 후처리 (무적 프레임 등)
        var playerHP = stateMachine.gameObject.GetComponent<PlayerHP>();
        if (playerHP != null)
        {
            // 과충전 후 짧은 무적 시간 부여 (옵션)
            // playerHP.SetInvincible(0.5f);
        }
    }
    
    // 과충전 데미지 적용 - 한 번만 실행
    private void ApplyOverchargeDamage()
    {
        // 이미 데미지를 받았으면 패스
        if (hasTakenDamage) return;
        
        // 플레이어 HP 컴포넌트 가져오기
        var playerHP = stateMachine.gameObject.GetComponent<PlayerHP>();
        if (playerHP != null)
        {
            // 데미지 적용 전 상태 체크
            var playerStateManager = stateMachine.gameObject.GetComponent<PlayerStateManager>();
            if (playerStateManager != null && playerStateManager.CurrentAttackState == AttackStateType.MoveAttacking)
            {
                Debug.Log("이동+공격 상태에서는 과충전 데미지 무시");
                return;
            }
            Debug.Log($"과충전으로 인한 데미지 적용: {overchargeDamage}");
            playerHP.TakeDamage(overchargeDamage);
            hasTakenDamage = true; // 데미지 적용 완료 표시
        }
        else
        {
            Debug.LogWarning("PlayerHP 컴포넌트를 찾을 수 없습니다.");
        }
    }
    
    // 과충전 공격 발사 (매우 강력한 공격)
    private void FireOverchargedWeapon()
    {
        // 데미지 적용
        ApplyOverchargeDamage();

        // 애니메이터 트리거 (OverchargeFire)
        var animator = stateMachine.GetAnimator();
        if (animator != null)
        {
            animator.SetTrigger("OverchargeFire");
        }

        // 무기 발사
        var weaponManager = stateMachine.GetWeaponManager();
        if (weaponManager != null)
        {
            Debug.Log("최종 과충전 공격 발사!");
            weaponManager.StopCharging();
        }
        else 
        {
            Debug.LogWarning("WeaponManager를 찾을 수 없습니다.");
        }
    }
} 