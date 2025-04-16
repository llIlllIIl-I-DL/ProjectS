using System.Collections;
using UnityEngine;

public class SpikeTrap : BaseTrap
{
    [Header("가시 설정")]
    [SerializeField] private float extendSpeed = 5f; // 가시가 확장되는 속도
    [SerializeField] private float retractSpeed = 2f; // 가시가 수축되는 속도
    [SerializeField] private float extendDistance = 1f; // 가시가 확장되는 거리
    [SerializeField] private bool autoActivate = true; // 자동으로 활성화되는지 여부
    [SerializeField] private float activationInterval = 3f; // 자동 활성화 간격
    
    private bool isExtended = false; // 가시가 확장되었는지 여부
    private Vector3 retractedPosition; // 가시의 기본 위치
    private Vector3 extendedPosition; // 가시의 확장된 위치
    private float timer = 0f; // 타이머
    private Animator animator; // 애니메이터
    
    protected override void Initialize()
    {
        base.Initialize();
        retractedPosition = transform.position;
        extendedPosition = retractedPosition + Vector3.up * extendDistance;
        animator = GetComponent<Animator>();
        
        if (autoActivate)
            StartCoroutine(AutoActivationCycle());
    }
    
    private IEnumerator AutoActivationCycle()
    {
        while (autoActivate)
        {
            yield return new WaitForSeconds(activationInterval);
            ActivateTrap();
            yield return new WaitForSeconds(1f);
            DeactivateTrap();
        }
    }
    
    public override void ActivateTrap()
    {
        if (isActive && !isExtended)
        {
            isExtended = true;
            
            if (animator != null)
                animator.SetTrigger("Extend");
            else
                StartCoroutine(ExtendSpikes());
        }
    }
    
    public override void DeactivateTrap()
    {
        if (isExtended)
        {
            isExtended = false;
            
            if (animator != null)
                animator.SetTrigger("Retract");
            else
                StartCoroutine(RetractSpikes());
        }
    }
    
    private IEnumerator ExtendSpikes()
    {
        float t = 0;
        Vector3 startPos = transform.position;
        
        while (t < 1)
        {
            t += Time.deltaTime * extendSpeed;
            transform.position = Vector3.Lerp(startPos, extendedPosition, t);
            yield return null;
        }
    }
    
    private IEnumerator RetractSpikes()
    {
        float t = 0;
        Vector3 startPos = transform.position;
        
        while (t < 1)
        {
            t += Time.deltaTime * retractSpeed;
            transform.position = Vector3.Lerp(startPos, retractedPosition, t);
            yield return null;
        }
    }
    
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);
        
        if (triggeredByPlayer && other.CompareTag("Player"))
        {
            Debug.Log("가시와 충돌");
            OnTrapContact(other.gameObject);
        }
    }
    
    // BaseTrap 추상 메서드 구현
    protected override void OnInteract(GameObject interactor)
    {
        // 필요한 경우 구현
    }
}