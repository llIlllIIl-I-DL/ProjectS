using System;
using UnityEngine;
using UnityEngine.Events;

public class ObjectPressurePlate : BaseObject
{
    #region Variables

    [Header("발판 설정")]
    [SerializeField] private bool isActivated = false; // 발판 활성화 여부
    [SerializeField] private bool staysPressed = true; // 한번 눌리면 계속 활성화 유지
    [SerializeField] private float resetDelay = 1.0f; // 리셋 딜레이 (staysPressed가 false일 때)
    [SerializeField] private string activatedTag = "Player"; // 활성화시키는 태그

    [Header("시각 효과")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite activatedSprite;
    [SerializeField] private GameObject activationEffect;

    [Header("이벤트")]
    public UnityEvent OnActivated;   // 활성화될 때 이벤트
    public UnityEvent OnDeactivated; // 비활성화될 때 이벤트

    // 타이머
    private float resetTimer = 0f;
    private bool needsReset = false;

    #endregion

    #region Unity Lifecycle

    protected override void Start()
    {
        base.Start();
        UpdateVisuals();
    }

    protected override void Update()
    {
        base.Update();

        // 리셋 타이머 처리
        if (needsReset && !staysPressed)
        {
            resetTimer -= Time.deltaTime;
            if (resetTimer <= 0f)
            {
                DeactivatePlate();
                needsReset = false;
            }
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);

        // 지정된 태그를 가진 오브젝트만 발판 활성화
        if (!isActivated && other.CompareTag(activatedTag))
        {
            ActivatePlate();
        }
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerExit2D(other);

        // staysPressed가 false면 객체가 떠날 때 리셋 시작
        if (isActivated && !staysPressed && other.CompareTag(activatedTag))
        {
            resetTimer = resetDelay;
            needsReset = true;
        }
    }

    #endregion

    #region Pressure Plate Methods

    /// <summary>
    /// 발판 활성화
    /// </summary>
    public void ActivatePlate()
    {
        if (isActivated)
            return;

        isActivated = true;
        UpdateVisuals();
        PlayInteractSound();

        // 이펙트 재생
        if (activationEffect != null)
        {
            Instantiate(activationEffect, transform.position, Quaternion.identity);
        }

        // 이벤트 발생
        OnActivated.Invoke();
    }

    /// <summary>
    /// 발판 비활성화
    /// </summary>
    public void DeactivatePlate()
    {
        if (!isActivated)
            return;

        isActivated = false;
        UpdateVisuals();

        // 이벤트 발생
        OnDeactivated.Invoke();
    }

    /// <summary>
    /// 발판 시각적 업데이트
    /// </summary>
    private void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = isActivated ? activatedSprite : normalSprite;
        }
    }

    /// <summary>
    /// 현재 활성화 상태 반환
    /// </summary>
    public bool IsActivated()
    {
        return isActivated;
    }

    #endregion

    #region Interaction
    /// <summary>
    /// 플레이어 상호작용 처리
    /// </summary>
    protected override void OnInteract(GameObject interactor)
    {
        // 발판은 상호작용하지 않음
    }

    # endregion
}
