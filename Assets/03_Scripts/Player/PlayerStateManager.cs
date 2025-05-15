using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateManager : MonoBehaviour
{
    [SerializeField] private PlayerSettings settings;

    private Dictionary<PlayerStateType, IPlayerState> states = new Dictionary<PlayerStateType, IPlayerState>();
    private IPlayerState currentState;
    private PlayerStateType currentStateType;

    // 컴포넌트 참조
    private PlayerInputHandler inputHandler;
    private PlayerMovement movement;
    private CollisionDetector collisionDetector;
    private PlayerAnimator playerAnimator;

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

    private int lastWallDirection = 1; // 벽 방향 저장용

    
    // 프로퍼티
    public PlayerStateType CurrentState => currentStateType;
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
    public event Action<PlayerStateType> OnStateChanged;
    public event Action<bool> OnCrouchStateChanged;
    public event Action<bool> OnClimbStateChanged;

    private void Awake()
    {
        // 컴포넌트 참조 설정
        inputHandler = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        collisionDetector = GetComponent<CollisionDetector>();
        playerAnimator = GetComponent<PlayerAnimator>();
        // 상태 초기화
        InitializeStates();
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

    private void InitializeStates()
    {
        // 상태 객체 생성 및 등록
        // 실제 구현에서는 각 상태 클래스 구현 필요
        states.Add(PlayerStateType.Idle, new PlayerIdleState(this));
        states.Add(PlayerStateType.Running, new PlayerRunningState(this));
        states.Add(PlayerStateType.Sprinting, new PlayerSprintingState(this));
        states.Add(PlayerStateType.Jumping, new PlayerJumpingState(this));
        states.Add(PlayerStateType.Falling, new PlayerFallingState(this));
        states.Add(PlayerStateType.WallSliding, new PlayerWallSlidingState(this));
        states.Add(PlayerStateType.Dashing, new PlayerDashingState(this));
        states.Add(PlayerStateType.Attacking, new PlayerAttackingState(this));
        states.Add(PlayerStateType.MoveAttacking, new PlayerMoveAttackingState(this));
        states.Add(PlayerStateType.Hit, new PlayerHitState(this));
        states.Add(PlayerStateType.Crouching, new PlayerCrouchingState(this));
        states.Add(PlayerStateType.Climbing, new PlayerClimbingState(this));
        states.Add(PlayerStateType.Death, new PlayerDeathState(this));
        // 초기 상태 설정
        ChangeState(PlayerStateType.Idle);
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

        // 현재 상태 업데이트
        currentState?.HandleInput();
        currentState?.Update();

        // 애니메이션 업데이트
        UpdateAnimation();

        // 공격 입력에 따라 IsAttacking 파라미터 업데이트
        if (playerAnimator != null && playerAnimator.GetAnimator() != null)
        {
            playerAnimator.GetAnimator().SetBool("IsAttacking", inputHandler.IsAttackPressed);
        }
    }

    private void FixedUpdate()
    {
        currentState?.FixedUpdate();
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
        if (CheckCrouchingTransition() || 
            CheckMovementTransition() || 
            CheckFallingTransition() || 
            CheckWallSlidingTransition())
        {
            return; // 상태 전환이 발생했으면 추가 검사 중단
        }
    }
    
    private bool CheckCrouchingTransition()
    {
        // Crouching 상태에서 앉기 입력이 해제되고 지상에 있으면 Idle로 전환
        if (currentStateType == PlayerStateType.Crouching && !inputHandler.IsDownPressed && collisionDetector.IsGrounded)
        {
            ChangeState(inputHandler.IsMoving() ? PlayerStateType.Running : PlayerStateType.Idle);
            return true;
        }
        return false;
    }
    
    private bool CheckMovementTransition()
    {
        // Idle 상태에서 이동 입력이 있으면 Running으로 전환
        if (currentStateType == PlayerStateType.Idle && inputHandler.IsMoving() && collisionDetector.IsGrounded)
        {
            ChangeState(PlayerStateType.Running);
            return true;
        }
        // Running 상태에서 이동 입력이 없으면 Idle로 전환
        else if (currentStateType == PlayerStateType.Running && !inputHandler.IsMoving() && collisionDetector.IsGrounded)
        {
            ChangeState(PlayerStateType.Idle);
            return true;
        }
        return false;
    }
    
    private bool CheckFallingTransition()
    {
        bool shouldSkipFallingCheck = currentStateType == PlayerStateType.Jumping ||
                              currentStateType == PlayerStateType.WallSliding ||
                              currentStateType == PlayerStateType.Dashing ||
                              currentStateType == PlayerStateType.Falling ||
                              currentStateType == PlayerStateType.Climbing;
        
        // 지상에 있지 않고 낙하 중이면 Falling으로 전환
        if (!shouldSkipFallingCheck && !collisionDetector.IsGrounded && movement.Velocity.y < 0)
        {
            ChangeState(PlayerStateType.Falling);
            return true;
        }
        return false;
    }
    
    private bool CheckWallSlidingTransition()
    {
        bool canEnterWallSlide = currentStateType != PlayerStateType.WallSliding &&
                               currentStateType != PlayerStateType.Dashing &&
                               currentStateType != PlayerStateType.Climbing;
        
        bool shouldExitWallSlide = isWallSliding && 
                                  (!collisionDetector.IsTouchingWall || 
                                   collisionDetector.WallDirection != movement.FacingDirection);
        
        // 벽 슬라이딩 진입 조건
        if (canEnterWallSlide && 
            collisionDetector.IsTouchingWall && 
            !collisionDetector.IsGrounded && 
            collisionDetector.WallDirection == movement.FacingDirection)
        {
            ChangeState(PlayerStateType.WallSliding);
            return true;
        }
        // 벽 슬라이딩 탈출 조건
        else if (shouldExitWallSlide)
        {
            ExitWallSliding();
            return true;
        }
        
        // 이미 벽 슬라이딩 중일 때 방향과 벽 방향 불일치 확인
        if (currentStateType == PlayerStateType.WallSliding && 
            (movement.FacingDirection != collisionDetector.WallDirection || !collisionDetector.IsTouchingWall))
        {
            ExitWallSliding();
            return true;
        }
        
        return false;
    }
    
    private void ExitWallSliding()
    {
        isWallSliding = false;
        
        // 벽에서 떨어지면 Falling으로 상태 변경
        if (currentStateType == PlayerStateType.WallSliding && !collisionDetector.IsGrounded)
        {
            ChangeState(PlayerStateType.Falling);
        }

        // 벽 슬라이딩에서 벗어날 때 WeaponManager에 벽 슬라이딩 상태와 벽 방향 전달
        WeaponManager.Instance.SetWallSlideInfo(false, 0);
    }

    public void ChangeState(PlayerStateType newState)
    {
        // 같은 상태면 무시
        if (currentStateType == newState) 
        {
            return;
        }

        // 사망 상태에서 Idle로 전환 시(부활 처리) 처리
        if (currentStateType == PlayerStateType.Death && newState == PlayerStateType.Idle)
        {
            HandleRespawn();
        }

        // 이전 상태 종료
        currentState?.Exit();

        // 새 상태로 변경
        currentStateType = newState;
        currentState = states[newState];
        currentState.Enter();

        // 상태 변경 이벤트 발생
        OnStateChanged?.Invoke(newState);
        Debug.Log($"상태 변경: {newState}");

        // 벽 슬라이딩 진입 시 WeaponManager에 벽 슬라이딩 상태와 벽 방향 전달
        if (newState == PlayerStateType.WallSliding)
        {
            lastWallDirection = collisionDetector.WallDirection;
            WeaponManager.Instance.SetWallSlideInfo(true, lastWallDirection);
        }
        // 벽 슬라이딩에서 벗어날 때
        else if (currentStateType == PlayerStateType.WallSliding)
        {
            WeaponManager.Instance.SetWallSlideInfo(false, 0);
        }
    }

    private void HandleRespawn()
    {
        // 애니메이터 상태 확인 및 리셋
        var playerAnimator = GetComponent<PlayerAnimator>();
        if (playerAnimator != null)
        {
            playerAnimator.SetDead(false);
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
        ChangeState(PlayerStateType.Jumping);
        lastJumpTime = 0;
    }
    
    private void PerformNormalJump()
    {
        isJumping = true;
        movement.Jump(settings.jumpForce);
        ChangeState(PlayerStateType.Jumping);
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
        ChangeState(PlayerStateType.Dashing);

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
            ChangeState(PlayerStateType.Falling);
        }
        else
        {
            ChangeState(inputHandler.IsMoving() ? PlayerStateType.Running : PlayerStateType.Idle);
        }

        // 대시 쿨다운
        yield return new WaitForSeconds(settings.dashCooldown);
        canDash = true;
    }

    private void HandleSprintActivated()
    {
        if (currentStateType == PlayerStateType.Running && collisionDetector.IsGrounded)
        {
            isSprinting = true;
            ChangeState(PlayerStateType.Sprinting);
        }
    }

    private void HandleAttackInput()
    {
        // 대시 중에는 공격 불가
        if (isDashing) return;
        
        // 이동+공격 입력 시 MoveAttacking 상태로 전환
        if (inputHandler.IsMoving() && inputHandler.IsAttackPressed)
        {
            ChangeState(PlayerStateType.MoveAttacking);
        }
        // 현재 공격 중이라면, 연속 공격 가능한지 체크
        else if (currentStateType == PlayerStateType.Attacking)
        {
            PlayerAttackingState attackState = states[PlayerStateType.Attacking] as PlayerAttackingState;
            if (attackState != null && attackState.CanAttack())
            {
                // 현재 상태 재진입으로 연속 공격
                currentState.Exit();
                currentState.Enter();
            }
        }
        else
        {
            // 공격 상태로 전환
            ChangeState(PlayerStateType.Attacking);
        }
    }
    
    private void HandleGroundedChanged(bool isGrounded)
    {
        if (isGrounded)
        {
            lastGroundedTime = settings.coyoteTime;

            // 공중에서 지면에 착지
            if (currentStateType == PlayerStateType.Falling ||
                currentStateType == PlayerStateType.Jumping)
            {
                // 이동 중이면 Running, 아니면 Idle
                ChangeState(inputHandler.IsMoving() ? PlayerStateType.Running : PlayerStateType.Idle);
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
            if (currentStateType != PlayerStateType.WallSliding &&
                currentStateType != PlayerStateType.Dashing)
            {
                ChangeState(PlayerStateType.WallSliding);
            }
        }
        else if ((!isTouchingWall || collisionDetector.WallDirection != movement.FacingDirection) && isWallSliding)
        {
            ExitWallSliding();
        }
    }

    private void HandleDirectionChanged(int direction)
    {
        // 방향이 바뀌면 벽 감지 방향도 업데이트
        collisionDetector.SetFacingDirection(direction);

        // 스프린트 중 방향이 바뀌면 스프린트 취소
        if (isSprinting && currentStateType == PlayerStateType.Sprinting)
        {
            isSprinting = false;
            ChangeState(PlayerStateType.Running);
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
            if (currentStateType == PlayerStateType.Falling || 
                currentStateType == PlayerStateType.Jumping)
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

    private void UpdateAnimation()
    {
        // 사망 상태에서는 애니메이션 업데이트를 한 번만 실행하도록
        if (currentStateType == PlayerStateType.Death && currentState is PlayerDeathState deathState && deathState.HasRespawned)
        {
            // 이미 리스폰 처리된 상태에서는 더 이상 애니메이션 업데이트 하지 않음
            return;
        }
        
        playerAnimator?.UpdateAnimation(currentStateType, inputHandler.IsMoving(), movement.Velocity);
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

    // 피격 처리 메서드
    public void TakeDamage(float damage)
    {
        // 현재 대시 중이거나 무적 상태이면 데미지를 무시할 수 있음
        if (isDashing) return;

        // PlayerHP 컴포넌트가 있다면 데미지 적용
        var playerHP = GetComponent<PlayerHP>();
        if (playerHP != null)
        {
            playerHP.TakeDamage(damage);
        }

        // Hit 상태로 전환
        ChangeState(PlayerStateType.Hit);
        
        Debug.Log($"플레이어가 {damage} 데미지를 받았습니다.");
    }

    // 앉기 입력 체크 메서드
    private void CheckCrouchInput()
    {
        bool canEnterCrouch = collisionDetector.IsGrounded && 
                             inputHandler.IsDownPressed &&
                             currentStateType != PlayerStateType.Crouching &&
                             currentStateType != PlayerStateType.Dashing &&
                             currentStateType != PlayerStateType.Attacking;
        
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
            ChangeState(PlayerStateType.Crouching);
            OnCrouchStateChanged?.Invoke(true);
            
            // 앉기 애니메이션 상태 설정
            playerAnimator?.SetCrouching(true);
            
            // 앉기 상태에서의 콜라이더 크기 조절 등은 PlayerCrouchingState에서 처리
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
                ChangeState(inputHandler.IsMoving() ? PlayerStateType.Running : PlayerStateType.Idle);
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
                             currentStateType != PlayerStateType.Climbing &&
                             currentStateType != PlayerStateType.Dashing;
        
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
            ChangeState(PlayerStateType.Climbing);
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
                ChangeState(PlayerStateType.Jumping);
            }
            else
            {
                // 그냥 사다리에서 내리는 경우
                if (collisionDetector.IsGrounded)
                {
                    ChangeState(inputHandler.IsMoving() ? PlayerStateType.Running : PlayerStateType.Idle);
                }
                else
                {
                    ChangeState(PlayerStateType.Falling);
                }
            }
        }
    }
}