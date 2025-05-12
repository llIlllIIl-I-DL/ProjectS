using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerSettings settings;

    [SerializeField] private bool hasWingsuit = false; // 윙슈트 장착 여부
    [SerializeField] private float sprintDuration = 1.5f; // 스프린트 지속 시간

    [SerializeField] public List<int> UnLockedUtility; //player가 현재까지 해금한 특성 리스트(해금 할 때마다 계속해서 쌓이기만 함)
    //초기화 기능 필요!

    // 필수 컴포넌트들
    private PlayerInputHandler inputHandler;
    private PlayerMovement movement;
    private CollisionDetector collisionDetector;
    private PlayerStateManager stateManager;
    private PlayerAnimator playerAnimator;
    private PlayerHP playerHP;

    public int utilityPoint;


    [HideInInspector] public float CurrentMoveSpeed { get; private set; } //현재 이동 속도

    [HideInInspector] public float CurrentRunSpeed { get; private set; } //현재 이동 속도

    [HideInInspector] public float CurrentMaxHP { get; private set; } //최대 HP

    [HideInInspector] public float CurrentSprintTime { get; private set; } //스프린트 지속 시간

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
            Invoke("DisableSprint", CurrentSprintTime);
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
    public void UpdateCurrentPlayerHP(float playercurrentHP)
    {
        CurrentMaxHP = playercurrentHP;

        Debug.Log($"{CurrentMaxHP}");
        PlayerUI.Instance.UpdatePlayerHPInUItext();
    }

    //CurrentMoveSpeed += 플레이어 스탯에 변동을 줄 수 있는 모든 요소
    public void UpdateCurrentPlayerMoveSpeed(float changedSpeed)
    {
        float moveSpeed = settings.moveSpeed += changedSpeed;
        CurrentMoveSpeed = moveSpeed;
    }


    public void UpdateCurrentPlayerRunSpeed(float changedSpeed)
    {
        float moveSpeed = settings.sprintMultiplier += changedSpeed;
        CurrentRunSpeed = moveSpeed;
    }


    public void UpdateCurrentSprintTime(float changedSprintTime)
    {
        float sprintTime = sprintDuration += changedSprintTime;
        CurrentSprintTime = sprintTime;
    }


    public void UpdateCurrentInventory()
    {
        // I키를 눌렀을 때 나타나는 모든 정보를 여기에 취합. 특성 포인트라든지, 획득한 복장이라든지...

        int nowUtilityPoint = utilityPoint;
        CurrentUtilityPoint = nowUtilityPoint;
    }



    public void UpdateCurrentUnLockedUtility(ItemData utilityItemData)
    {
        UnLockedUtility.Add(utilityItemData.id);
        Debug.Log("해금된 특성 리스트에 추가되었습니다.");
    }
}