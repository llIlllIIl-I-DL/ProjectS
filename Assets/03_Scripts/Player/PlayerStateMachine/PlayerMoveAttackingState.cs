using UnityEngine;

public class PlayerMoveAttackingState : PlayerStateBase
{
    private float attackStartTime;
    private float attackDuration = 0.25f; // 공격 모션 지속 시간
    private float attackCooldown = 0.2f;  // 공격 쿨다운
    private bool canAttackAgain = true;

    private Vector2 lastAimDirection;

    public PlayerMoveAttackingState(PlayerStateManager stateManager) : base(stateManager)
    {
    }

    public override void Enter()
    {
        attackStartTime = Time.time;
        lastAimDirection = new Vector2(player.GetMovement().FacingDirection, 0).normalized;
        //FireWeapon(); // 필요시 무기 발사
        Debug.Log("이동+공격 상태 시작: 총 발사");
    }

    public override void Update()
    {
        if (Time.time >= attackStartTime + attackDuration)
        {
            var collisionDetector = player.GetCollisionDetector();
            var inputHandler = player.GetInputHandler();

            if (!collisionDetector.IsGrounded)
            {
                player.ChangeState(PlayerStateType.Falling);
            }
            else if (inputHandler.IsMoving() && inputHandler.IsAttackPressed)
            {
                player.ChangeState(PlayerStateType.MoveAttacking);
            }
            else if (inputHandler.IsMoving())
            {
                player.ChangeState(PlayerStateType.Running);
            }
            else if (inputHandler.IsAttackPressed)
            {
                player.ChangeState(PlayerStateType.Attacking);
            }
            else
            {
                player.ChangeState(PlayerStateType.Idle);
            }
        }
    }

    public override void FixedUpdate()
    {
        var inputHandler = player.GetInputHandler();
        var movement = player.GetMovement();
        movement.Move(inputHandler.MoveDirection); // 속도 감소 없이 이동
    }

    public override void Exit()
    {
        Debug.Log("이동+공격 상태 종료");
    }

    // 연속 공격 가능 여부 체크
    public bool CanAttack()
    {
        return canAttackAgain;
    }
} 