using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerSettings settings;
    [SerializeField] private bool hasWingsuit = false; // 윙슈트 장착 여부

    // 필수 컴포넌트들
    private PlayerInputHandler inputHandler;
    private PlayerMovement movement;
    private CollisionDetector collisionDetector;
    private PlayerStateManager stateManager;
    private PlayerAnimator playerAnimator;
    private PlayerHP playerHP;

    public int utilityPoint;


    [HideInInspector] public float CurrentMoveSpeed { get; private set; } //현재 이동 속도
    [HideInInspector] public float CurrentJumpForce { get; private set; } //점프 높이
    [HideInInspector] public float CurrentMaxHP { get; private set; } //점프 높이

    [HideInInspector] public int CurrentUtilityPoint { get; private set; } //특성 포인트 보유 현황



    [HideInInspector]
    public ItemData CurrentattributeTypeData;

    private void Awake()
    {
        // 필요한 컴포넌트 추가
        EnsureComponents();

        utilityPoint = 0;
    }

    private void OnEnable()
    {
        // 이벤트 구독
        if (inputHandler != null)
        {
            inputHandler.OnSprintActivated += HandleSprint;
            inputHandler.OnWingsuitActivated += HandleWingsuitToggle;
        }
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        if (inputHandler != null)
        {
            inputHandler.OnSprintActivated -= HandleSprint;
            inputHandler.OnWingsuitActivated -= HandleWingsuitToggle;
        }
    }
    
    private void Start()
    {
        // 윙슈트 상태 초기화
        if (movement != null)
        {
            movement.HasWingsuit = hasWingsuit;
        }
    }
    
    // 윙슈트 토글 처리
    private void HandleWingsuitToggle()
    {
        if (!hasWingsuit) return;
        
        if (movement != null)
        {
            movement.CheckUpKeyDoubleTap();
        }
    }

    private void HandleSprint()
    {
        Debug.Log("스프린트 활성화!");
        if (movement != null)
        {
            movement.SetSprinting(true);
            
            // 애니메이션 업데이트
            if (playerAnimator != null)
            {
                playerAnimator.SetSprinting(true);
            }
            
            // 상태 매니저 업데이트
            if (stateManager != null)
            {
                stateManager.ChangeState(PlayerStateType.Sprinting);
            }
            
            // 일정 시간 후 스프린트 비활성화
            Invoke("DisableSprint", 1.5f);
        }
    }

    private void DisableSprint()
    {
        if (movement != null)
        {
            movement.SetSprinting(false);
            
            // 애니메이션 업데이트
            if (playerAnimator != null)
            {
                playerAnimator.SetSprinting(false);
            }
            
            // 상태 매니저 업데이트 (이동 중이면 Running, 아니면 Idle)
            if (stateManager != null && inputHandler != null)
            {
                if (inputHandler.IsMoving())
                {
                    stateManager.ChangeState(PlayerStateType.Running);
                }
                else
                {
                    stateManager.ChangeState(PlayerStateType.Idle);
                }
            }
        }
    }

    private void EnsureComponents()
    {
        // 컴포넌트 가져오기 또는 추가
        inputHandler = GetComponent<PlayerInputHandler>();
        if (inputHandler == null)
            inputHandler = gameObject.AddComponent<PlayerInputHandler>();

        movement = GetComponent<PlayerMovement>();
        if (movement == null)
            movement = gameObject.AddComponent<PlayerMovement>();

        collisionDetector = GetComponent<CollisionDetector>();
        if (collisionDetector == null)
            collisionDetector = gameObject.AddComponent<CollisionDetector>();

        stateManager = GetComponent<PlayerStateManager>();
        if (stateManager == null)
            stateManager = gameObject.AddComponent<PlayerStateManager>();

        playerAnimator = GetComponent<PlayerAnimator>();
        if (playerAnimator == null)
            playerAnimator = gameObject.AddComponent<PlayerAnimator>();

        playerHP = GetComponent<PlayerHP>();
        if (playerHP == null)
            playerHP = gameObject.AddComponent<PlayerHP>();
    }




    // 플레이어 변동 스탯 관리
    public void UpdateCurrentPlayerHP()
    {
        float maxHP = playerHP.MaxHP;
        CurrentMaxHP = maxHP;

        Debug.Log($"{CurrentMaxHP}");
    }

    //CurrentMoveSpeed += 플레이어 스탯에 변동을 줄 수 있는 모든 요소
    public void UpdateCurrentPlayerMoveSpeed(float changedSpeed)
    {
        float moveSpeed = settings.moveSpeed += changedSpeed;
        CurrentMoveSpeed = moveSpeed;
    }

    public void UpdateCurrentPlayerJumpForce(float changedJumpForce)
    {
        /*
        currentXVelocity = Mathf.Sign(currentXVelocity) * settings.moveSpeed * 1.5f;
        */

        CurrentJumpForce = changedJumpForce;
        
    }

    public void UpdateCurrentInventory()
    {
        // I키를 눌렀을 때 나타나는 모든 정보를 여기에 취합. 특성 포인트라든지, 획득한 복장이라든지...

        int nowUtilityPoint = utilityPoint;
        CurrentUtilityPoint = nowUtilityPoint;
    }
}