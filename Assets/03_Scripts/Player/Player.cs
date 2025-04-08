using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections;
public enum PlayerStateType
{
    Idle,
    Running,
    Sprinting,
    Jumping,
    Falling,
    WallSliding,
    Dashing
}

public class PlayerController : MonoBehaviour, PlayerInput.IPlayerActions
{
    #region 직렬화된 변수들
    [Header("Movement Parameters")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float sprintMultiplier = 1.5f; // 스프린트 속도 배율
    [SerializeField] private float doubleTapTime = 0.3f; // 더블 탭 감지 시간
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
    [SerializeField] private float wallSlideAcceleration = 5f; // 벽 슬라이드 가속도
    [SerializeField] private float wallFastSlideSpeed = 6f; // 빠른 벽 슬라이드 속도 (아래 방향키)
    [SerializeField] private float wallJumpForce = 12f;
    [SerializeField] private Vector2 wallJumpDirection = new Vector2(1f, 1.5f);
    [SerializeField] private float wallStickTime = 0.2f; // 벽에 붙어있는 시간

    // 레이어 마스크
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    // 디버깅
    [SerializeField] private bool showDebugRays = true;
    #endregion

    #region 컴포넌트 참조
    // 컴포넌트 참조 - 접근 제한자 수정
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private Animator animator;

    // 컴포넌트에 대한 제한된 접근 제공
    public Rigidbody2D Rb => rb;
    public BoxCollider2D BoxCollider => boxCollider;
    #endregion

    // Input System
    private PlayerInput playerInputs;

    #region 입력 관련 변수들
    // 방향 입력 변수 (bool)
    private bool isLeftPressed;
    private bool isRightPressed;
    private bool isUpPressed;
    private bool isDownPressed;

    // 내부 계산용 이동 벡터
    private Vector2 moveDirection;

    // 더블 탭 감지를 위한 변수
    private float lastLeftTapTime;
    private float lastRightTapTime;
    private bool leftDoubleTapped;
    private bool rightDoubleTapped;
    #endregion

    #region 상태 변수들
    // 타이머 변수들
    private float lastGroundedTime;
    private float lastJumpTime;
    private float lastWallTime;
    private float lastDashTime; // 마지막 대시 시간

    // 상태 플래그들
    private bool isJumping;
    private bool isWallSliding;
    private bool isDashing;
    private bool isSprinting;
    private bool canDash = true;
    private int facingDirection = 1;

    // 입력 플래그들
    private bool jumpPressed;
    private bool jumpReleased = true;
    private bool dashPressed;

    // 상태 변수에 대한 접근 속성
    public float LastGroundedTime { get => lastGroundedTime; set => lastGroundedTime = value; }
    public float LastJumpTime { get => lastJumpTime; set => lastJumpTime = value; }
    public float LastWallTime { get => lastWallTime; set => lastWallTime = value; }
    public float LastDashTime { get => lastDashTime; }
    public bool IsJumping { get => isJumping; set => isJumping = value; }
    public bool IsWallSliding { get => isWallSliding; set => isWallSliding = value; }
    public bool IsDashing { get => isDashing; set => isDashing = value; }
    public bool IsSprinting { get => isSprinting; set => isSprinting = value; }
    public bool CanDash { get => canDash; set => canDash = value; }
    public int FacingDirection { get => facingDirection; set => facingDirection = value; }
    public bool JumpPressed { get => jumpPressed; set => jumpPressed = value; }
    public bool JumpReleased { get => jumpReleased; set => jumpReleased = value; }
    public bool DashPressed { get => dashPressed; set => dashPressed = value; }
    #endregion

    #region 상태 머신 관련
    // 상태 머신 구현
    private Dictionary<PlayerStateType, IPlayerState> states = new Dictionary<PlayerStateType, IPlayerState>();
    private IPlayerState currentState;
    private PlayerStateType currentStateType;
    #endregion

    #region 캡슐화된 파라미터 접근자
    // 직렬화된 변수에 대한 접근 속성
    public float JumpForce => jumpForce;
    public float JumpCutMultiplier => jumpCutMultiplier;
    public float FallGravityMultiplier => fallGravityMultiplier;
    public float JumpBufferTime => jumpBufferTime;
    public float WallSlideSpeed => wallSlideSpeed;
    public float WallFastSlideSpeed => wallFastSlideSpeed;
    public float WallSlideAcceleration => wallSlideAcceleration;
    public float WallJumpForce => wallJumpForce;
    public Vector2 WallJumpDirection => wallJumpDirection;
    public float WallStickTime => wallStickTime;
    public float MoveSpeed => moveSpeed;
    public float DashSpeed => dashSpeed;
    public float DashDuration => dashDuration;
    public float DashCooldown => dashCooldown;
    #endregion

    private float inputCheckTimer = 0f;
    private bool hasLoggedInput = false;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponentInChildren<Animator>();

        // Input System 초기화
        playerInputs = new PlayerInput();
        playerInputs.Player.SetCallbacks(this);

        // 상태 초기화
        InitializeStates();
    }

    private void Start()
    {
        // Input System 활성화 확인 (추가 안전장치)
        try
        {
            if (playerInputs != null)
            {
                playerInputs.Player.Enable();
                Debug.Log("PlayerInput이 Start에서 활성화되었습니다.");
            }
            else
            {
                Debug.LogError("PlayerInput이 null입니다. 입력이 작동하지 않을 수 있습니다.");
                // 새로 생성 시도
                playerInputs = new PlayerInput();
                playerInputs.Player.SetCallbacks(this);
                playerInputs.Enable();
                Debug.Log("PlayerInput을 재생성하여 활성화했습니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Input System 초기화 중 오류 발생: {e.Message}");
        }
        
        // 시작 상태 확인
        Debug.Log($"시작 상태: {currentStateType}, 입력 정보: 왼쪽={isLeftPressed}, 오른쪽={isRightPressed}");
        
        // 임시 해결책: 짧은 딜레이 후 입력 시스템 리셋
        //Invoke("ResetInputSystem", 0.2f);
    }

    // 입력 시스템 리셋 (시작 시 문제 해결용)
    private void ResetInputSystem()
    {
        Debug.Log("입력 시스템 리셋 시도...");
        
        try
        {
            // 입력 시스템 다시 활성화
            if (playerInputs != null)
            {
                playerInputs.Player.Disable();
                playerInputs.Player.Enable();
                Debug.Log("입력 시스템 다시 활성화됨");
            }
            
            // 응급 처치: 입력 프로세스 강제 진행
            // 키보드가 감지되었는지 확인
            if (Keyboard.current != null)
            {
                Debug.Log("키보드 감지됨, 입력 테스트 중...");
                
                // 현재 키보드 상태 확인
                if (Keyboard.current.anyKey.isPressed)
                {
                    Debug.Log("키 입력이 감지됨");
                }
                
                // 강제 움직임 테스트
                Invoke("ForceMovementTest", 0.1f);
            }
            else
            {
                Debug.LogWarning("키보드가 감지되지 않았습니다!");
                // 키보드가 없어도 강제 테스트 시도
                Invoke("ForceMovementTest", 0.2f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"입력 시스템 리셋 중 오류 발생: {e.Message}");
            // 오류가 발생해도 강제 움직임은 시도
            Invoke("ForceMovementTest", 0.3f);
        }
    }
    
    // 강제 움직임 테스트 (시작 시 문제 해결용)
    private void ForceMovementTest()
    {
        Debug.Log("강제 움직임 테스트 실행");
        // 더미 입력 생성 - 임시 테스트용
        isRightPressed = true;
        
        // 이동 벡터 업데이트
        UpdateMovementVector();
        
        // Move 메서드 직접 호출
        Move();
        
        // 디버그용 로그
        Debug.Log($"강제 움직임: moveDirection={moveDirection}, velocity={rb.velocity}");
        
        // 0.5초 후 상태 변경
        Invoke("CheckMovementAfterForce", 0.5f);
    }
    
    // 강제 움직임 후 상태 확인
    private void CheckMovementAfterForce()
    {
        Debug.Log($"강제 움직임 후 상태: state={currentStateType}, velocity={rb.velocity}, isMoving={IsMoving()}");
        
        // 이동 중이지만 아직 Running 상태가 아니라면 상태 변경
        if (IsMoving() && currentStateType == PlayerStateType.Idle)
        {
            Debug.Log("상태 강제 변경: Idle → Running");
            ChangeState(PlayerStateType.Running);
        }
    }

    private void InitializeStates()
    {
        // 모든 상태 인스턴스 생성
        states.Add(PlayerStateType.Idle, new PlayerIdleState(this));
        states.Add(PlayerStateType.Running, new PlayerRunningState(this));
        states.Add(PlayerStateType.Sprinting, new PlayerSprintingState(this));
        states.Add(PlayerStateType.Jumping, new PlayerJumpingState(this));
        states.Add(PlayerStateType.Falling, new PlayerFallingState(this));
        states.Add(PlayerStateType.WallSliding, new PlayerWallSlidingState(this));
        states.Add(PlayerStateType.Dashing, new PlayerDashingState(this));

        // 초기 상태 설정
        ChangeState(PlayerStateType.Idle);
    }

    private void OnEnable()
    {
        playerInputs.Player.Enable();
        Debug.Log("PlayerInput이 OnEnable에서 활성화되었습니다.");
    }

    private void OnDisable()
    {
        playerInputs.Player.Disable();
    }

    private void Update()
    {
        // 입력 시스템 정기 확인 (처음 3초 동안만)
        if (Time.time < 3f && !hasLoggedInput)
        {
            inputCheckTimer -= Time.deltaTime;
            if (inputCheckTimer <= 0f)
            {
                inputCheckTimer = 0.5f; // 0.5초마다 체크
                
                // 현재 키보드 상태 확인 
                if (Keyboard.current != null)
                {
                    bool anyKeyPressed = Keyboard.current.anyKey.isPressed;
                    
                    // 이미 움직이고 있다면 로그 중지
                    if (rb.velocity.magnitude > 0.1f || currentStateType != PlayerStateType.Idle)
                    {
                        hasLoggedInput = true;
                        Debug.Log("캐릭터가 움직이기 시작했습니다. 입력 확인 종료.");
                    }
                    else if (anyKeyPressed)
                    {
                        // 키가 눌렸지만 이동하지 않는 경우
                        Debug.Log("키 입력 감지됨, 하지만 캐릭터는 움직이지 않습니다. 상태를 점검합니다.");
                        
                        // 방향키가 눌렸는지 추가 확인
                        bool leftKeyPressed = Keyboard.current.leftArrowKey.isPressed;
                        bool rightKeyPressed = Keyboard.current.rightArrowKey.isPressed;
                        
                        // 플래그와 실제 키 상태 일치 확인
                        if ((leftKeyPressed != isLeftPressed) || (rightKeyPressed != isRightPressed))
                        {
                            Debug.LogWarning($"입력 불일치 발견: 왼쪽키={leftKeyPressed} vs 플래그={isLeftPressed}, 오른쪽키={rightKeyPressed} vs 플래그={isRightPressed}");
                            
                            // 플래그 강제 수정
                            isLeftPressed = leftKeyPressed;
                            isRightPressed = rightKeyPressed;
                            UpdateMovementVector();
                            
                            // 상태 변경 시도
                            if (IsMoving() && currentStateType == PlayerStateType.Idle)
                            {
                                Debug.Log("입력 수정 후 강제 상태 변경: Idle → Running");
                                ChangeState(PlayerStateType.Running);
                            }
                        }
                    }
                }
            }
        }
        
        // 입력에 따른 이동 방향 벡터 계산
        UpdateMovementVector();

        // Idle 상태에서 이동 입력이 감지되면 즉시 Running 상태로 전환 (추가된 부분)
        if (currentStateType == PlayerStateType.Idle && IsMoving() && IsGrounded())
        {
            Debug.Log("Idle 상태에서 이동 감지: Idle → Running 상태로 즉시 전환");
            ChangeState(PlayerStateType.Running);
        }

        // 더블 탭 감지 체크
        CheckDoubleTaps();

        // 지면 검사 및 코요테 타임 갱신
        if (IsGrounded())
        {
            lastGroundedTime = coyoteTime;
        }
        else
        {
            lastGroundedTime -= Time.deltaTime;
        }

        // 벽 검사 및 벽 시간 갱신
        UpdateWallDetection();

        // 점프 버퍼 타임 감소
        if (lastJumpTime > 0)
        {
            lastJumpTime -= Time.deltaTime;
            
            // 점프 버퍼 타임이 만료되면 점프 입력도 초기화
            if (lastJumpTime <= 0 && !isJumping)
            {
                jumpPressed = false;
            }
        }

        // 스프린트 상태 조정
        UpdateSprintState();

        // 상태 업데이트
        currentState?.HandleInput();
        currentState?.Update();
        
        // 매 프레임 애니메이션 파라미터 업데이트 (추가)
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }

    // 키 입력에 따른 이동 벡터 계산
    private void UpdateMovementVector()
    {
        // 좌우 이동 방향 계산 (좌우 동시 입력 시 최근 입력 우선)
        float horizontalMovement = 0f;
        if (isRightPressed) horizontalMovement += 1f;
        if (isLeftPressed) horizontalMovement -= 1f;

        // 수직 이동 방향 계산
        float verticalMovement = 0f;
        if (isUpPressed) verticalMovement += 1f;
        if (isDownPressed) verticalMovement -= 1f;

        // 이동 벡터 설정
        moveDirection = new Vector2(horizontalMovement, verticalMovement).normalized;
    }

    // 더블 탭 감지 로직
    private void CheckDoubleTaps()
    {
        // 더블 탭 상태 초기화
        leftDoubleTapped = false;
        rightDoubleTapped = false;

        // 현재 시간 기준으로 더블 탭 만료 체크
        float currentTime = Time.time;

        // 왼쪽 방향키 더블 탭 만료 체크
        if (currentTime - lastLeftTapTime > doubleTapTime)
        {
            leftDoubleTapped = false;
        }

        // 오른쪽 방향키 더블 탭 만료 체크
        if (currentTime - lastRightTapTime > doubleTapTime)
        {
            rightDoubleTapped = false;
        }
    }

    // 스프린트 상태 업데이트
    private void UpdateSprintState()
    {
        // 달리기 상태가 아니거나 땅에 닿지 않은 경우 스프린트 비활성화
        if (!IsMoving() || !IsGrounded())
        {
            isSprinting = false;

            // 달리는 중이 아닌데 스프린트 상태면 상태 변경
            if (currentStateType == PlayerStateType.Sprinting)
            {
                if (IsGrounded())
                {
                    ChangeState(PlayerStateType.Idle);
                }
                else
                {
                    ChangeState(PlayerStateType.Falling);
                }
            }
        }

        // 스프린트 상태에서 움직임이 바뀌면 스프린트 종료
        if (isSprinting &&
            ((facingDirection == 1 && !isRightPressed) ||
             (facingDirection == -1 && !isLeftPressed)))
        {
            isSprinting = false;

            if (currentStateType == PlayerStateType.Sprinting)
            {
                ChangeState(PlayerStateType.Running);
            }
        }
    }

    // 이동 중인지 확인하는 헬퍼 메서드
    public bool IsMoving()
    {
        return isLeftPressed || isRightPressed;
    }

    // 이동 방향 벡터 반환
    public Vector2 GetMovementVector()
    {
        return moveDirection;
    }

    // 현재 진행 방향의 입력이 눌렸는지 확인
    public bool IsMovingInFacingDirection()
    {
        return (facingDirection == 1 && isRightPressed) ||
               (facingDirection == -1 && isLeftPressed);
    }

    public void ChangeState(PlayerStateType newState)
    {
        // 이미 같은 상태면 무시
        if (currentStateType == newState)
        {
            return;
        }

        // 현재 상태 종료
        currentState?.Exit();

        // 새 상태로 변경
        currentStateType = newState;
        currentState = states[newState];

        // 새 상태 시작
        currentState.Enter();

        // 애니메이션 파라미터 업데이트 (트리거 대신 기본 파라미터 사용)
        UpdateAnimations(null);

        // 디버그 로그
        Debug.Log($"Changed to state: {newState}");
    }

    // 공통 유틸리티 메서드
    public bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            new Vector2(boxCollider.bounds.size.x * 0.95f, 0.2f),
            0f,
            Vector2.down,
            0.5f,
            groundLayer
        );

        if (showDebugRays)
        {
            Debug.DrawRay(
                boxCollider.bounds.center + new Vector3(boxCollider.bounds.extents.x * 0.9f, -boxCollider.bounds.extents.y, 0),
                Vector2.down * 0.2f,
                Color.red
            );
            Debug.DrawRay(
                boxCollider.bounds.center - new Vector3(boxCollider.bounds.extents.x * 0.9f, boxCollider.bounds.extents.y, 0),
                Vector2.down * 0.2f,
                Color.red
            );
        }

        return hit.collider != null;
    }

    public bool IsTouchingWall()
    {
        // 벽 감지 영역 확장 및 정밀도 향상
        RaycastHit2D hit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            new Vector2(0.2f, boxCollider.bounds.size.y * 0.8f), // 너비를 0.1f에서 0.2f로 확장
            0f,
            new Vector2(facingDirection, 0),
            0.3f, // 감지 거리를 0.2f에서 0.3f로 증가
            wallLayer
        );
        
        // 명확한 디버그 로그 추가
        if (hit.collider != null)
        {
            Debug.Log($"벽 감지됨! collider={hit.collider.name}, 거리={hit.distance}, 위치={hit.point}");
        }

        // 디버그 레이 더 선명하게 표시
        if (showDebugRays)
        {
            // 벽 감지 시 색상 변경
            Color rayColor = hit.collider != null ? Color.green : Color.blue;
            
            // 위쪽 레이
            Debug.DrawRay(
                boxCollider.bounds.center + new Vector3(boxCollider.bounds.extents.x * facingDirection, boxCollider.bounds.extents.y * 0.8f, 0),
                new Vector3(0.3f * facingDirection, 0, 0), // 감지 거리 일치
                rayColor,
                0.1f // 레이 지속 시간 설정
            );
            
            // 중간 레이 추가
            Debug.DrawRay(
                boxCollider.bounds.center + new Vector3(boxCollider.bounds.extents.x * facingDirection, 0, 0),
                new Vector3(0.3f * facingDirection, 0, 0),
                rayColor,
                0.1f
            );
            
            // 아래쪽 레이
            Debug.DrawRay(
                boxCollider.bounds.center + new Vector3(boxCollider.bounds.extents.x * facingDirection, -boxCollider.bounds.extents.y * 0.8f, 0),
                new Vector3(0.3f * facingDirection, 0, 0),
                rayColor,
                0.1f
            );
            
            // 박스캐스트 영역 시각화
            Debug.DrawLine(
                boxCollider.bounds.center + new Vector3(boxCollider.bounds.extents.x * facingDirection, boxCollider.bounds.extents.y * 0.8f, 0),
                boxCollider.bounds.center + new Vector3(boxCollider.bounds.extents.x * facingDirection, -boxCollider.bounds.extents.y * 0.8f, 0),
                rayColor,
                0.1f
            );
        }
        
        // 벽 레이어 확인 디버그 (처음 호출 시 한 번만)
        if (Time.frameCount % 60 == 0) // 약 1초마다 로그
        {
            int layerBit = 1 << LayerMask.NameToLayer("Wall");
            Debug.Log($"벽 레이어 설정 확인: wallLayer={wallLayer.value}, Wall 레이어 비트={layerBit}, 일치={wallLayer == layerBit}");
            
            // 씬에 있는 벽 오브젝트 개수 확인
            GameObject[] wallObjects = GameObject.FindGameObjectsWithTag("Wall");
            Debug.Log($"씬에 'Wall' 태그가 있는 오브젝트 수: {wallObjects.Length}");
        }

        return hit.collider != null;
    }
    
    // 벽 감지 상태를 업데이트하고 처리하는 메서드 추가
    private void UpdateWallDetection()
    {
        // 벽 감지 테스트 - 이동 방향과 벽 사이 충돌 체크
        bool isTouchingWallNow = IsTouchingWall();
        
        if (isTouchingWallNow && !IsGrounded())
        {
            lastWallTime = wallStickTime;
            
            // 벽 감지 시 디버그 로그
            if (!isWallSliding)
            {
                Debug.Log($"벽에 접촉! facingDirection={facingDirection}, 상태={currentStateType}");
            }

            // 벽에 닿았을 때 상태 전환
            if (currentStateType != PlayerStateType.WallSliding &&
                currentStateType != PlayerStateType.Dashing)
            {
                Debug.Log("벽 슬라이딩 상태로 전환합니다.");
                ChangeState(PlayerStateType.WallSliding);
            }
        }
        else
        {
            // 벽에서 떨어졌을 때 상태 업데이트
            if (isWallSliding && lastWallTime <= 0)
            {
                Debug.Log("벽 슬라이딩 종료: 벽에서 떨어짐");
                isWallSliding = false;
                
                // 공중에 떠 있으면 Falling 상태로 전환
                if (!IsGrounded() && currentStateType == PlayerStateType.WallSliding)
                {
                    Debug.Log("Falling 상태로 전환합니다.");
                    ChangeState(PlayerStateType.Falling);
                }
            }
            
            lastWallTime -= Time.deltaTime;
        }
    }

    public void UpdateAnimations(string stateType = null)
    {
        if (animator != null)
        {
            try
            {
                Debug.Log($"애니메이션 업데이트: 현재 상태={currentStateType}, IsMoving={IsMoving()}, Velocity={rb.velocity}");
                
                // 각 파라미터 설정 전에 파라미터 존재 여부 확인
                // IsMoving 업데이트: 방향 입력 상태와 속도 모두 확인
                if (HasParameter("IsMoving"))
                {
                    bool isMoving = IsMoving() && Mathf.Abs(rb.velocity.x) > 0.1f;
                    animator.SetBool("IsMoving", isMoving);
                    Debug.Log($"IsMoving 파라미터를 {isMoving}로 설정");
                }
                
                // 직접 상태별 파라미터 설정
                /*if (HasParameter("IsIdle"))
                    animator.SetBool("IsIdle", currentStateType == PlayerStateType.Idle);
                
                if (HasParameter("IsRunning"))
                    animator.SetBool("IsRunning", currentStateType == PlayerStateType.Running);*/
                
                if (HasParameter("IsSprinting"))
                    animator.SetBool("IsSprinting", isSprinting || currentStateType == PlayerStateType.Sprinting);
                
                if (HasParameter("IsJumping"))
                    animator.SetBool("IsJumping", currentStateType == PlayerStateType.Jumping);
                
                if (HasParameter("IsFalling"))
                    animator.SetBool("IsFalling", currentStateType == PlayerStateType.Falling);
                
                if (HasParameter("IsWallSliding"))
                    animator.SetBool("IsWallSliding", isWallSliding || currentStateType == PlayerStateType.WallSliding);
                
                if (HasParameter("IsDashing"))
                    animator.SetBool("IsDashing", isDashing || currentStateType == PlayerStateType.Dashing);
                
                // Animator 업데이트 직접 호출 (필요시)
                // animator.Update(0);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"애니메이션 파라미터 설정 중 오류 발생: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("Animator 컴포넌트가 null입니다!");
        }
    }
    
    // 애니메이터에 해당 파라미터가 존재하는지 확인하는 헬퍼 메서드
    private bool HasParameter(string paramName)
    {
        if (animator == null)
            return false;
            
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        
        Debug.LogWarning($"애니메이터에 '{paramName}' 파라미터가 존재하지 않습니다.");
        return false;
    }

    // 이동 처리 - 모든 상태에서 공통으로 사용 가능
    public void Move()
    {
        // 방향 변경
        if (moveDirection.x != 0)
        {
            facingDirection = (int)Mathf.Sign(moveDirection.x);
            transform.localScale = new Vector3(facingDirection, 1, 1);
        }

        // 이동 입력이 없으면 빠르게 감속
        if (Mathf.Abs(moveDirection.x) < 0.01f && IsGrounded())
        {
            // 속도가 매우 작으면 완전히 멈춤
            if (Mathf.Abs(rb.velocity.x) < 0.6f)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
                return;
            }

            // 강한 마찰력 적용
            float friction = Mathf.Min(Mathf.Abs(rb.velocity.x), frictionAmount * 2);
            friction *= Mathf.Sign(rb.velocity.x);
            rb.AddForce(Vector2.right * -friction, ForceMode2D.Impulse);
            return;
        }

        // 목표 속도 (스프린트 상태 반영)
        float currentMoveSpeed = moveSpeed;
        if (isSprinting)
        {
            currentMoveSpeed *= sprintMultiplier;
        }

        float targetSpeed = moveDirection.x * currentMoveSpeed;

        // 현재 속도와 목표 속도 간의 차이
        float speedDiff = targetSpeed - rb.velocity.x;

        // 가속도 결정 (방향 전환 시 빠르게 반응)
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        // 비선형 가속을 위한 승수 적용
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velocityPower) * Mathf.Sign(speedDiff);

        // 이동 힘 적용
        rb.AddForce(movement * Vector2.right);
    }

    // 점프 처리
    public void Jump()
    {
        Debug.Log("Jump executed with force: " + jumpForce);

        // 점프 버퍼와 코요테 타임 리셋
        lastJumpTime = 0;
        lastGroundedTime = 0;

        // 대시 직후 점프인지 확인
        bool jumpingAfterDash = Time.time - lastDashTime < 0.3f;
        float actualJumpForce = jumpForce;

        // 대시 직후 점프라면 속도 완전 초기화 및 점프 높이 제한
        if (jumpingAfterDash)
        {
            Debug.Log("Jump after dash - limiting height and resetting velocity");
            actualJumpForce = jumpForce;
            rb.velocity = Vector2.zero; // 모든 속도 리셋
        }
        else
        {
            // 일반 점프 시 수평 속도 제한만 적용
            float currentXVelocity = rb.velocity.x;
            if (Mathf.Abs(currentXVelocity) > moveSpeed * 1.5f)
            {
                currentXVelocity = Mathf.Sign(currentXVelocity) * moveSpeed * 1.5f;
            }
            rb.velocity = new Vector2(currentXVelocity, 0); // 수직 속도만 리셋
        }

        // 수직 속도 설정 (제한된 점프 힘 적용)
        rb.velocity = new Vector2(rb.velocity.x, actualJumpForce);

        isJumping = true;

        // 점프 효과음 재생
        // AudioManager.Instance.PlaySound("jump");
    }

    // 벽 점프 처리
    public void WallJump()
    {
        lastJumpTime = 0;
        isWallSliding = false;

        // 벽에서 반대 방향으로 점프
        Vector2 direction = new Vector2(-facingDirection * wallJumpDirection.x, wallJumpDirection.y);
        rb.velocity = Vector2.zero;
        rb.AddForce(direction * wallJumpForce, ForceMode2D.Impulse);

        // 벽 점프 후 잠시 방향 전환
        StartCoroutine(DisableMovement(0.2f));
    }

    // 벽 슬라이딩 처리
    public void WallSlide()
    {
        isWallSliding = true;

        // 기본 벽 슬라이딩 속도 설정
        float targetSlideSpeed = -wallSlideSpeed;

        // 아래 방향키를 누르고 있으면 빠르게 하강
        if (isDownPressed)
        {
            targetSlideSpeed = -wallFastSlideSpeed;
        }

        // 현재 낙하 속도가 목표보다 빠르면 속도 제한
        if (rb.velocity.y < targetSlideSpeed)
        {
            // 부드럽게 목표 속도로 감속
            float speedDif = targetSlideSpeed - rb.velocity.y;
            float movement = speedDif * wallSlideAcceleration;

            // 한 프레임당 최대 변화량 제한
            movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime),
                                          Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));

            rb.AddForce(movement * Vector2.up);
        }
    }

    private IEnumerator DisableMovement(float duration)
    {
        // 현재 상태 저장
        bool wasLeftPressed = isLeftPressed;
        bool wasRightPressed = isRightPressed;
        bool wasMovementDisabled = true;

        // 입력 비활성화
        isLeftPressed = false;
        isRightPressed = false;

        // 이동 방향 업데이트
        UpdateMovementVector();

        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(duration);

        // 입력 복원 (이미 다른 입력이 있는 경우 덮어쓰지 않음)
        if (wasMovementDisabled && !isLeftPressed && !isRightPressed)
        {
            isLeftPressed = wasLeftPressed;
            isRightPressed = wasRightPressed;

            // 이동 방향 업데이트
            UpdateMovementVector();

            Debug.Log($"입력 복원됨: 왼쪽={isLeftPressed}, 오른쪽={isRightPressed}");
        }
    }

    // 대시 시작 - 다른 클래스에서 호출할 수 있는 공용 메서드
    public void StartDash()
    {
        // 대시 가능한 상태인지 확인 및 중복 호출 방지
        if (canDash && !isDashing)
        {
            Debug.Log("StartDash: 대시 시작");
            
            // 상태 관리 변수 설정
            canDash = false;
            dashPressed = false; // 입력 소비
            lastDashTime = Time.time;
            
            // 대시 쿨다운 관리 코루틴 시작
            StartCoroutine(DashCooldownManager());
            
            // 대시 상태로 전환
            ChangeState(PlayerStateType.Dashing);
        }
        else
        {
            Debug.Log($"StartDash: 대시 불가 (canDash: {canDash}, isDashing: {isDashing})");
        }
    }
    
    // 대시 쿨다운 독립적으로 관리하는 코루틴
    private IEnumerator DashCooldownManager()
    {
        // 대시 쿨다운 (지속 시간 + 쿨다운 시간)
        Debug.Log($"DashCooldownManager: Started, total cooldown: {dashDuration + dashCooldown}s");
        yield return new WaitForSeconds(dashDuration + dashCooldown);
        
        // 쿨다운 종료 후 대시 가능 상태로 설정
        canDash = true;
        Debug.Log("DashCooldownManager: Completed, CanDash set to true");
    }

    #region Input System Callbacks

    public void OnMove(InputAction.CallbackContext context)
    {
        
        // 현재 키보드 입력 상태 읽기
        Vector2 inputVector = ReadKeyboardInput();
        
        // 상태 업데이트는 performed 또는 canceled 시에만
        if (context.performed || context.canceled)
        {
            // 이전 상태 저장
            bool wasMoving = IsMoving();
            
            // 입력 상태 업데이트
            UpdateInputStates(inputVector);
            
            // 이동 벡터 업데이트
            UpdateMovementVector();
            
            // 상태 변경 처리
            HandleMovementStateChange(wasMoving);
        }
    }

    // 키보드 입력 읽기 - 분리하여 재사용성 높임
    private Vector2 ReadKeyboardInput()
    {
        Vector2 inputVector = Vector2.zero;
        
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed) inputVector.x -= 1;
            if (Keyboard.current.rightArrowKey.isPressed) inputVector.x += 1;
            if (Keyboard.current.upArrowKey.isPressed) inputVector.y += 1;
            if (Keyboard.current.downArrowKey.isPressed) inputVector.y -= 1;
            
            // 디버그: 키 입력 감지 확인 (문제 진단용)
            if ((Keyboard.current.leftArrowKey.isPressed || Keyboard.current.rightArrowKey.isPressed) && 
                currentStateType == PlayerStateType.Idle)
            {
                Debug.Log($"방향키 감지됨 (왼쪽: {Keyboard.current.leftArrowKey.isPressed}, 오른쪽: {Keyboard.current.rightArrowKey.isPressed})");
            }
        }
        
        return inputVector;
    }

    // 입력 상태 업데이트 및 더블 탭 처리
    private void UpdateInputStates(Vector2 inputVector)
    {
        // 왼쪽 방향키 처리
        bool wasLeftPressed = isLeftPressed;
        isLeftPressed = inputVector.x < -0.1f;
        
        // 새로 눌림 감지되면 더블 탭 체크
        if (!wasLeftPressed && isLeftPressed) 
        {
            CheckDoubleTap(DirectionType.Left);
            
            // 즉시 상태 확인 및 변경 (추가된 부분)
            if (currentStateType == PlayerStateType.Idle && IsGrounded())
            {
                Debug.Log("왼쪽 방향키 감지: Idle → Running 상태로 즉시 전환");
                ChangeState(PlayerStateType.Running);
            }
        }
        
        // 오른쪽 방향키 처리
        bool wasRightPressed = isRightPressed;
        isRightPressed = inputVector.x > 0.1f;
        
        // 새로 눌림 감지되면 더블 탭 체크  
        if (!wasRightPressed && isRightPressed)
        {
            CheckDoubleTap(DirectionType.Right);
            
            // 즉시 상태 확인 및 변경 (추가된 부분)
            if (currentStateType == PlayerStateType.Idle && IsGrounded())
            {
                Debug.Log("오른쪽 방향키 감지: Idle → Running 상태로 즉시 전환");
                ChangeState(PlayerStateType.Running);
            }
        }
        
        // 위/아래 방향키 처리
        isUpPressed = inputVector.y > 0.1f;
        isDownPressed = inputVector.y < -0.1f;
    }

    // 방향별 더블 탭 로직 통합
    private void CheckDoubleTap(DirectionType direction)
    {
        float lastTapTime = (direction == DirectionType.Left) ? lastLeftTapTime : lastRightTapTime;
        float timeSinceLastTap = Time.time - lastTapTime;
        
        // 더블 탭 조건 충족 시
        if (timeSinceLastTap <= doubleTapTime && IsGrounded())
        {
            if (direction == DirectionType.Left)
                leftDoubleTapped = true;
            else
                rightDoubleTapped = true;
            
            isSprinting = true;
            
            // 달리기 중일 때만 스프린트 상태로 변경
            if (currentStateType == PlayerStateType.Running)
                ChangeState(PlayerStateType.Sprinting);
        }
        
        // 마지막 탭 시간 업데이트
        if (direction == DirectionType.Left)
            lastLeftTapTime = Time.time;
        else
            lastRightTapTime = Time.time;
    }

    // 이동 상태 변경 로직 분리
    private void HandleMovementStateChange(bool wasMoving)
    {
        bool isMovingNow = IsMoving();
        
        // 이동 시작: 대기 → 달리기
        if (!wasMoving && isMovingNow && currentStateType == PlayerStateType.Idle)
        {
            ChangeState(PlayerStateType.Running);
        }
        // 이동 중지: 달리기/스프린트 → 대기
        else if (wasMoving && !isMovingNow && 
                (currentStateType == PlayerStateType.Running || 
                 currentStateType == PlayerStateType.Sprinting))
        {
            ChangeState(PlayerStateType.Idle);
        }
    }

    // 방향 타입 정의 (열거형)
    private enum DirectionType
    {
        Left,
        Right
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jumpPressed = true;
            jumpReleased = false;
            lastJumpTime = jumpBufferTime;
            Debug.Log("Jump Pressed!");

            // 벽 슬라이딩 중 점프 처리
            if (isWallSliding)
            {
                WallJump();
                ChangeState(PlayerStateType.Jumping);
                jumpPressed = false; // 입력 소비
                lastJumpTime = 0; // 버퍼 시간 초기화
            }
            // 대시 상태일 때는 점프 입력만 저장 (Dash 코루틴 종료 후 처리)
            else if (isDashing)
            {
                // 입력만 저장하고 대시가 끝날 때까지 기다림
                Debug.Log("Jump input during dash - waiting for dash to complete");
            }
            // 일반 점프 처리
            else if (lastGroundedTime > 0)
            {
                Jump();
                ChangeState(PlayerStateType.Jumping);
                jumpPressed = false; // 입력 소비
                lastJumpTime = 0; // 버퍼 시간 초기화
            }
        }
        else if (context.canceled)
        {
            jumpReleased = true;
            Debug.Log("Jump Released!");

            // 점프 컷 처리 (점프 중 버튼을 떼면 상승 속도 감소)
            if (isJumping && rb.velocity.y > 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
            }
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // 이미 대시 중이거나 쿨다운 중이면 무시
            if (isDashing || !canDash)
            {
                Debug.Log($"OnDash: 대시 불가 (isDashing: {isDashing}, canDash: {canDash})");
                return;
            }
            
            Debug.Log("OnDash: 대시 입력 감지");
            dashPressed = true;

            // 대시 가능 상태이면 StartDash 메서드 호출
            StartDash();
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