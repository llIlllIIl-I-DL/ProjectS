using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime;

/// <summary>
/// 벨브 오브젝트 - 플레이어와 상호작용시 벨브를 열거나 닫음
/// </summary>
public class ObjectValve : BaseObject
{
    #region Variables
    [Header("벨브 설정")]
    [SerializeField] private bool isOpen = false; // 문이 열려있는지 여부

    [Header("애니메이션")]
    [SerializeField] private Animator valveAnimator; // 문 애니메이터
    [SerializeField] private string openAnimTrigger = "Open"; // 열기 애니메이션 트리거
    [SerializeField] private string closeAnimTrigger = "Close"; // 닫기 애니메이션 트리거

    [Header("자동 닫힘")]
    [SerializeField] private bool autoClose = false; // 자동으로 닫히는지 여부
    [SerializeField] private float autoCloseDelay; // 자동 닫힘 지연 시간

    [Header("이벤트")]
    public UnityEvent OnActivated;   // 활성화될 때 이벤트
    public UnityEvent OnDeactivated; // 비활성화될 때 이벤트

    // 자동 닫힘 타이머
    private float autoCloseTimer;

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();
        if (valveAnimator == null)
            valveAnimator = GetComponent<Animator>();
    }

    protected override void Start()
    {
        base.Start();
        UpdateValveState(isOpen, false);
    }

    protected override void Update()
    {
        base.Update();

        // 자동 닫힘 처리
        if (autoClose && isOpen)
        {
            autoCloseTimer -= Time.deltaTime;
            if (autoCloseTimer <= 0)
            {
                CloseValve();
            }
        }
    }

    #endregion

    #region Valve Operations

    /// <summary>
    /// 문 상태 업데이트
    /// </summary>
    private void UpdateValveState(bool open, bool playEffects = true)
    {
        isOpen = open;

        // 애니메이션 재생
        if (valveAnimator != null)
        {
            valveAnimator.SetTrigger(isOpen ? openAnimTrigger : closeAnimTrigger);
        }

        // 사운드 효과 재생
        if (playEffects)
        {
            if (isOpen)
            {
                // AudioManager.Instance.PlaySFX("DoorOpen");
            }
            else
            {
                // AudioManager.Instance.PlaySFX("DoorClose");
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
    public void OpenValve()
    {
        if (!isOpen)
        {
            UpdateValveState(true);
            OnActivated.Invoke();
        }
    }

    /// <summary>
    /// 문 닫기
    /// </summary>
    public void CloseValve()
    {
        if (isOpen)
        {
            UpdateValveState(false);
            OnDeactivated.Invoke();
        }
    }

    #endregion

    #region Interaction

    /// <summary>
    /// 플레이어 상호작용 처리
    /// </summary>
    protected override void OnInteract(GameObject interactor)
    {
        if (objectId == "BossValve" && BossWarningUI.Instance != null)
        {
            // 경고창 띄우기 (실제 열기는 UI 쪽에서)
            BossWarningUI.Instance.BossWarningWindowUI(interactor, this);
            return;
        }



        if (!isOpen)
        {
            OpenValve();
        }



        {
            CloseValve();
        }
    }
    #endregion
}