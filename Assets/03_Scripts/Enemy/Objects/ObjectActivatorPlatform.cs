using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class ObjectActivatorPlatform : BaseObject
{
    [Header("플랫폼 설정")]
    [SerializeField] private bool isActivated = false;          // 활성화 상태
    [SerializeField] private bool toggleOnActivate = true;      // 활성화시 전환(토글) 여부
    [SerializeField] private float activationDelay;        // 활성화 지연 시간

    [Header("임시 플랫폼 설정")]
    [SerializeField] private bool isTemporaryPlatform = false;                // 임시 플랫폼 여부
    [SerializeField] private float deactivateAfterPlayerLeftDelay;    // 플레이어가 떠난 후 비활성화 대기 시간
    [SerializeField] private float reactivationDelay;                   // 재활성화까지 대기 시간
    [SerializeField] private bool autoReactivate = true;                     // 자동 재활성화 여부

    [Header("자동 비활성화 설정")]
    [SerializeField] private bool autoDeactivate = false;       // 자동 비활성화 여부
    [SerializeField] private float deactivationDelay;    // 비활성화까지 유지 시간
    [SerializeField] private bool showDeactivationWarning = true; // 비활성화 경고 표시 여부
    [SerializeField] private float warningTime;          // 경고 시간

    [Header("물리 설정")]
    [SerializeField] private bool moveOnActivate = false;       // 활성화시 이동 여부
    [SerializeField] private float moveSpeed;              // 이동 속도
    [SerializeField] private Vector2 targetPosition;            // 목표 위치
    [SerializeField] private bool enableColliderOnActivate = true; // 활성화시 콜라이더 활성화

    [Header("시각 효과")]
    [SerializeField] private Sprite normalSprite;               // 기본 스프라이트
    [SerializeField] private Sprite activatedSprite;            // 활성화된 스프라이트
    [SerializeField] private Sprite warningSprite;              // 경고 스프라이트
    [SerializeField] private GameObject activationEffect;       // 활성화 이펙트
    [SerializeField] private GameObject warningEffect;          // 경고 이펙트
    [SerializeField] private Animator platformAnimator;         // 애니메이션 컨트롤러
    [SerializeField] private string activateAnimTrigger = "Activate"; // 활성화 애니메이션 트리거
    [SerializeField] private string deactivateAnimTrigger = "Deactivate"; // 비활성화 애니메이션 트리거
    [SerializeField] private string warningAnimTrigger = "Warning";   // 경고 애니메이션 트리거

    [Header("이벤트")]
    public UnityEvent OnActivated;                              // 활성화 이벤트
    public UnityEvent OnDeactivated;                            // 비활성화 이벤트
    public UnityEvent OnReachedTarget;                          // 목표 위치 도달 이벤트
    public UnityEvent OnWarning;                                // 비활성화 경고 이벤트


    private bool playerIsOnPlatform = false;

    private Vector2 startPosition;                              // 초기 위치
    private float activationTimer;                              // 활성화 타이머
    private float deactivationTimer;                            // 비활성화 타이머
    private bool isMoving = false;                              // 이동 중인지 여부
    private bool isWarning = false;                             // 경고 상태인지 여부
    private Collider2D platformCollider;                        // 플랫폼 콜라이더

    protected override void Awake()
    {
        base.Awake();
        platformCollider = GetComponent<Collider2D>();
        if (platformAnimator == null)
            platformAnimator = GetComponent<Animator>();
    }

    protected override void Start()
    {
        base.Start();
        startPosition = transform.position;

        // 임시 플랫폼의 경우 초기 상태 설정
        if (isTemporaryPlatform)
        {
            // 처음에는 활성화 상태로 시작
            isActivated = true;
            CompleteActivation(); // 완전한 활성화 처리를 위해 호출
        }
        else
        {
            // 일반 플랫폼의 경우
            if (platformCollider != null && !isActivated && enableColliderOnActivate)
            {
                platformCollider.enabled = false; // 비활성화 상태면 콜라이더도 비활성화
            }
        }

        UpdateVisuals();
    }

    protected override void Update()
    {
        base.Update();

        // 활성화 지연 타이머 처리
        if (activationTimer > 0)
        {
            activationTimer -= Time.deltaTime;
            if (activationTimer <= 0)
            {
                CompleteActivation();
            }
        }

        // 자동 비활성화 타이머 처리
        if (isActivated && autoDeactivate && deactivationTimer > 0)
        {
            deactivationTimer -= Time.deltaTime;

            // 경고 상태 체크
            if (showDeactivationWarning && !isWarning && deactivationTimer <= warningTime)
            {
                ShowWarning();
            }

            // 비활성화 시간 체크
            if (deactivationTimer <= 0)
            {
                DeactivatePlatform();
            }
        }

        // 이동 처리
        if (isMoving && moveOnActivate)
        {
            MovePlatform();
        }
    }

    private void MovePlatform()
    {
        Vector2 targetPos = isActivated ? targetPosition : startPosition;
        transform.position = Vector2.MoveTowards(
            transform.position,
            targetPos,
            moveSpeed * Time.deltaTime
        );

        // 목표 지점에 도달했는지 확인
        if (Vector2.Distance(transform.position, targetPos) < 0.01f)
        {
            isMoving = false;
            OnReachedTarget.Invoke();
        }
    }

    /// <summary>
    /// 비활성화 경고 표시
    /// </summary>
    private void ShowWarning()
    {
        isWarning = true;

        // 경고 스프라이트 변경
        if (spriteRenderer != null && warningSprite != null)
        {
            spriteRenderer.sprite = warningSprite;
        }

        // 경고 애니메이션 재생
        if (platformAnimator != null)
        {
            platformAnimator.SetTrigger(warningAnimTrigger);
        }

        // 경고 이펙트 생성
        if (warningEffect != null)
        {
            Instantiate(warningEffect, transform.position, Quaternion.identity);
        }

        // 경고 사운드 재생
        // AudioManager.Instance.PlaySFX("PlatformWarning");

        // 경고 이벤트 발생
        OnWarning.Invoke();
    }

    /// <summary>
    /// 외부에서 플랫폼을 활성화시킬 때 호출
    /// </summary>
    public void ActivatePlatform()
    {
        if (toggleOnActivate)
        {
            // 토글 모드: 활성화 상태 전환
            if (isActivated)
                DeactivatePlatform();
            else
                StartActivation();
        }
        else if (!isActivated)
        {
            // 기본 모드: 비활성화 상태일 때만 활성화
            StartActivation();
        }
    }

    /// <summary>
    /// 활성화 시작 (지연이 있을 수 있음)
    /// </summary>
    private void StartActivation()
    {
        if (activationDelay > 0)
        {
            activationTimer = activationDelay;
        }
        else
        {
            CompleteActivation();
        }
    }

    /// <summary>
    /// 활성화 완료 처리
    /// </summary>
    private void CompleteActivation()
    {
        isActivated = true;
        isWarning = false;
        UpdateVisuals();
        PlayInteractSound();

        // 자동 비활성화 타이머 설정
        if (autoDeactivate)
        {
            deactivationTimer = deactivationDelay;
        }

        // 콜라이더 활성화 (발판으로 사용하기 위해)
        if (platformCollider != null && enableColliderOnActivate)
            platformCollider.enabled = true;

        // 이동 처리
        if (moveOnActivate)
            isMoving = true;

        // 이펙트 재생
        if (activationEffect != null)
        {
            Instantiate(activationEffect, transform.position, Quaternion.identity);
        }

        // 이벤트 발생
        OnActivated.Invoke();
    }

    /// <summary>
    /// 플랫폼 비활성화
    /// </summary>
    public void DeactivatePlatform()
    {
        if (!isActivated)
            return;

        isActivated = false;
        isWarning = false;
        UpdateVisuals();

        // 콜라이더 비활성화
        if (platformCollider != null && enableColliderOnActivate)
            platformCollider.enabled = false;

        // 이동 모드 활성화
        if (moveOnActivate)
            isMoving = true;

        if (platformAnimator != null)
        {
            platformAnimator.SetTrigger(deactivateAnimTrigger);
        }

        // 이벤트 발생
        OnDeactivated.Invoke();
    }

    /// <summary>
    /// 시각적 효과 업데이트
    /// </summary>
    private void UpdateVisuals()
    {
        // 스프라이트 변경
        if (spriteRenderer != null)
        {
            if (isActivated && isWarning && warningSprite != null)
                spriteRenderer.sprite = warningSprite;
            else
                spriteRenderer.sprite = isActivated ? activatedSprite : normalSprite;
        }

        // 애니메이션 재생
        if (platformAnimator != null)
        {
            platformAnimator.SetBool("IsActivated", isActivated);
            platformAnimator.SetBool("IsWarning", isWarning);

            if (isActivated && !isWarning)
                platformAnimator.SetTrigger(activateAnimTrigger);
        }
    }

    /// <summary>
    /// 플레이어 상호작용 처리 - 이 플랫폼은 직접 상호작용하지 않음
    /// </summary>
    protected override void OnInteract(GameObject interactor)
    {
        // 기본적으로 직접 상호작용은 없음 (다른 오브젝트에 의해 활성화됨)
    }

    /// <summary>
    /// 현재 활성화 상태 반환
    /// </summary>
    public bool IsActivated()
    {
        return isActivated;
    }

    /// <summary>
    /// 비활성화 타이머 재설정 (플레이어가 계속 있을 경우 등)
    /// </summary>
    public void ResetDeactivationTimer()
    {
        if (isActivated && autoDeactivate)
        {
            deactivationTimer = deactivationDelay;
            isWarning = false;
            UpdateVisuals();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isTemporaryPlatform && collision.gameObject.CompareTag("Player"))
        {
            playerIsOnPlatform = true;
            if (!isActivated)
            {
                ActivatePlatform();
                Debug.Log("플레이어가 플랫폼에 올라감");
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (isTemporaryPlatform && collision.gameObject.CompareTag("Player"))
        {
            playerIsOnPlatform = false;
            StopAllCoroutines(); // 기존 코루틴 중지
            StartCoroutine(DeactivateAfterDelay());
            Debug.Log("플레이어가 플랫폼에서 떠남");
        }
    }

    private IEnumerator DeactivateAfterDelay()
    {
        // 플레이어가 떠난 후 지정된 시간만큼 대기
        yield return new WaitForSeconds(deactivateAfterPlayerLeftDelay);
        
        // 플레이어가 여전히 없다면
        if (!playerIsOnPlatform)
        {
            DeactivatePlatform();
            
            // 자동 재활성화가 설정되어 있다면
            if (autoReactivate)
            {
                // 재활성화 대기
                yield return new WaitForSeconds(reactivationDelay);
                
                // 플레이어가 없을 때만 재활성화
                if (!playerIsOnPlatform)
                {
                    StartActivation();
                    Debug.Log("플랫폼 재활성화");
                }
            }
        }
    }
}