using UnityEngine;
using UnityEngine.Events;

public class ObjectRotatingPlatform : BaseObject
{
    [Header("회전 플랫폼 설정")]
    [SerializeField] private RotationType rotationType;  // 회전 축 타입
    [SerializeField] private bool isActive = false;                           // 활성화 상태
    [SerializeField] private float rotationSpeed;                       // 회전 속도 (도/초)
    [SerializeField] private bool reverseRotation = false;                   // 반대 방향 회전

    [Header("회전 제한 설정 (좌/우 축만 적용)")]
    [SerializeField] private float maxAngle;                           // 최대 회전 각도
    [SerializeField] private float minAngle;                            // 최소 회전 각도
    [SerializeField] private bool pingPongRotation = true;                   // 왕복 회전

    [Header("자동 작동")]
    [SerializeField] private bool autoActivate = false;                      // 자동 활성화
    [SerializeField] private float activationInterval;                  // 활성화 간격
    [SerializeField] private float activeTime;                          // 활성화 지속 시간

    [Header("시각 효과")]
    [SerializeField] private GameObject rotationEffect;                      // 회전 이펙트
    [SerializeField] private AudioClip rotationSound;                        // 회전 사운드

    [Header("이벤트")]
    public UnityEvent OnStartRotation;                                       // 회전 시작 이벤트
    public UnityEvent OnStopRotation;                                        // 회전 중지 이벤트
    public UnityEvent OnReachMaxAngle;                                       // 최대 각도 도달 이벤트
    public UnityEvent OnReachMinAngle;                                       // 최소 각도 도달 이벤트

    // 내부 변수
    private float currentAngle = 0f;                                         // 현재 각도
    private float rotationDirection = 1f;                                    // 회전 방향
    private Vector3 pivotPoint;                                              // 회전 축 포인트
    private float autoTimer = 0f;                                            // 자동 타이머
    private bool reachedMaxAngle = false;                                    // 최대 각도 도달 플래그
    private bool reachedMinAngle = true;                                     // 최소 각도 도달 플래그
    private AudioSource audioSource;                                         // 오디오 소스

    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && rotationSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.clip = rotationSound;
            audioSource.playOnAwake = false;
            audioSource.volume = 0.5f;
        }
    }

    protected override void Start()
    {
        base.Start();
        
        // 회전 축 포인트 설정
        SetupPivotPoint();
        
        // 초기 각도 설정
        if (rotationType != RotationType.Center)
        {
            currentAngle = minAngle;
        }
        
        // 방향 설정
        rotationDirection = reverseRotation ? -1f : 1f;
        
        // 자동 활성화 타이머 초기화
        if (autoActivate)
        {
            autoTimer = activationInterval;
        }
    }

    protected override void Update()
    {
        base.Update();
        
        // 자동 활성화 처리
        if (autoActivate)
        {
            HandleAutoActivation();
        }
        
        // 회전 처리
        if (isActive)
        {
            RotatePlatform();
        }
    }

    /// <summary>
    /// 회전 축 포인트 설정
    /// </summary>
    private void SetupPivotPoint()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return;
        
        Bounds bounds = renderer.bounds;
        
        switch (rotationType)
        {
            case RotationType.Center:
                pivotPoint = transform.position;
                break;
            case RotationType.Left:
                // 왼쪽 끝을 회전축으로 설정
                pivotPoint = new Vector3(
                    transform.position.x - bounds.size.x/2,
                    transform.position.y,
                    transform.position.z
                );
                break;
            case RotationType.Right:
                // 오른쪽 끝을 회전축으로 설정
                pivotPoint = new Vector3(
                    transform.position.x + bounds.size.x/2,
                    transform.position.y,
                    transform.position.z
                );
                break;
        }
        
        // 초기 각도 설정
        currentAngle = minAngle;
        // 초기 회전 적용
        transform.rotation = Quaternion.Euler(0, 0, currentAngle);
    }

    /// <summary>
    /// 자동 활성화 처리
    /// </summary>
    private void HandleAutoActivation()
    {
        autoTimer -= Time.deltaTime;
        
        if (autoTimer <= 0)
        {
            if (isActive)
            {
                // 활성화 상태면 비활성화로 전환
                isActive = false;
                StopRotationSound();
                OnStopRotation.Invoke();
                autoTimer = activationInterval;
            }
            else
            {
                // 비활성화 상태면 활성화로 전환
                isActive = true;
                PlayRotationSound();
                OnStartRotation.Invoke();
                autoTimer = activeTime;
            }
        }
    }

    /// <summary>
    /// 플랫폼 회전 로직
    /// </summary>
    private void RotatePlatform()
    {
        float rotationAmount = rotationSpeed * Time.deltaTime * rotationDirection;
        
        if (rotationType != RotationType.Center)
        {
            // 현재 각도 업데이트
            currentAngle += rotationAmount;
            
            // pingPong 및 각도 제한 처리
            if (pingPongRotation)
            {
                if (currentAngle >= maxAngle && !reachedMaxAngle)
                {
                    currentAngle = maxAngle;
                    reachedMaxAngle = true;
                    reachedMinAngle = false;
                    rotationDirection *= -1;
                    OnReachMaxAngle.Invoke();
                }
                else if (currentAngle <= minAngle && !reachedMinAngle)
                {
                    currentAngle = minAngle;
                    reachedMinAngle = true;
                    reachedMaxAngle = false;
                    rotationDirection *= -1;
                    OnReachMinAngle.Invoke();
                }
            }
            else
            {
                // 왕복이 아닌 경우 - 각도 제한만 적용
                currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);
                
                // 최대/최소 각도 도달 이벤트 발생
                if (currentAngle >= maxAngle && !reachedMaxAngle)
                {
                    reachedMaxAngle = true;
                    reachedMinAngle = false;
                    OnReachMaxAngle.Invoke();
                }
                else if (currentAngle <= minAngle && !reachedMinAngle)
                {
                    reachedMinAngle = true;
                    reachedMaxAngle = false;
                    OnReachMinAngle.Invoke();
                }
            }
            
            // 회전 적용
            if (rotationType == RotationType.Left || rotationType == RotationType.Right)
            {
                // transform.RotateAround를 사용하여 축을 중심으로 회전
                if (rotationType == RotationType.Left)
                {
                    // 왼쪽 축을 중심으로 회전
                    transform.RotateAround(pivotPoint, Vector3.forward, rotationAmount);
                }
                else // Right
                {
                    // 오른쪽 축을 중심으로 회전 (방향 반전)
                    transform.RotateAround(pivotPoint, Vector3.forward, -rotationAmount);
                }
            }
        }
        else
        {
            // Center 타입 회전
            transform.RotateAround(pivotPoint, Vector3.forward, rotationAmount);
        }
    }
    
    /// <summary>
    /// 회전 사운드 재생
    /// </summary>
    private void PlayRotationSound()
    {
        if (audioSource != null && rotationSound != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }
    
    /// <summary>
    /// 회전 사운드 중지
    /// </summary>
    private void StopRotationSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// 회전 활성화
    /// </summary>
    public void ActivateRotation()
    {
        if (!isActive)
        {
            isActive = true;
            PlayRotationSound();
            
            // 이펙트 생성
            if (rotationEffect != null)
            {
                Instantiate(rotationEffect, transform.position, Quaternion.identity);
            }
            
            OnStartRotation.Invoke();
        }
    }

    /// <summary>
    /// 회전 비활성화
    /// </summary>
    public void DeactivateRotation()
    {
        if (isActive)
        {
            isActive = false;
            StopRotationSound();
            OnStopRotation.Invoke();
        }
    }

    /// <summary>
    /// 회전 방향 전환
    /// </summary>
    public void ToggleRotationDirection()
    {
        rotationDirection *= -1;
        reverseRotation = !reverseRotation;
    }
    
    /// <summary>
    /// 초기 위치로 리셋
    /// </summary>
    public void ResetToInitialPosition()
    {
        currentAngle = minAngle;
        transform.rotation = Quaternion.Euler(0, 0, currentAngle);
    }

    /// <summary>
    /// 플레이어 상호작용 처리
    /// </summary>
    protected override void OnInteract(GameObject interactor)
    {
        // 상호작용시 회전 활성화/비활성화 토글
        if (!isActive)
            ActivateRotation();
        else
            DeactivateRotation();
    }

    /// <summary>
    /// 회전 종류 변경 (인스펙터에서 변경 시 피벗 포인트 업데이트)
    /// </summary>
    public void SetRotationType(RotationType type)
    {
        rotationType = type;
        SetupPivotPoint();
    }
}
