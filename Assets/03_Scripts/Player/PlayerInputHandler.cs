using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerInputHandler : MonoBehaviour, PlayerInput.IPlayerActions
{
    [SerializeField] private float doubleTapTime = 0.5f;

    private PlayerInput playerInputs;

    // 방향 입력 상태
    public bool IsLeftPressed { get; private set; }
    public bool IsRightPressed { get; private set; }
    public bool IsUpPressed { get; private set; }
    public bool IsDownPressed { get; private set; }

    // 동작 입력 상태
    public bool JumpPressed { get; private set; }
    public bool JumpReleased { get; private set; } = true;
    public bool DashPressed { get; private set; }

    // 더블 탭 관련
    private float lastLeftTapTime;
    private float lastRightTapTime;
    private float lastUpTapTime;
    public bool LeftDoubleTapped { get; private set; }
    public bool RightDoubleTapped { get; private set; }
    public bool UpDoubleTapped { get; private set; }

    // 이벤트
    public event Action<bool> OnJumpInput;
    public event Action OnJumpRelease;
    public event Action OnDashInput;
    public event Action OnSprintActivated;
    public event Action<Vector2, bool> OnNormalAttack;    // 방향, 연타 여부
    public event Action<Vector2, bool> OnChargedAttack;   // 방향, 오버차지 여부
    public event Action OnCrouchInput;
    public event Action OnWingsuitActivated;
    public event Action OnAttackInput;    // PlayerStateManager에서 필요한 공격 입력 이벤트

    private Vector2 moveDirection;
    public Vector2 MoveDirection => moveDirection;

    // 공격 관련 상태
    private bool isAttackButtonHeld = false;
    private float attackButtonHoldTime = 0f;
    private float lastAttackTime = 0f;

    private WeaponManager weaponManager;

    private void Awake()
    {
        playerInputs = new PlayerInput();

        // 콜백 설정
        playerInputs.Player.SetCallbacks(this);
        
        // WeaponManager 참조 가져오기
        weaponManager = WeaponManager.Instance;
    }

    private void OnEnable()
    {
        playerInputs.Player.Enable();
        Debug.Log("PlayerInput이 활성화되었습니다.");
        
        // 이벤트 구독 상태 확인
        if (OnSprintActivated == null)
        {
            Debug.LogWarning("OnSprintActivated 이벤트에 구독된 리스너가 없습니다!");
        }
    }

    private void OnDisable()
    {
        playerInputs.Player.Disable();
    }

    private void Update()
    {
        // 더블 탭 상태 업데이트
        CheckDoubleTaps();

        // 이동 벡터 업데이트
        UpdateMovementVector();
        
        // 공격 버튼 홀드 시간 업데이트
        if (isAttackButtonHeld)
        {
            attackButtonHoldTime += Time.deltaTime;
            
            // WeaponManager의 차징 업데이트
            if (weaponManager != null)
            {
                weaponManager.UpdateCharging(Time.deltaTime);
            }
        }
    }

    private void UpdateMovementVector()
    {
        // 좌우 이동 방향 계산
        float horizontalMovement = 0f;
        if (IsRightPressed) horizontalMovement += 1f;
        if (IsLeftPressed) horizontalMovement -= 1f;

        // 수직 이동 방향 계산
        float verticalMovement = 0f;
        if (IsUpPressed) verticalMovement += 1f;
        if (IsDownPressed) verticalMovement -= 1f;

        // 이동 벡터 설정
        moveDirection = new Vector2(horizontalMovement, verticalMovement).normalized;
    }

    private void CheckDoubleTaps()
    {
        // 현재 시간 기준으로 더블 탭 만료 체크
        float currentTime = Time.time;

        // 왼쪽 방향키 더블 탭 만료 체크
        if (currentTime - lastLeftTapTime > doubleTapTime)
        {
            LeftDoubleTapped = false;
        }

        // 오른쪽 방향키 더블 탭 만료 체크
        if (currentTime - lastRightTapTime > doubleTapTime)
        {
            RightDoubleTapped = false;
        }
        
        // 위쪽 방향키 더블 탭 만료 체크
        if (currentTime - lastUpTapTime > doubleTapTime)
        {
            UpDoubleTapped = false;
        }
    }

    // PlayerInput.IPlayerActions 인터페이스 구현
    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled)
        {
            Vector2 inputVector = context.ReadValue<Vector2>();

            // 이전 상태 저장
            bool wasLeftPressed = IsLeftPressed;
            bool wasRightPressed = IsRightPressed;
            bool wasUpPressed = IsUpPressed;

            // 입력 상태 업데이트
            IsLeftPressed = inputVector.x < -0.1f;
            IsRightPressed = inputVector.x > 0.1f;
            IsUpPressed = inputVector.y > 0.1f;
            IsDownPressed = inputVector.y < -0.1f;

            // 더블 탭 체크
            if (!wasLeftPressed && IsLeftPressed)
            {
                float timeSinceLastTap = Time.time - lastLeftTapTime;
                if (timeSinceLastTap <= doubleTapTime)
                {
                    LeftDoubleTapped = true;
                    Debug.Log("왼쪽 방향키 더블 탭 감지!");
                    OnSprintActivated?.Invoke();
                }
                lastLeftTapTime = Time.time;
            }

            if (!wasRightPressed && IsRightPressed)
            {
                float timeSinceLastTap = Time.time - lastRightTapTime;
                if (timeSinceLastTap <= doubleTapTime)
                {
                    RightDoubleTapped = true;
                    Debug.Log("오른쪽 방향키 더블 탭 감지!");
                    OnSprintActivated?.Invoke();
                }
                lastRightTapTime = Time.time;
            }
            
            if (!wasUpPressed && IsUpPressed)
            {
                float timeSinceLastTap = Time.time - lastUpTapTime;
                if (timeSinceLastTap <= doubleTapTime)
                {
                    UpDoubleTapped = true;
                    Debug.Log("위쪽 방향키 더블 탭 감지! 윙슈트 모드 전환!");
                    OnWingsuitActivated?.Invoke();
                }
                lastUpTapTime = Time.time;
            }
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            JumpPressed = true;
            JumpReleased = false;
            OnJumpInput?.Invoke(true);
        }
        else if (context.canceled)
        {
            JumpReleased = true;
            JumpPressed = false;
            OnJumpRelease?.Invoke();
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            DashPressed = true;
            OnDashInput?.Invoke();
        }
        else if (context.canceled)
        {
            DashPressed = false;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("공격 입력 감지");
            isAttackButtonHeld = true;
            attackButtonHoldTime = 0f;
            
            // 차징 시작
            if (weaponManager != null)
            {
                weaponManager.StartCharging();
            }
            
            // 공격 입력 이벤트 호출
            OnAttackInput?.Invoke();
        }
        else if (context.canceled)
        {
            isAttackButtonHeld = false;
            
            // 버튼을 뗐을 때 차지 상태에 따라 공격 결정
            Vector2 direction = GetAttackDirection();
            
            if (attackButtonHoldTime < 0.3f)
            {
                // 짧게 눌렀을 때 - 일반 공격
                if (weaponManager != null)
                {
                    weaponManager.FireWeapon(direction);
                }
                OnNormalAttack?.Invoke(direction, false);
            }
            else if (attackButtonHoldTime >= 0.3f)
            {
                // 차징된 공격 발사
                if (weaponManager != null)
                {
                    weaponManager.ReleaseCharge(direction);
                }
                
                // 오버차지 여부 확인 (2초 이상 차지)
                bool isOvercharged = attackButtonHoldTime >= 2.0f;
                OnChargedAttack?.Invoke(direction, isOvercharged);
            }
            
            lastAttackTime = Time.time;
            attackButtonHoldTime = 0f;
        }
    }

    // 공격 방향 계산 (플레이어가 바라보는 방향 기준)
    private Vector2 GetAttackDirection()
    {
        // 플레이어의 이동 방향 컴포넌트 얻기
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            // 플레이어가 바라보는 방향으로 설정
            return new Vector2(playerMovement.FacingDirection, 0).normalized;
        }
        
        // 기본적으로 오른쪽 방향 반환
        return Vector2.right;
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("앉기 입력 감지");
            OnCrouchInput?.Invoke();
        }
    }

    public void OnNext(InputAction.CallbackContext context)
    {
        // 키를 처음 눌렀을 때만(started) 한 번 실행
        if (context.started)
        {
            // 선택 입력 처리A, S 로 작동
            PlayerUI.Instance.MovetoLeftType();
        }
    }
    
    public void OnPrev(InputAction.CallbackContext context)
    {
        // 키를 처음 눌렀을 때만(started) 한 번 실행
        if (context.started)
        {
            // 선택 입력 처리A, S 로 작동
            PlayerUI.Instance.MovetoRightType();
        }
    }

    public void OnSpecialAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("특수 공격 입력 감지");
            // 특수 공격 처리
        }
    }

    public bool IsMoving()
    {
        return IsLeftPressed || IsRightPressed;
    }

    public void ResetInputState()
    {
        JumpPressed = false;
        DashPressed = false;
    }
}