using System;
using UnityEngine;

/// <summary>
/// 문 오브젝트 - 플레이어와 상호작용 시 열림, 닫힘 또는 다른 오브젝트와 상호작용
/// </summary>
public class ObjectDoor : BaseObject
{
    #region Variables
    
    [Header("문 설정")]
    [SerializeField] private bool isOpen = false;  // 문이 열려있는지 여부
    [SerializeField] private bool isLocked = true; // 문이 잠겨있는지 여부
    
    [Header("애니메이션")]
    [SerializeField] private Animator doorAnimator;            // 문 애니메이터
    [SerializeField] private string openAnimTrigger = "Open";  // 열기 애니메이션 트리거
    [SerializeField] private string closeAnimTrigger = "Close"; // 닫기 애니메이션 트리거
    
    [Header("자동 닫힘")]
    [SerializeField] private bool autoClose = false;   // 자동으로 닫히는지 여부
    [SerializeField] private float autoCloseDelay;     // 자동 닫힘 지연 시간
    
    private Collider2D doorCollider;  // 문 콜라이더
    private float autoCloseTimer;     // 자동 닫힘 타이머
    
    #endregion
    
    #region Unity Lifecycle
    
    protected override void Awake()
    {
        base.Awake();
        doorCollider = GetComponent<Collider2D>();
        
        if (doorAnimator == null)
            doorAnimator = GetComponent<Animator>();
    }
    
    protected override void Start() 
    {
        base.Start();
        UpdateDoorState(isOpen, false);
    }
    
    protected override void Update()
    {
        base.Update();
        
        // 자동 닫힘 처리
        if (autoClose && isOpen && !isLocked)
        {
            autoCloseTimer -= Time.deltaTime;
            if (autoCloseTimer <= 0)
            {
                CloseDoor();
            }
        }
    }
    
    #endregion
    
    #region Door Operations
    
    /// <summary>
    /// 문 상태 업데이트
    /// </summary>
    private void UpdateDoorState(bool open, bool playEffects = true)
    {
        isOpen = open;
        
        // 애니메이션 재생
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger(isOpen ? openAnimTrigger : closeAnimTrigger);
        }
        
        // 콜라이더 활성화/비활성화
        if (doorCollider != null)
        {
            doorCollider.enabled = !isOpen;
        }

        // 사운드 효과 재생
        if (playEffects)
        {
            // 오디오 실행방법이 여러개
            if (isOpen)
            {
                AudioManager.Instance.PlaySFX("DoorOpen");
                // AudioSource.PlayClipAtPoint();
            }
            else
            {
                AudioManager.Instance.PlaySFX("DoorClose");
            }
        }
        
        // 자동 닫힘 타이머 설정
        if (isOpen && autoClose)
        {
            autoCloseTimer = autoCloseDelay;
        }
    }
    
    /// <summary>
    /// 문 열기
    /// </summary>
    public void OpenDoor()
    {
        if (!isLocked && !isOpen)
        {
            UpdateDoorState(true);
        }
    }
    
    /// <summary>
    /// 문 닫기
    /// </summary>
    public void CloseDoor()
    {
        if (isOpen)
        {
            UpdateDoorState(false);
        }
    }
    
    /// <summary>
    /// 문 잠금 상태 토글
    /// </summary>
    public void ToggleLock(bool locked)
    {
        if (isLocked == locked)
            return;
            
        isLocked = locked;
    }
    
    /// <summary>
    /// 외부에서 잠금 해제
    /// </summary>
    public void Unlock()
    {
        ToggleLock(false);
    }
    
    #endregion
    
    #region Interaction
    
    protected override void OnPlayerEnterRange(GameObject player)
    {
        base.OnPlayerEnterRange(player);
        ShowInteractionPrompt();
    }

    protected override void OnPlayerExitRange(GameObject player)
    {
        base.OnPlayerExitRange(player);
        HideInteractionPrompt();
    }

    /// <summary>
    /// 플레이어 상호작용 처리
    /// </summary>
    protected override void OnInteract(GameObject interactor)
    {
        // 오브젝트 ID에 따라 다른 상호작용 실행
        switch (objectId)
        {
            case "BossDoor":
                // 보스 경고 UI 표시
                if (BossWarningUI.Instance != null)
                {
                    BossWarningUI.Instance.BossWarningWindowUI(interactor, this);
                    return;
                }
                break;
                
            // case "LockedDoor":
            //     // 잠긴 문 메시지 표시
            //     if (isLocked)
            //     {
            //         AudioManager.Instance.PlaySFX("DoorLocked");
            //         return;
            //     }
            //     break;
                
            // case "SecretDoor":
            //     // 비밀 문 발견 로직
            //     // GameManager.Instance.UnlockAchievement("SecretFound");
            //     break;
                
            // case "TimedDoor":
            //     // 시간제한 문 열기
            //     if (isOpen)
            //     {
            //         CloseDoor();
            //     }
            //     else
            //     {
            //         OpenDoor();
            //         autoClose = true;
            //         autoCloseTimer = autoCloseDelay;
            //     }
            //     return;
        }
        
        // 기본 문 상호작용 (특수 케이스가 아닐 경우)
        if (isLocked)
        {
            AudioManager.Instance.PlaySFX("DoorLocked");
            return;
        }
        
        // 일반적인 문 열기/닫기
        if (isOpen)
        {
            CloseDoor();
        }
        else
        {
            OpenDoor();
        }
    }

    /// <summary>
    /// 보스 방 입장 결정에 따른 처리
    /// </summary>
    /// <param name="isApproved">입장 승인 여부</param>
    public void OnEntrance(bool isApproved)
    {
        if (isApproved)
        {
            OpenDoor();
            Debug.Log("당신은 들어가기로 결정했다.");
        }
        else
        {
            Debug.Log("당신은 들어가지 않기로 결정했다.");
        }
    }
    
    #endregion
}