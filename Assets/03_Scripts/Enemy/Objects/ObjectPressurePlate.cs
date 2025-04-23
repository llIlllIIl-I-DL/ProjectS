using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

/// <summary>
/// 발판 오브젝트 - 플레이어가 발판 위에 서면 활성화됨
/// 다중 태그 지원
/// </summary>
public class ObjectPressurePlate : BaseObject
{
    #region Variables

    [Header("발판 설정")]
    [SerializeField] private bool isActivated = false; // 발판 활성화 여부
    [SerializeField] private bool staysPressed = true; // 한번 눌리면 계속 활성화 유지
    [SerializeField] private float resetDelay = 1.0f; // 리셋 딜레이 (staysPressed가 false일 때)
    [SerializeField] private string activatedTag = "Player"; // 활성화시키는 태그

    [Header("다중 태그 활성화 설정")]
    [SerializeField] private bool requireMultipleTags = false;  // 여러 태그가 필요한지 여부
    [SerializeField] private List<string> requiredTags = new List<string>();  // 필요한 태그 목록
    [SerializeField] private bool allTagsOnSingleObject = false;  // 한 오브젝트가 모든 태그를 가져야 하는지

    // 현재 발판 위에 있는 오브젝트와 태그 추적
    private HashSet<string> presentTags = new HashSet<string>();
    private Dictionary<int, string> objectsOnPlate = new Dictionary<int, string>();

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

        if (isActivated)
            return;

        if (requireMultipleTags)
        {
            // 다중 태그 모드
            int objectId = other.gameObject.GetInstanceID();

            // 오브젝트의 태그 저장
            string tag = other.tag;
            objectsOnPlate[objectId] = tag;

            // 필요한 태그인 경우 저장
            if (requiredTags.Contains(tag))
            {
                presentTags.Add(tag);
                Debug.Log($"태그 '{tag}' 감지! ({presentTags.Count}/{requiredTags.Count})");
            }

            // 모든 필요 태그가 있는지 확인
            if (CheckAllTagsPresent())
            {
                ActivatePlate();
            }
        }
        else
        {
            // 기존 단일 태그 모드
            if (other.CompareTag(activatedTag))
            {
                ActivatePlate();
            }
        }
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerExit2D(other);

        int objectId = other.gameObject.GetInstanceID();

        if (requireMultipleTags)
        {
            // 발판에서 나간 오브젝트의 태그 제거
            if (objectsOnPlate.TryGetValue(objectId, out string tagToRemove))
            {
                objectsOnPlate.Remove(objectId);

                // 같은 태그를 가진 다른 오브젝트가 없는 경우에만 제거
                bool tagStillPresent = false;
                foreach (var tag in objectsOnPlate.Values)
                {
                    if (tag == tagToRemove)
                    {
                        tagStillPresent = true;
                        break;
                    }
                }

                if (!tagStillPresent && requiredTags.Contains(tagToRemove))
                {
                    presentTags.Remove(tagToRemove);
                    Debug.Log($"태그 '{tagToRemove}' 제거됨");
                }

                // 발판이 활성화되어 있고, staysPressed가 false라면
                if (isActivated && !staysPressed)
                {
                    if (!CheckAllTagsPresent())
                    {
                        // 모든 태그가 있지 않으면 비활성화 또는 타이머 시작
                        resetTimer = resetDelay;
                        needsReset = true;
                    }
                }
            }
        }
        else if (isActivated && !staysPressed && other.CompareTag(activatedTag))
        {
            // 기존 단일 태그 모드 로직
            resetTimer = resetDelay;
            needsReset = true;
        }
    }
    
    /// <summary>
    /// 모든 필요한 태그가 있는지 확인
    /// </summary>
    private bool CheckAllTagsPresent()
    {
        if (allTagsOnSingleObject)
        {
            // 한 오브젝트가 모든 태그를 가져야 하는 경우
            foreach (var obj in objectsOnPlate.Values)
            {
                GameObject gameObj = GameObject.Find(obj); // 이름으로 찾기 (더 나은 방법이 있을 수 있음)
                bool hasAllTags = true;

                foreach (string requiredTag in requiredTags)
                {
                    if (!gameObj.CompareTag(requiredTag))
                    {
                        hasAllTags = false;
                        break;
                    }
                }

                if (hasAllTags)
                    return true;
            }
            return false;
        }
        else
        {
            // 각 태그가 서로 다른 오브젝트에 있을 수 있는 경우
            return presentTags.Count >= requiredTags.Count && requiredTags.All(tag => presentTags.Contains(tag));
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

        // 매니저에 상태 등록
        ObjectManager.Instance.RegisterTriggerState(objectId, true);

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

        // 매니저에 상태 등록
        ObjectManager.Instance.RegisterTriggerState(objectId, false);

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

    #endregion
}
