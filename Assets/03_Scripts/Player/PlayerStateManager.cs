using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateManager : MonoBehaviour
{
    [SerializeField] private PlayerSettings settings;

    // 상태 머신 참조
    private PlayerMovementStateMachine movementStateMachine;
    private PlayerAttackStateMachine attackStateMachine;

    // 컴포넌트 참조
    private PlayerInputHandler inputHandler;
    private PlayerMovement movement;
    private CollisionDetector collisionDetector;
    private PlayerAnimator playerAnimator;
    private Animator animator;

    // 타이머 변수
    private float lastGroundedTime;
    private float lastJumpTime;
    private float lastWallTime;
    private float lastDashTime;

    // 상태 플래그
    private bool isJumping;
    private bool isWallSliding;
    private bool isDashing;
    private bool isSprinting;
    private bool canDash = true;
    private bool isCrouching;
    private bool isClimbing;

    // 프로퍼티
    public MovementStateType CurrentMovementState => movementStateMachine != null ? movementStateMachine.CurrentMovementState : MovementStateType.Idle;
    public AttackStateType CurrentAttackState => attackStateMachine != null ? attackStateMachine.CurrentAttackState : AttackStateType.None;
    public float LastGroundedTime => lastGroundedTime;
    public float LastJumpTime => lastJumpTime;
    public float LastWallTime => lastWallTime;
    public float LastDashTime => lastDashTime;
    public bool IsJumping => isJumping;
    public bool IsWallSliding => isWallSliding;
    public bool IsDashing => isDashing;
    public bool CanDash => canDash;
    public bool IsCrouching => isCrouching;
    public bool IsClimbing => isClimbing;

    // 이벤트
    public event Action<MovementStateType> OnMovementStateChanged;
    public event Action<AttackStateType> OnAttackStateChanged;
    public event Action<bool> OnCrouchStateChanged;
    public event Action<bool> OnClimbStateChanged;

    private void Awake()
    {
        // 컴포넌트 참조 설정
        inputHandler = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        collisionDetector = GetComponent<CollisionDetector>();
        playerAnimator = GetComponent<PlayerAnimator>();
        animator = GetComponent<Animator>();
        
        // 상태 머신 참조 가져오기
        movementStateMachine = GetComponent<PlayerMovementStateMachine>();
        attackStateMachine = GetComponent<PlayerAttackStateMachine>();

        if (movementStateMachine == null || attackStateMachine == null)
        {
            Debug.LogError("상태 머신 컴포넌트가 없습니다.");
        }
    }

    private void OnEnable()
    {
        // 이벤트 구독
        if (inputHandler != null)
        {
            inputHandler.OnJumpInput += HandleJumpInput;
            inputHandler.OnJumpRelease += HandleJumpRelease;
            inputHandler.OnDashInput += HandleDashInput;
            inputHandler.OnSprintActivated += HandleSprintActivated;
            inputHandler.OnAttackInput += HandleAttackInput;
        }

        if (collisionDetector != null)
        {
            collisionDetector.OnGroundedChanged += HandleGroundedChanged;
            collisionDetector.OnWallTouchChanged += HandleWallTouchChanged;
            collisionDetector.OnLadderTouchChanged += HandleLadderTouchChanged;
        }

        if (movement != null)
        {
            movement.OnDirectionChanged += HandleDirectionChanged;
        }
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        if (inputHandler != null)
        {
            inputHandler.OnJumpInput -= HandleJumpInput;
            inputHandler.OnJumpRelease -= HandleJumpRelease;
            inputHandler.OnDashInput -= HandleDashInput;
            inputHandler.OnSprintActivated -= HandleSprintActivated;
            inputHandler.OnAttackInput -= HandleAttackInput;
        }

        if (collisionDetector != null)
        {
            collisionDetector.OnGroundedChanged -= HandleGroundedChanged;
            collisionDetector.OnWallTouchChanged -= HandleWallTouchChanged;
            collisionDetector.OnLadderTouchChanged -= HandleLadderTouchChanged;
        }

        if (movement != null)
        {
            movement.OnDirectionChanged -= HandleDirectionChanged;
        }
    }

    // 이동 상태를 변경하는 메서드 추가
    public void ChangeState(MovementStateType newState)
    {
        if (movementStateMachine != null)
        {
            movementStateMachine.ChangeState(newState);
        }
        else
        {
            Debug.LogError("상태 변경 실패: MovementStateMachine이 null입니다.");
        }
    }

    private void Update()
    {
        // 타이머 업데이트
        UpdateTimers();

        // 앉기 입력 체크
        CheckCrouchInput();
        
        // 사다리 입력 체크
        CheckClimbInput();

        // 상태 관련 자동 전환 확인
        CheckStateTransitions();

        UpdateAnimatorParameters();
    }

    private void UpdateTimers()
    {
        // 코요테 타임 감소
        if (!collisionDetector.IsGrounded)
        {
            lastGroundedTime -= Time.deltaTime;
        }

        // 점프 버퍼 타임 감소
        if (lastJumpTime > 0)
        {
            lastJumpTime -= Time.deltaTime;

            // 점프 버퍼 타임이 만료되면 점프 입력도 초기화
            if (lastJumpTime <= 0 && !isJumping)
            {
                inputHandler.ResetInputState();
            }
        }

        // 벽 붙기 시간 감소
        if (!collisionDetector.IsTouchingWall)
        {
            lastWallTime -= Time.deltaTime;
        }
    }

    private void CheckStateTransitions()
    {
        // 특정 상태 간 강제 전환이 필요한 경우 처리
        CheckCombinedStateTransitions();
    }
    
    private void CheckCombinedStateTransitions()
    {
        // 상태 머신이 null인 경우 종료
        if (movementStateMachine == null || attackStateMachine == null || inputHandler == null)
        {
            Debug.LogWarning("CheckCombinedStateTransitions: 필요한 컴포넌트가 초기화되지 않았습니다");
            return;
        }

        // 이동+공격 상태 조합 관리
        if (CurrentMovementState == MovementStateType.Running && 
            CurrentAttackState == AttackStateType.None && 
            inputHandler.IsAttackPressed)
        {
            attackStateMachine.ChangeState(AttackStateType.MoveAttacking);
        }
        // 공격 중 이동 시작 시 이동+공격으로 전환
        else if (CurrentMovementState == MovementStateType.Idle && 
                 CurrentAttackState == AttackStateType.Attacking && 
                 inputHandler.IsMoving())
        {
            attackStateMachine.ChangeState(AttackStateType.MoveAttacking);
        }
    }

    private void HandleJumpInput(bool pressed)
    {
        if (pressed)
        {
            lastJumpTime = settings.jumpBufferTime;

            // 벽 슬라이딩 중이면 벽 점프
            if (isWallSliding)
            {
                PerformWallJump();
            }
            // 점프 가능한 상태이면 점프
            else if (lastGroundedTime > 0)
            {
                PerformNormalJump();
            }
        }
    }
    
    private void PerformWallJump()
    {
        movement.WallJump(settings.wallJumpForce, settings.wallJumpDirection);
        movementStateMachine.ChangeState(MovementStateType.Jumping);
        lastJumpTime = 0;
    }
    
    private void PerformNormalJump()
    {
        isJumping = true;
        movement.Jump(settings.jumpForce);
        movementStateMachine.ChangeState(MovementStateType.Jumping);
        lastJumpTime = 0;
        lastGroundedTime = 0;
    }

    private void HandleJumpRelease()
    {
        // 점프 컷
        if (isJumping && movement.Velocity.y > 0)
        {
            movement.JumpCut();
        }
    }

    private void HandleDashInput()
    {
        if (canDash && !isDashing)
        {
            StartDash();
        }
    }

    private void StartDash()
    {
        isDashing = true;
        canDash = false;
        lastDashTime = Time.time;

        // 대시 상태로 변경
        movementStateMachine.ChangeState(MovementStateType.Dashing);

        // 대시 실행
        movement.Dash(settings.dashSpeed, settings.dashDuration);

        // 일정 시간 후 대시 가능 상태로 복귀
        StartCoroutine(DashCooldownRoutine());
    }

    private IEnumerator DashCooldownRoutine()
    {
        yield return new WaitForSeconds(settings.dashDuration);

        // 대시 종료
        isDashing = false;

        // 낙하 중이면 Falling 상태로 전환
        if (!collisionDetector.IsGrounded)
        {
            movementStateMachine.ChangeState(MovementStateType.Falling);
        }
        else
        {
            movementStateMachine.ChangeState(inputHandler.IsMoving() ? MovementStateType.Running : MovementStateType.Idle);
        }

        // 대시 쿨다운
        yield return new WaitForSeconds(settings.dashCooldown);
        canDash = true;
    }

    private void HandleSprintActivated()
    {
        if (CurrentMovementState == MovementStateType.Running && collisionDetector.IsGrounded)
        {
            isSprinting = true;
            // 여기서는 Sprinting 상태를 별도로 처리할 수도 있지만, 필요시 Running 상태에서 구현
        }
    }

    private void HandleAttackInput()
    {
        // 대시 중에는 공격 불가
        if (isDashing) return;
        
        if (inputHandler.IsMoving() && inputHandler.IsAttackPressed)
        {
            attackStateMachine.ChangeState(AttackStateType.MoveAttacking);
        }
        else if (inputHandler.IsAttackPressed)
        {
            attackStateMachine.ChangeState(AttackStateType.Attacking);
        }
    }
    
    private void HandleGroundedChanged(bool isGrounded)
    {
        if (isGrounded)
        {
            lastGroundedTime = settings.coyoteTime;

            // 공중에서 지면에 착지
            if (CurrentMovementState == MovementStateType.Falling ||
                CurrentMovementState == MovementStateType.Jumping)
            {
                // 이동 중이면 Running, 아니면 Idle
                movementStateMachine.ChangeState(inputHandler.IsMoving() ? MovementStateType.Running : MovementStateType.Idle);
                isJumping = false;
            }
        }
    }

    private void HandleWallTouchChanged(bool isTouchingWall)
    {
        if (isTouchingWall && !collisionDetector.IsGrounded && collisionDetector.WallDirection == movement.FacingDirection)
        {
            lastWallTime = settings.wallStickTime;
            
            // 벽에 닿으면 WallSliding으로 상태 변경
            if (CurrentMovementState != MovementStateType.WallSliding &&
                CurrentMovementState != MovementStateType.Dashing)
            {
                movementStateMachine.ChangeState(MovementStateType.WallSliding);
            }
        }
        else if ((!isTouchingWall || collisionDetector.WallDirection != movement.FacingDirection) && isWallSliding)
        {
            ExitWallSliding();
        }
    }

    private void ExitWallSliding()
    {
        isWallSliding = false;
        
        // 벽에서 떨어지면 Falling으로 상태 변경
        if (CurrentMovementState == MovementStateType.WallSliding && !collisionDetector.IsGrounded)
        {
            movementStateMachine.ChangeState(MovementStateType.Falling);
        }
    }

    private void HandleDirectionChanged(int direction)
    {
        // 방향이 바뀌면 벽 감지 방향도 업데이트
        collisionDetector.SetFacingDirection(direction);

        // 스프린트 중 방향이 바뀌면 스프린트 취소
        if (isSprinting && CurrentMovementState == MovementStateType.Running)
        {
            isSprinting = false;
        }
        
        // 벽 슬라이딩 중 방향이 바뀌면 벽 슬라이딩 취소
        if (isWallSliding && direction != collisionDetector.WallDirection)
        {
            ExitWallSliding();
        }
    }

    private void HandleLadderTouchChanged(bool isOnLadder)
    {
        // 사다리에 접촉한 경우 (선택적으로 자동 진입 가능)
        if (isOnLadder && inputHandler.MoveDirection.y != 0)
        {
            // 일부 상태에서는 자동으로 사다리 오르기 상태로 전환할 수 있음
            if (CurrentMovementState == MovementStateType.Falling || 
                CurrentMovementState == MovementStateType.Jumping)
            {
                EnterClimbingState();
            }
        }
        // 사다리에서 벗어난 경우
        else if (!isOnLadder && isClimbing)
        {
            ExitClimbingState(false);
        }
    }

    // 앉기 입력 체크 메서드
    private void CheckCrouchInput()
    {
        bool canEnterCrouch = collisionDetector.IsGrounded && 
                             inputHandler.IsDownPressed &&
                             CurrentMovementState != MovementStateType.Crouching &&
                             CurrentMovementState != MovementStateType.Dashing;
        
        if (canEnterCrouch)
        {
            EnterCrouchState();
        }
    }

    // 앉기 상태 진입 메서드
    private void EnterCrouchState()
    {
        if (!isCrouching)
        {
            isCrouching = true;
            movementStateMachine.ChangeState(MovementStateType.Crouching);
            OnCrouchStateChanged?.Invoke(true);
            
            // 앉기 애니메이션 상태 설정
            playerAnimator?.SetCrouching(true);
        }
    }

    // 앉기 상태 종료 메서드
    public void ExitCrouchState()
    {
        if (isCrouching)
        {
            isCrouching = false;
            OnCrouchStateChanged?.Invoke(false);
            
            // 앉기 애니메이션 상태 해제
            playerAnimator?.SetCrouching(false);
            
            // 플레이어가 천장과 충돌하지 않는지 확인 후 상태 전환
            if (CanStandUp())
            {
                movementStateMachine.ChangeState(inputHandler.IsMoving() ? MovementStateType.Running : MovementStateType.Idle);
            }
        }
    }

    // 일어설 수 있는지 확인하는 메서드
    private bool CanStandUp()
    {
        // 필요 시 천장 충돌 체크 로직 구현
        // 여기서는 간단하게 true 반환
        return true;
    }

    // 사다리 입력 체크 메서드
    private void CheckClimbInput()
    {
        bool canEnterClimb = collisionDetector.IsOnLadder && 
                             Mathf.Abs(inputHandler.MoveDirection.y) > 0.1f &&
                             CurrentMovementState != MovementStateType.Climbing &&
                             CurrentMovementState != MovementStateType.Dashing;
        
        if (canEnterClimb)
        {
            EnterClimbingState();
        }
    }

    // 사다리 오르기 상태 진입 메서드
    private void EnterClimbingState()
    {
        if (!isClimbing)
        {
            isClimbing = true;
            movementStateMachine.ChangeState(MovementStateType.Climbing);
            OnClimbStateChanged?.Invoke(true);
            
            // 사다리 오르기 애니메이션 설정
            playerAnimator?.SetClimbing(true);
        }
    }

    // 사다리 오르기 상태 종료 메서드
    public void ExitClimbingState(bool shouldJump)
    {
        if (isClimbing)
        {
            isClimbing = false;
            OnClimbStateChanged?.Invoke(false);
            
            // 사다리 오르기 애니메이션 해제
            playerAnimator?.SetClimbing(false);
            
            if (shouldJump)
            {
                // 점프로 사다리를 내리는 경우
                isJumping = true;
                movement.Jump(settings.jumpForce * 0.7f); // 사다리에서 점프는 일반 점프보다 약하게
                movementStateMachine.ChangeState(MovementStateType.Jumping);
            }
            else
            {
                // 그냥 사다리에서 내리는 경우
                if (collisionDetector.IsGrounded)
                {
                    movementStateMachine.ChangeState(inputHandler.IsMoving() ? MovementStateType.Running : MovementStateType.Idle);
                }
                else
                {
                    movementStateMachine.ChangeState(MovementStateType.Falling);
                }
            }
        }
    }
    public void HandleDamage(float damage)
    {
        if (isDashing) return;

        var playerHP = GetComponent<PlayerHP>();
        if (playerHP != null)
        {
            playerHP.TakeDamage(damage);
        }

        // 상태 전환 및 기타 처리
        movementStateMachine.ChangeState(MovementStateType.Death);
        Debug.Log($"플레이어가 {damage} 데미지를 받았습니다.");
    }
    
    // 유틸리티 메서드
    public PlayerSettings GetSettings() => settings;
    public PlayerInputHandler GetInputHandler() => inputHandler;
    public PlayerMovement GetMovement() => movement;
    public CollisionDetector GetCollisionDetector() => collisionDetector;

    public void SetJumping(bool value) => isJumping = value;
    public void SetWallSliding(bool value) => isWallSliding = value;
    public void SetSprinting(bool value) => isSprinting = value;
    public void SetCrouching(bool value) => isCrouching = value;
    public void SetClimbing(bool value) => isClimbing = value;
    
    // 공격 상태머신과 이동 상태머신 접근자
    public PlayerMovementStateMachine GetMovementStateMachine() => movementStateMachine;
    public PlayerAttackStateMachine GetAttackStateMachine() => attackStateMachine;

    private void UpdateAnimatorParameters()
    {
        if (animator == null) return;
        bool isAttacking = (CurrentAttackState != AttackStateType.None);
        bool isMoving = inputHandler != null && inputHandler.IsMoving();
        animator.SetBool("IsAttacking", isAttacking);
        animator.SetBool("IsMoving", isMoving);
    }
}