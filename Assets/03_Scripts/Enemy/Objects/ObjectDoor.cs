using System;
using UnityEngine;

/// <summary>
/// 문 오브젝트 - 플레이어와 상호작용 시 열림, 닫힘 또는 다른 오브젝트와 상호작용
/// </summary>

public class ObjectDoor : BaseObject
{
    #region Variables
    [Header("문 설정")]
    [SerializeField] private bool isOpen = false; // 문이 열려있는지 여부
    [SerializeField] private bool isLocked = true; // 문이 잠겨있는지 여부
    
    [Header("애니메이션")]
    [SerializeField] private Animator doorAnimator; // 문 애니메이터
    [SerializeField] private string openAnimTrigger = "Open"; // 열기 애니메이션 트리거
    [SerializeField] private string closeAnimTrigger = "Close"; // 닫기 애니메이션 트리거
    
    [Header("자동 닫힘")]
    [SerializeField] private bool autoClose = false; // 자동으로 닫히는지 여부
    [SerializeField] private float autoCloseDelay; // 자동 닫힘 지연 시간
    
    // 필요한 컴포넌트 참조
    private Collider2D doorCollider;
    
    // 자동 닫힘 타이머
    private float autoCloseTimer;
    
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
            if (isOpen)
            {
                AudioManager.Instance.PlaySFX("DoorOpen");
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
        else if (isLocked)
        {
            
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
        
        // 잠금 해제 사운드 재생
        if (!isLocked)
        {

        }
    }
    
    /// <summary>
    /// 외부에서 잠금 해제
    /// </summary>
    public void Unlock()
    {
        ToggleLock(false);
    }

    /// <summary>
    /// 발판 두개 이상으로 문 열기
    /// </summary>
    
    
    #endregion
    
    #region Interaction
    
    /// <summary>
    /// 플레이어 상호작용 처리
    /// </summary>
    protected override void OnInteract(GameObject interactor)
    {
        // 문은 상호작용 없음
    }
    
    #endregion
}