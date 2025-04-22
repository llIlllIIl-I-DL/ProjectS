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

    

    [HideInInspector]
    public ItemData CurrentattributeTypeData;

    private void Awake()
    {
        // 필요한 컴포넌트 추가
        EnsureComponents();
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
    }
}