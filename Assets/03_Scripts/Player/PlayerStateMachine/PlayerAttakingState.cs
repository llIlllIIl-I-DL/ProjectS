using UnityEngine;

public class PlayerAttackingState : PlayerStateBase
{
    private float attackStartTime;
    private float attackDuration = 1f; // 공격 모션 지속 시간
    private float attackCooldown = 0.2f;  // 공격 쿨다운
    private bool canAttackAgain = true;

    private Vector2 lastAimDirection;

    public PlayerAttackingState(PlayerStateManager stateManager) : base(stateManager)
    {
    }

    public override void Enter()
    {
        attackStartTime = Time.time;

        // 플레이어가 바라보는 방향 저장
        lastAimDirection = new Vector2(player.GetMovement().FacingDirection, 0).normalized;

        // 총알 발사
        //FireWeapon();

        Debug.Log("공격 상태 시작: 총 발사");
    }

    public override void Update()
    {
        // 이동 입력에 따라 AttakingMove 파라미터 업데이트
        var inputHandler = player.GetInputHandler();
        var playerAnimator = player.GetComponent<PlayerAnimator>();
        if (playerAnimator != null)
        {
            playerAnimator.GetAnimator()?.SetFloat("AttakingMove", inputHandler.MoveDirection.magnitude);
        }
        // 공격 모션 종료 체크
        if (Time.time >= attackStartTime + attackDuration)
        {
            // 공격 끝난 후 상태 전환
            var collisionDetector = player.GetCollisionDetector();

            if (!collisionDetector.IsGrounded)
            {
                player.ChangeState(PlayerStateType.Falling);
            }
            else if (inputHandler.IsMoving())
            {
                player.ChangeState(PlayerStateType.Running);
            }
            else
            {
                player.ChangeState(PlayerStateType.Idle);
            }
        }
    }

    public override void FixedUpdate()
    {
        // 공격 중에도 이동은 가능하게 처리
        var inputHandler = player.GetInputHandler();
        var movement = player.GetMovement();

        // 속도 감소 없이 이동
        movement.Move(inputHandler.MoveDirection);
    }

    private void FireWeapon()
    {
        // 총알 생성 및 발사
        var weaponManager = player.GetComponent<WeaponManager>();
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
        player.StartCoroutine(AttackCooldown());
    }

    private System.Collections.IEnumerator AttackCooldown()
    {
        canAttackAgain = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttackAgain = true;
    }

    public override void Exit()
    {
        Debug.Log("공격 상태 종료");
    }

    // 연속 공격 가능 여부 체크
    public bool CanAttack()
    {
        return canAttackAgain;
    }
}