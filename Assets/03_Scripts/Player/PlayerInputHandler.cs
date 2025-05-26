using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


public class PlayerInputHandler : MonoBehaviour, PlayerInput. IPlayerActions
{
    private BaseObject baseObject;

    private bool isInteracting = false;

    public bool IsInteracting
    {
        get => isInteracting;
        set => isInteracting = value;
    }


    [SerializeField] private float interactionRadius = 2f; // 상호작용 가능 범위
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
    
    // 공격 입력 상태
    public bool IsAttackPressed { get; private set; }
    public bool IsChargingAttack { get; private set; }

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
    public event Action OnAttackInput;
    public event Action OnAttackRelease;
    public event Action OnChargeAttackStart;
    public event Action OnChargeAttackRelease;
    public event Action OnCrouchInput;
    public event Action OnWingsuitActivated;

    private Vector2 moveDirection;
    public Vector2 MoveDirection => moveDirection;

    private void Awake()
    {
        playerInputs = new PlayerInput();

        // 콜백 설정
        playerInputs.Player.SetCallbacks(this);
    }

    private void OnEnable()
    {
        playerInputs.Player.Enable();
        Debug.Log("PlayerInput이 활성화되었습니다.");
        
        // 이벤트 구독 상태 확인 - 디버깅용 로그 제거
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
        if (isInteracting == true)
            return;

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
            Debug.Log("공격 입력 감지 - 차징 시작");
            IsAttackPressed = true;
            
            // 이전에 차징 중이었다면 먼저 발사 처리
            if (IsChargingAttack)
            {
                Debug.Log("이전 차징 중단 및 발사");
                IsChargingAttack = false;
                OnAttackRelease?.Invoke();
                OnChargeAttackRelease?.Invoke();
                
                // WeaponManager null 체크 추가
                if (WeaponManager.Instance != null)
                {
                    WeaponManager.Instance.StopCharging();
                    // 약간의 지연 후 새로운 차징 시작
                    StartCoroutine(StartNewChargingAfterDelay(0.05f));
                }
            }
            else
            {
                // 일반적인 차징 시작
                IsChargingAttack = true;
                OnAttackInput?.Invoke();
                OnChargeAttackStart?.Invoke();
                
                // WeaponManager null 체크 추가
                if (WeaponManager.Instance != null)
                {
                    WeaponManager.Instance.StartCharging();
                }
            }
        }
        else if (context.canceled)
        {
            Debug.Log("공격 입력 해제 - 발사");
            IsAttackPressed = false;
            
            // 차징 중이었을 때만 발사 처리
            if (IsChargingAttack)
            {
                IsChargingAttack = false;
                OnAttackRelease?.Invoke();
                OnChargeAttackRelease?.Invoke();
                
                // WeaponManager null 체크 추가
                if (WeaponManager.Instance != null)
                {
                    WeaponManager.Instance.StopCharging();
                }
            }
        }
    }

    // 약간의 지연 후 새로운 차징 시작
    private IEnumerator StartNewChargingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // 여전히 공격 버튼이 눌려있는 경우에만 새로운 차징 시작
        if (IsAttackPressed)
        {
            Debug.Log("새로운 차징 시작");
            IsChargingAttack = true;
            OnAttackInput?.Invoke();
            OnChargeAttackStart?.Invoke();
            
            // WeaponManager null 체크 추가
            if (WeaponManager.Instance != null)
            {
                WeaponManager.Instance.StartCharging();
            }
        }
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

    // 다른 오브젝트와 충돌 감지
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 상호작용 가능한 오브젝트 확인
        BaseObject obj = other.GetComponent<BaseObject>();
        if (obj != null)
        {
            baseObject = obj;
        }
    }

    // 충돌 종료 감지
    private void OnTriggerExit2D(Collider2D other)
    {
        // 상호작용 가능한 오브젝트에서 벗어났는지 확인
        BaseObject obj = other.GetComponent<BaseObject>();
        if (obj != null && obj == baseObject)
        {
            baseObject = null;
        }
    }
    
    // 가장 가까운 상호작용 가능한 오브젝트 찾기
    private BaseObject FindNearestInteractableObject()
    {
        // 주변의 모든 콜라이더 탐색
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
        
        BaseObject nearestObject = null;
        float minDistance = interactionRadius;
        
        foreach (Collider2D collider in colliders)
        {
            BaseObject obj = collider.GetComponent<BaseObject>();
            if (obj != null)
            {
                float distance = Vector2.Distance(transform.position, obj.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestObject = obj;
                }
            }
        }
        
        return nearestObject;
    }

    public void OnInteraction(InputAction.CallbackContext context)
    {
        IsInteracting = !IsInteracting;

        if (context.started)
        {
            Debug.Log("상호작용 입력 감지");

            GameObject interactor = this.gameObject;

            // 이미 감지된 오브젝트가 없으면 가장 가까운 오브젝트 찾기
            if (baseObject == null)
            {
                baseObject = FindNearestInteractableObject();
            }

            if (baseObject != null)
            {
                baseObject.TryInteract(interactor);
            }

            else
            {
                Debug.LogWarning("상호작용 가능한 오브젝트가 범위 내에 없습니다.");
            }
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
        IsAttackPressed = false;
        IsChargingAttack = false;
    }

    public void OnInventory(InputAction.CallbackContext context)
    {
        if (isInteracting == true)
            return;

        if (context.started)
        {
            UIManager.Instance.inputUI.OpenInventory();
        }
    }

    public void OnMap(InputAction.CallbackContext context)
    {
        if (isInteracting == true)
            return;

        if (context.started)
        {
            UIManager.Instance.inputUI.OpenMap();
        }
    }

    public void OnPauseMenu(InputAction.CallbackContext context)
    {
        if (isInteracting == true)
            return;

        if (context.started)
        {
            UIManager.Instance.inputUI.OpenPauseMenu();
        }
    }
}