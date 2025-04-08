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

    // 이벤트
    public event Action<PlayerStateType> OnStateChanged;

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
        states.Add(PlayerStateType.Hit, new PlayerHitState(this));
        // 초기 상태 설정
        ChangeState(PlayerStateType.Idle);
    }

    private void Update()
    {
        // 타이머 업데이트
        UpdateTimers();

        // 상태 관련 자동 전환 확인
        CheckStateTransitions();

        // 현재 상태 업데이트
        currentState?.HandleInput();
        currentState?.Update();

        // 애니메이션 업데이트
        UpdateAnimation();
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
        // Idle 상태에서 이동 입력이 있으면 Running으로 전환
        if (currentStateType == PlayerStateType.Idle && inputHandler.IsMoving() && collisionDetector.IsGrounded)
        {
            ChangeState(PlayerStateType.Running);
        }

        // Running 상태에서 이동 입력이 없으면 Idle로 전환
        else if (currentStateType == PlayerStateType.Running && !inputHandler.IsMoving() && collisionDetector.IsGrounded)
        {
            ChangeState(PlayerStateType.Idle);
        }

        // 지상에 있지 않고 낙하 중이면 Falling으로 전환
        else if (currentStateType != PlayerStateType.Jumping &&
                 currentStateType != PlayerStateType.WallSliding &&
                 currentStateType != PlayerStateType.Dashing &&
                 currentStateType != PlayerStateType.Falling &&
                 !collisionDetector.IsGrounded &&
                 movement.Velocity.y < 0)
        {
            ChangeState(PlayerStateType.Falling);
        }

        // 벽에 붙어있으면 WallSliding으로 전환
        else if (currentStateType != PlayerStateType.WallSliding &&
                 currentStateType != PlayerStateType.Dashing &&
                 collisionDetector.IsTouchingWall &&
                 !collisionDetector.IsGrounded)
        {
            ChangeState(PlayerStateType.WallSliding);
        }
    }

    public void ChangeState(PlayerStateType newState)
    {
        // 같은 상태면 무시
        if (currentStateType == newState) return;

        // 이전 상태 종료
        currentState?.Exit();

        // 새 상태로 변경
        currentStateType = newState;
        currentState = states[newState];
        currentState.Enter();

        // 상태 변경 이벤트 발생
        OnStateChanged?.Invoke(newState);
        Debug.Log($"상태 변경: {newState}");
    }

    private void HandleJumpInput(bool pressed)
    {
        if (pressed)
        {
            lastJumpTime = settings.jumpBufferTime;

            // 벽 슬라이딩 중이면 벽 점프
            if (isWallSliding)
            {
                movement.WallJump(settings.wallJumpForce, settings.wallJumpDirection);
                ChangeState(PlayerStateType.Jumping);
                lastJumpTime = 0;
            }
            // 점프 가능한 상태이면 점프
            else if (lastGroundedTime > 0)
            {
                isJumping = true;
                movement.Jump(settings.jumpForce);
                ChangeState(PlayerStateType.Jumping);
                lastJumpTime = 0;
            }
        }
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

    // 공격 입력 처리 메서드 추가:
    private void HandleAttackInput()
    {
        // 대시 중에는 공격 불가
        if (isDashing) return;
        
        // 현재 공격 중이라면, 연속 공격 가능한지 체크
        if (currentStateType == PlayerStateType.Attacking)
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
        if (isTouchingWall && !collisionDetector.IsGrounded)
        {
            lastWallTime = settings.wallStickTime;

            // 벽에 닿으면 WallSliding으로 상태 변경
            if (currentStateType != PlayerStateType.WallSliding &&
                currentStateType != PlayerStateType.Dashing)
            {
                ChangeState(PlayerStateType.WallSliding);
            }
        }
        else if (!isTouchingWall && isWallSliding)
        {
            isWallSliding = false;

            // 벽에서 떨어지면 Falling으로 상태 변경
            if (currentStateType == PlayerStateType.WallSliding && !collisionDetector.IsGrounded)
            {
                ChangeState(PlayerStateType.Falling);
            }
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
    }

    private void UpdateAnimation()
    {
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
}