using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어 상태 열거형
public enum PlayerStateType
{
    Idle,
    Running,
    Jumping,
    Falling,
    WallSliding,
    Dashing
}

public class PlayerController : MonoBehaviour, PlayerControls.IPlayerActions
{
    [Header("Movement Parameters")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 60f;
    [SerializeField] private float velocityPower = 0.9f;
    [SerializeField] private float frictionAmount = 0.2f;

    [Header("Jump Parameters")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float fallGravityMultiplier = 1.7f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Dash Parameters")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.4f;

    [Header("Wall Parameters")]
    [SerializeField] private float wallSlideSpeed = 3f;
    [SerializeField] private float wallJumpForce = 12f;
    [SerializeField] private Vector2 wallJumpDirection = new Vector2(1f, 1.5f);

    // 컴포넌트 참조
    public Rigidbody2D Rb { get; private set; }
    public BoxCollider2D BoxCollider { get; private set; }
    public Animator Animator { get; private set; }

    // 레이어 마스크
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    // 디버깅
    [SerializeField] private bool showDebugRays = true;

    // Input System
    private PlayerControls playerControls;

    // 상태 변수 - 외부 상태에서 접근 가능하도록 public 속성으로 변경
    public Vector2 MoveInput { get; private set; }
    public float LastGroundedTime { get; set; }
    public float LastJumpTime { get; set; }
    public bool IsJumping { get; set; }
    public bool IsWallSliding { get; set; }
    public bool IsDashing { get; set; }
    public bool CanDash { get; set; } = true;
    public int FacingDirection { get; set; } = 1;
    public bool JumpPressed { get; set; }
    public bool JumpReleased { get; set; } = true;
    public bool DashPressed { get; set; }

    // 상태 머신 구현
    private Dictionary<PlayerStateType, IPlayerState> states = new Dictionary<PlayerStateType, IPlayerState>();
    private IPlayerState currentState;
    private PlayerStateType currentStateType;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        BoxCollider = GetComponent<BoxCollider2D>();
        Animator = GetComponentInChildren<Animator>();

        // Input System 초기화
        playerControls = new PlayerControls();
        playerControls.Player.SetCallbacks(this);

        // 상태 초기화
        InitializeStates();
    }

    private void InitializeStates()
    {
        // 모든 상태 인스턴스 생성
        states.Add(PlayerStateType.Idle, new PlayerIdleState(this));
        states.Add(PlayerStateType.Running, new PlayerRunningState(this));
        states.Add(PlayerStateType.Jumping, new PlayerJumpingState(this));
        states.Add(PlayerStateType.Falling, new PlayerFallingState(this));
        states.Add(PlayerStateType.WallSliding, new PlayerWallSlidingState(this));
        states.Add(PlayerStateType.Dashing, new PlayerDashingState(this));

        // 초기 상태 설정
        ChangeState(PlayerStateType.Idle);
    }

    private void OnEnable()
    {
        playerControls.Player.Enable();
    }

    private void OnDisable()
    {
        playerControls.Player.Disable();
    }

    private void Update()
    {
        // 지면 검사 및 코요테 타임 갱신
        if (IsGrounded())
        {
            LastGroundedTime = coyoteTime;
        }
        else
        {
            LastGroundedTime -= Time.deltaTime;
        }

        // 점프 버퍼 타임 감소
        if (LastJumpTime > 0)
        {
            LastJumpTime -= Time.deltaTime;
        }

        // 상태 업데이트
        currentState?.HandleInput();
        currentState?.Update();
    }

    private void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }

    public void ChangeState(PlayerStateType newState)
    {
        // 현재 상태 종료
        currentState?.Exit();

        // 새 상태로 변경
        currentStateType = newState;
        currentState = states[newState];

        // 새 상태 시작
        currentState.Enter();

        // 디버그 로그
        Debug.Log($"Changed to state: {newState}");
    }

    // 공통 유틸리티 메서드
    public bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.BoxCast(
            BoxCollider.bounds.center,
            new Vector2(BoxCollider.bounds.size.x * 0.95f, 0.2f),
            0f,
            Vector2.down,
            0.5f,
            groundLayer
        );

        if (showDebugRays)
        {
            Debug.DrawRay(
                BoxCollider.bounds.center + new Vector3(BoxCollider.bounds.extents.x * 0.9f, -BoxCollider.bounds.extents.y, 0),
                Vector2.down * 0.2f,
                Color.red
            );
            Debug.DrawRay(
                BoxCollider.bounds.center - new Vector3(BoxCollider.bounds.extents.x * 0.9f, BoxCollider.bounds.extents.y, 0),
                Vector2.down * 0.2f,
                Color.red
            );
        }

        return hit.collider != null;
    }

    public bool IsTouchingWall()
    {
        RaycastHit2D hit = Physics2D.BoxCast(
            BoxCollider.bounds.center,
            new Vector2(0.1f, BoxCollider.bounds.size.y * 0.8f),
            0f,
            new Vector2(FacingDirection, 0),
            0.2f,
            wallLayer
        );

        if (showDebugRays)
        {
            Debug.DrawRay(
                BoxCollider.bounds.center + new Vector3(BoxCollider.bounds.extents.x * FacingDirection, BoxCollider.bounds.extents.y * 0.8f, 0),
                new Vector3(0.2f * FacingDirection, 0, 0),
                Color.blue
            );
            Debug.DrawRay(
                BoxCollider.bounds.center + new Vector3(BoxCollider.bounds.extents.x * FacingDirection, -BoxCollider.bounds.extents.y * 0.8f, 0),
                new Vector3(0.2f * FacingDirection, 0, 0),
                Color.blue
            );
        }

        return hit.collider != null;
    }

    public void UpdateAnimations(string stateName = null)
    {
        if (Animator != null)
        {
            // 이동 애니메이션
            Animator.SetFloat("Speed", Mathf.Abs(Rb.velocity.x));

            // 점프/낙하 애니메이션
            Animator.SetFloat("VerticalVelocity", Rb.velocity.y);
            Animator.SetBool("IsGrounded", IsGrounded());

            // 벽 슬라이드 애니메이션
            Animator.SetBool("IsWallSliding", IsWallSliding);

            // 대시 애니메이션
            Animator.SetBool("IsDashing", IsDashing);

            // 상태별 특정 애니메이션 트리거 (필요한 경우)
            if (!string.IsNullOrEmpty(stateName))
            {
                Animator.SetTrigger(stateName);
            }
        }
    }

    // 이동 처리 - 모든 상태에서 공통으로 사용 가능
    public void Move()
    {
        // 방향 변경
        if (MoveInput.x != 0)
        {
            FacingDirection = (int)Mathf.Sign(MoveInput.x);
            transform.localScale = new Vector3(FacingDirection, 1, 1);
        }

        // 이동 입력이 없으면 빠르게 감속
        if (Mathf.Abs(MoveInput.x) < 0.01f && IsGrounded())
        {
            // 속도가 매우 작으면 완전히 멈춤
            if (Mathf.Abs(Rb.velocity.x) < 0.2f)
            {
                Rb.velocity = new Vector2(0f, Rb.velocity.y);
                return;
            }

            // 강한 마찰력 적용
            float friction = Mathf.Min(Mathf.Abs(Rb.velocity.x), frictionAmount * 2);
            friction *= Mathf.Sign(Rb.velocity.x);
            Rb.AddForce(Vector2.right * -friction, ForceMode2D.Impulse);
            return;
        }

        // 목표 속도
        float targetSpeed = MoveInput.x * moveSpeed;

        // 현재 속도와 목표 속도 간의 차이
        float speedDiff = targetSpeed - Rb.velocity.x;

        // 가속도 결정 (방향 전환 시 빠르게 반응)
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        // 비선형 가속을 위한 승수 적용
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velocityPower) * Mathf.Sign(speedDiff);

        // 이동 힘 적용
        Rb.AddForce(movement * Vector2.right);
    }

    // 점프 처리
    public void Jump()
    {
        Debug.Log("Jump executed with force: " + jumpForce);

        // 점프 버퍼와 코요테 타임 리셋
        LastJumpTime = 0;
        LastGroundedTime = 0;

        // 수직 속도를 직접 설정
        Rb.velocity = new Vector2(Rb.velocity.x, jumpForce);

        IsJumping = true;

        // 점프 효과음 재생
        // AudioManager.Instance.PlaySound("jump");
    }

    // 벽 점프 처리
    public void WallJump()
    {
        LastJumpTime = 0;
        IsWallSliding = false;

        // 벽에서 반대 방향으로 점프
        Vector2 direction = new Vector2(-FacingDirection * wallJumpDirection.x, wallJumpDirection.y);
        Rb.velocity = Vector2.zero;
        Rb.AddForce(direction * wallJumpForce, ForceMode2D.Impulse);

        // 벽 점프 후 잠시 방향 전환
        StartCoroutine(DisableMovement(0.2f));
    }

    // 대시 처리
    public IEnumerator Dash()
    {
        CanDash = false;
        IsDashing = true;

        // 대시 중 중력 비활성화
        float originalGravity = Rb.gravityScale;
        Rb.gravityScale = 0;

        // 대시 방향 결정 (입력이 없으면 현재 바라보는 방향)
        float dashDirection = (MoveInput.x != 0) ? Mathf.Sign(MoveInput.x) : FacingDirection;
        
        // 원래 입력값 저장
        Vector2 originalInput = MoveInput;

        // 대시 속도 설정
        Rb.velocity = new Vector2(dashDirection * dashSpeed, 0);

        // 대시 이펙트 생성
        // InstantiateDashEffect();

        yield return new WaitForSeconds(dashDuration);

        // 대시 종료 후 상태 변경
        IsDashing = false;
        Rb.gravityScale = originalGravity;
        
        // 이동 입력 복원 (이미 새 입력이 있는 경우 무시)
        if (MoveInput == Vector2.zero)
        {
            MoveInput = originalInput;
            Debug.Log($"대시 후 입력 복원: {MoveInput}");
        }

        // 대시 종료 후 적절한 상태로 전환
        if (IsGrounded())
        {
            if (Mathf.Abs(MoveInput.x) > 0.1f)
                ChangeState(PlayerStateType.Running);
            else
                ChangeState(PlayerStateType.Idle);
        }
        else
        {
            ChangeState(PlayerStateType.Falling);
        }

        // 대시 쿨다운
        yield return new WaitForSeconds(dashCooldown);
        CanDash = true;
    }

    private IEnumerator DisableMovement(float duration)
    {
        // 입력 무시 상태 저장
        Vector2 cachedInput = MoveInput;
        bool wasControlsDisabled = true;
        
        // 입력 비활성화
        MoveInput = Vector2.zero;

        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(duration);

        // 입력 복원 (이미 다른 입력이 있는 경우 덮어쓰지 않음)
        if (wasControlsDisabled && MoveInput == Vector2.zero)
        {
            MoveInput = cachedInput;
            Debug.Log($"입력 복원됨: {MoveInput}");
        }
    }

    #region Input System Callbacks

    public void OnMove(InputAction.CallbackContext context)
    {
        // 키보드 화살표 키 상태 확인
        Vector2 moveInput = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed)
                moveInput.x = -1;
            else if (Keyboard.current.rightArrowKey.isPressed)
                moveInput.x = 1;

            if (Keyboard.current.upArrowKey.isPressed)
                moveInput.y = 1;
            else if (Keyboard.current.downArrowKey.isPressed)
                moveInput.y = -1;
        }

        MoveInput = moveInput;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            JumpPressed = true;
            JumpReleased = false;
            LastJumpTime = jumpBufferTime;
            Debug.Log("Jump Pressed!");
        }
        else if (context.canceled)
        {
            JumpReleased = true;
            Debug.Log("Jump Released!");
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            DashPressed = true;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("Attack!");
            // 공격 로직 구현
        }
    }

    #endregion
}