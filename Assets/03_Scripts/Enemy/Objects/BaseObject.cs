using System;
using UnityEngine;

/// <summary>
/// 모든 인터랙티브 게임 오브젝트의 기본 클래스
/// </summary>
public abstract class BaseObject : MonoBehaviour
{
    #region Variables

    [Header("기본 설정")]
    [SerializeField] protected bool isInteractable = true;
    [SerializeField] protected float interactionRange;
    [SerializeField] protected string objectId = "Object";

    [Header("사운드")]
    [SerializeField] protected AudioClip interactSound;
    [SerializeField] protected float interactSoundVolume;
    
    // 참조 및 상태
    protected SpriteRenderer spriteRenderer;
    protected bool isPlayerInRange = false;

    #endregion

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        // 컴포넌트 참조 초기화
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected virtual void Start()
    {
        // 추가 초기화 작업
        Initialize();
        // 오브젝트 매니저에 등록
        //ObjectManager.Instance.RegisterObject(objectId, this);
    }

    protected virtual void Update()
    {
        // 플레이어 감지 업데이트
        DetectPlayer();
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            OnPlayerEnterRange(other.gameObject);
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            OnPlayerExitRange(other.gameObject);
        }
    }

    #endregion

    #region Core Methods

    /// <summary>
    /// 오브젝트 초기화 메서드
    /// </summary>
    protected virtual void Initialize()
    {
        // 하위 클래스에서 구현
    }

    /// <summary>
    /// 플레이어를 감지하는 메서드
    /// </summary>
    protected virtual void DetectPlayer()
    {
        // 트리거 콜라이더가 없는 경우에도 작동하도록 레이캐스트 사용
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            bool wasInRange = isPlayerInRange;
            isPlayerInRange = distance <= interactionRange;
            
            // 상태 변경 시에만 이벤트 발생
            if (isPlayerInRange != wasInRange)
            {
                if (isPlayerInRange)
                    OnPlayerEnterRange(player);
                else
                    OnPlayerExitRange(player);
            }
        }
    }

    /// <summary>
    /// 상호작용 사운드 재생
    /// </summary>
    protected virtual void PlayInteractSound()
    {
        if (interactSound != null)
        {
            AudioSource.PlayClipAtPoint(interactSound, transform.position, interactSoundVolume);
        }
    }

    #endregion

    #region Event Methods

    /// <summary>
    /// 플레이어가 범위에 들어왔을 때 호출
    /// </summary>
    protected virtual void OnPlayerEnterRange(GameObject player)
    {
        // UI 표시 등의 작업
        ShowInteractionPrompt();
    }

    /// <summary>
    /// 플레이어가 범위를 벗어났을 때 호출
    /// </summary>
    protected virtual void OnPlayerExitRange(GameObject player)
    {
        // UI 숨김 등의 작업
        HideInteractionPrompt();
    }

    /// <summary>
    /// 상호작용 UI 표시
    /// </summary>
    protected virtual void ShowInteractionPrompt()
    {
        // UI 매니저를 통해 상호작용 프롬프트 표시
    }

    /// <summary>
    /// 상호작용 UI 숨김
    /// </summary>
    protected virtual void HideInteractionPrompt()
    {
        // UI 매니저를 통해 상호작용 프롬프트 숨김
    }

    /// <summary>
    /// 상호작용 가능 여부 설정
    /// </summary>
    public virtual void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
    }

    #endregion

    #region Public Interface

    /// <summary>
    /// 외부에서 상호작용 시도할 때 호출
    /// </summary>
    public virtual bool TryInteract(GameObject interactor)
    {
        if (!isInteractable || !isPlayerInRange)
            return false;
            
        // 상호작용 로직 수행
        OnInteract(interactor);
        return true;
    }

    /// <summary>
    /// 실제 상호작용 처리
    /// </summary>
    protected virtual void OnInteract(GameObject interactor)
    {
        UIManager.Instance.playerInputHandler.IsInteracting = true;
    }

    
    /// <summary>
    /// 오브젝트의 현재 상태를 문자열로 변환
    /// </summary>
    public override string ToString()
    {
        return $"{objectId} (Interactable: {isInteractable})";
    }

    #endregion
}