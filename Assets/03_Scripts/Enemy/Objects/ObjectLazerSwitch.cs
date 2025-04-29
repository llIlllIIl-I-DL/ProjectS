using UnityEngine;
using UnityEngine.Events;

/// <summary> 
/// 레이저에 반응하는 스위치
/// </summary>
public class LaserActivatedSwitch : MonoBehaviour, ILaserInteractable
{
    [Header("활성화 설정")]
    [SerializeField] private float activationThreshold; // 활성화 임계값 (초)
    [SerializeField] private float deactivationDelay;   // 비활성화 지연 시간 (초)
    [SerializeField] private bool stayActive = false;          // 한번 활성화되면 계속 유지

    [Header("시각 효과")]
    [SerializeField] private Color inactiveColor = Color.white;  // 비활성화 색상
    [SerializeField] private Color activeColor = Color.green;    // 활성화 색상
    [SerializeField] private GameObject activationEffect;        // 활성화 이펙트
    [SerializeField] private AudioClip activationSound;          // 활성화 소리

    [Header("이벤트")]
    public UnityEvent OnSwitchActivated;    // 활성화될 때 이벤트
    public UnityEvent OnSwitchDeactivated;  // 비활성화될 때 이벤트
    
    private float activationTimer = 0f;
    private bool isActivated = false;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }
    
    private void Start()
    {
        // 초기 상태는 비활성화
        SetActivationState(false, false); // 시작 시 이벤트 발생 안함
    }
    
    private void Update()
    {
        // stayActive가 true인 경우 타이머를 감소시키지 않음
        if (stayActive && isActivated) return;
        
        // 타이머가 0보다 크면 감소
        if (activationTimer > 0)
        {
            activationTimer -= Time.deltaTime;
            
            // 타이머가 0이 되면 비활성화
            if (activationTimer <= 0 && isActivated)
            {
                SetActivationState(false);
            }
        }
    }
    
    /// <summary>
    /// 레이저 히트 처리
    /// </summary>
    public void OnLaserHit(Vector2 hitPoint, Vector2 direction)
    {
        // 레이저가 맞았을 때 타이머 설정
        activationTimer = deactivationDelay;
        
        // 이미 활성화 상태가 아니라면 활성화
        if (!isActivated)
        {
            SetActivationState(true);
        }
    }
    
    /// <summary>
    /// 활성화 상태 설정
    /// </summary>
    private void SetActivationState(bool active, bool invokeEvents = true)
    {
        // 상태 변경 없으면 리턴
        if (isActivated == active) return;
        
        isActivated = active;
        
        // 색상 변경
        if (spriteRenderer != null)
        {
            spriteRenderer.color = active ? activeColor : inactiveColor;
        }
        
        // 이벤트 실행
        if (invokeEvents)
        {
            if (active)
            {
                OnSwitchActivated?.Invoke();
                
                // 활성화 효과
                if (activationEffect != null)
                {
                    GameObject effect = Instantiate(activationEffect, transform.position, Quaternion.identity);
                    Destroy(effect, 1f);
                }
                
                // 소리 재생
                if (audioSource != null && activationSound != null)
                {
                    audioSource.clip = activationSound;
                    audioSource.Play();
                }
            }
            else if (!stayActive)
            {
                OnSwitchDeactivated?.Invoke();
            }
        }
        
        Debug.Log($"스위치 상태: {(active ? "활성화" : "비활성화")}");
    }
    
    /// <summary>
    /// 수동으로 스위치 활성화 (외부에서 호출 가능)
    /// </summary>
    public void ActivateSwitch()
    {
        activationTimer = deactivationDelay;
        SetActivationState(true);
    }
    
    /// <summary>
    /// 수동으로 스위치 비활성화 (외부에서 호출 가능)
    /// </summary>
    public void DeactivateSwitch()
    {
        activationTimer = 0;
        SetActivationState(false);
    }
}