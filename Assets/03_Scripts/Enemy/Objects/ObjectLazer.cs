using UnityEngine;
using System.Collections;

/// <summary>
/// 레이저 오브젝트 - 플레이어와 상호작용 시 레이저 발사
/// </summary>
public class ObjectLazer : BaseObject
{
    #region Variables

    [Header("레이저 설정")]
    [SerializeField] private float laserRange = 20f;      // 레이저 최대 사거리
    [SerializeField] private float laserDuration = 3f;    // 레이저 지속 시간
    [SerializeField] private float cooldownTime = 1f;     // 재사용 대기시간
    [SerializeField] private LayerMask hitLayers;         // 레이저가 충돌할 레이어
    [SerializeField] private int maxReflections = 3;      // 최대 반사 횟수

    [Header("시각 효과")]
    [SerializeField] private LineRenderer laserLine;      // 레이저 라인 렌더러
    [SerializeField] private float laserWidth = 0.1f;     // 레이저 두께
    [SerializeField] private GameObject impactEffect;     // 충돌 효과
    [SerializeField] private Transform firePoint;         // 레이저 발사 위치

    [Header("소리 효과")]
    [SerializeField] private AudioClip chargeSound;       // 충전 소리
    [SerializeField] private AudioClip fireSound;         // 발사 소리

    private bool isOnCooldown = false;                    // 쿨다운 상태
    private bool isLaserActive = false;                   // 레이저 활성화 상태
    private AudioSource audioSource;                      // 오디오 소스 컴포넌트
    private Coroutine laserCoroutine;                     // 레이저 코루틴 참조

    [Header("F Interaction")]
    [SerializeField] private GameObject interactionBtnUI;
    [SerializeField] private Transform interactionBtnUITransform;
    private GameObject interactionButtonUI;

    #endregion

    #region Unity Lifecycle

    protected override void Start()
    {
        base.Start();
        InitializeComponents();
    }

    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // LineRenderer 초기화
        if (laserLine == null)
            laserLine = gameObject.AddComponent<LineRenderer>();
        
        laserLine.startWidth = laserWidth;
        laserLine.endWidth = laserWidth;
        laserLine.positionCount = (maxReflections + 1) * 2 + 1;  // 최적화된 포인트 수
        laserLine.enabled = false;
        
        // 발사 지점 설정
        if (firePoint == null)
            firePoint = transform;
            
        // 오디오 소스 설정
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    #endregion

    #region Interaction

    /// <summary>
    /// 플레이어가 상호작용할 때 호출됨
    /// </summary>
    protected override void OnInteract(GameObject interactor)
    {
        // 레이저가 이미 활성화된 상태면 쿨다운 상태와 상관없이 끌 수 있음
        if (isLaserActive)
        {
            OffLazer();
            Debug.Log("레이저 강제 종료됨");
            return;
        }
        
        // 레이저가 꺼진 상태에서는 쿨다운 체크
        if (isOnCooldown)
        {
            Debug.Log("레이저 쿨다운 중...");
            return;
        }

        // 레이저 켜기
        UIManager.Instance.playerInputHandler.IsInteracting = false;
        OnLazer();
    }

    #endregion

    #region Laser Control

    /// <summary>
    /// 레이저 발사 (외부에서 호출 가능)
    /// </summary>
    public void OnLazer()
    {
        if (isOnCooldown)
            return;
        
        // 이미 실행 중인 레이저가 있다면 중지
        if (laserCoroutine != null)
            StopCoroutine(laserCoroutine);
        
        // 상태 업데이트 및 코루틴 시작
        isLaserActive = true;
        laserCoroutine = StartCoroutine(FireLaser());
    }
    
    /// <summary>
    /// 레이저 중지 (외부에서 호출 가능)
    /// </summary>
    public void OffLazer()
    {
        // 코루틴이 실행 중이면 중지
        if (laserCoroutine != null)
        {
            StopCoroutine(laserCoroutine);
            laserCoroutine = null;
        }
        
        // 상태 초기화
        laserLine.enabled = false;
        isOnCooldown = false;
        isLaserActive = false;
        // SetInteractable(true);
        
        // 오디오 정지
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    /// <summary>
    /// 레이저 발사 코루틴
    /// </summary>
    private IEnumerator FireLaser()
    {
        isOnCooldown = true;
        // SetInteractable(false);
        
        // 충전 소리 재생
        PlaySound(chargeSound);
        
        // 충전 시간
        yield return new WaitForSeconds(0.5f);
        
        // 발사 소리 재생
        PlaySound(fireSound);
        
        // 레이저 활성화 및 처리
        laserLine.enabled = true;
        
        float elapsedTime = 0f;
        while (elapsedTime < laserDuration && isLaserActive)
        {
            ProcessLaserWithReflections();
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 레이저 비활성화
        laserLine.enabled = false;
        
        // 상태가 이미 비활성화되었으면 쿨다운 스킵
        if (!isLaserActive)
        {
            isOnCooldown = false;
            isLaserActive = false;
            // SetInteractable(true);
            laserCoroutine = null;
            yield break; // return 대신 yield break 사용
        }
        
        // 쿨다운
        yield return new WaitForSeconds(cooldownTime);
        
        // 상태 초기화
        isOnCooldown = false;
        isLaserActive = false;
        // SetInteractable(true);
        laserCoroutine = null;
    }
    
    /// <summary>
    /// 소리 재생 도우미 함수
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    #endregion

    #region Laser Physics

    /// <summary>
    /// 반사를 고려한 레이저 처리 메서드
    /// </summary>
    private void ProcessLaserWithReflections()
    {
        Vector2 startPos = firePoint.position;
        Vector2 direction = firePoint.right;
        
        // 최적화된 포인트 수 계산
        int maxPoints = 1 + (maxReflections + 1) * 2;
        laserLine.positionCount = maxPoints;
        
        // 발사 시작점 설정
        laserLine.SetPosition(0, startPos);
        
        // 레이저 반사 처리
        int pointCount = 1;
        CastLaserRecursive(startPos, direction, 0, ref pointCount);
        
        // 남은 포인트 정리
        for (int i = pointCount; i < laserLine.positionCount; i++)
            laserLine.SetPosition(i, laserLine.GetPosition(pointCount - 1));
    }

    /// <summary>
    /// 재귀적 레이저 처리 - 반사 구현
    /// </summary>
    private void CastLaserRecursive(Vector2 startPos, Vector2 direction, int reflectionCount, ref int pointIndex, GameObject lastHitObject = null)
    {
        // 반사 횟수 제한 확인
        if (reflectionCount > maxReflections)
            return;
        
        // 레이캐스트 실행
        RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, direction, laserRange, hitLayers);
        
        // 유효한 첫 번째 히트 찾기
        RaycastHit2D hit = default;
        bool foundHit = false;
        
        foreach (var hitInfo in hits)
        {
            if (lastHitObject == null || hitInfo.collider.gameObject != lastHitObject)
            {
                hit = hitInfo;
                foundHit = true;
                break;
            }
        }
        
        if (foundHit)
        {
            ProcessHitObject(hit, startPos, direction, reflectionCount, ref pointIndex);
        }
        else
        {
            // 충돌 없음 - 최대 거리까지 레이저 그리기
            Vector2 endPos = startPos + direction * laserRange;
            laserLine.SetPosition(pointIndex++, endPos);
        }
    }
    
    /// <summary>
    /// 히트한 오브젝트 처리
    /// </summary>
    private void ProcessHitObject(RaycastHit2D hit, Vector2 startPos, Vector2 direction, int reflectionCount, ref int pointIndex)
    {
        // 반사경 체크
        ObjectMirror mirror = hit.collider.GetComponent<ObjectMirror>();
        
        if (mirror != null && reflectionCount < maxReflections)
        {
            ProcessMirrorHit(hit, mirror, direction, reflectionCount, ref pointIndex);
        }
        else
        {
            ProcessNormalHit(hit, direction, ref pointIndex);
        }
    }
    
    /// <summary>
    /// 반사경에 히트 처리
    /// </summary>
    private void ProcessMirrorHit(RaycastHit2D hit, ObjectMirror mirror, Vector2 direction, int reflectionCount, ref int pointIndex)
    {
        // 히트 지점에서 라인 설정
        laserLine.SetPosition(pointIndex++, hit.point);
        
        // 충돌 효과 생성
        CreateImpactEffect(hit.point);
        
        // 중앙 지점 및 반사 계산
        Vector3 mirrorCenter = mirror.transform.position;
        Vector2 reflectedDir = mirror.ReflectLaser(direction);
        
        // 중앙으로 라인 이어서 그리기
        laserLine.SetPosition(pointIndex++, mirrorCenter);
        
        // 다음 레이저 시작점 (약간 오프셋)
        Vector2 nextStartPos = (Vector2)mirrorCenter + (reflectedDir * 0.25f);
        
        // 다음 반사 레이저 계산
        CastLaserRecursive(nextStartPos, reflectedDir, reflectionCount + 1, ref pointIndex, mirror.gameObject);
    }
    
    /// <summary>
    /// 일반 오브젝트에 히트 처리
    /// </summary>
    private void ProcessNormalHit(RaycastHit2D hit, Vector2 direction, ref int pointIndex)
    {
        // 히트 지점에서 라인 종료
        laserLine.SetPosition(pointIndex++, hit.point);
        
        // 충돌 효과 생성
        CreateImpactEffect(hit.point);
        
        // 인터페이스 호출
        TriggerInteractions(hit, direction);
    }
    
    /// <summary>
    /// 충돌 효과 생성
    /// </summary>
    private void CreateImpactEffect(Vector2 position)
    {
        if (impactEffect != null)
        {
            GameObject impact = Instantiate(impactEffect, position, Quaternion.identity);
            Destroy(impact, 0.2f);
        }
    }
    
    /// <summary>
    /// 상호작용 인터페이스 트리거
    /// </summary>
    private void TriggerInteractions(RaycastHit2D hit, Vector2 direction)
    {
        // 레이저 상호작용 인터페이스
        ILaserInteractable laserInteractable = hit.collider.GetComponent<ILaserInteractable>();
        if (laserInteractable != null)
        {
            laserInteractable.OnLaserHit(hit.point, direction);
        }

        // 데미지 인터페이스
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            // 데미지 처리 로직 추가
            // damageable.TakeDamage(laserDamage);
        }
    }
    protected override void OnPlayerEnterRange(GameObject player)
    {
        base.OnPlayerEnterRange(player);
    }

    protected override void OnPlayerExitRange(GameObject player)
    {
        base.OnPlayerExitRange(player);
    }

    protected override void ShowInteractionPrompt()
    {
        if(isLaserActive == false)
        interactionButtonUI = Instantiate(interactionBtnUI, interactionBtnUITransform);

    }
    protected override void HideInteractionPrompt()
    {
        interactionButtonUI.SetActive(false);
    }

    protected override void OnTriggerExit2D(Collider2D collider2D)
    {
        base.OnTriggerExit2D(collider2D);
    }

    #endregion

    #region Editor

#if UNITY_EDITOR
    // 편집기에서 레이저 방향 시각화
    private void OnDrawGizmos()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(firePoint.position, firePoint.right * laserRange);
        }
    }
    #endif

    #endregion
}