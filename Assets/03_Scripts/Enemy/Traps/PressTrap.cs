using System.Collections;
using UnityEngine;

public class PressTrap : BaseTrap
{
    [Header("프레스 설정")]
    [SerializeField] private float pressSpeed = 10f;
    [SerializeField] private float returnSpeed = 3f;
    [SerializeField] private float pressDistance = 3f;
    [SerializeField] private float pauseTime = 1f;
    [SerializeField] private bool autoActivate = true;
    [SerializeField] private float activationInterval = 5f;
    
    private bool isPressing = false;
    private Vector3 startPosition;
    private Vector3 pressedPosition;
    private float timer = 0f;
    private Animator animator;
    
    protected override void Initialize()
    {
        base.Initialize();
        startPosition = transform.position;
        pressedPosition = startPosition + Vector3.down * pressDistance;
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
            yield return new WaitForSeconds(pauseTime + (pressDistance / pressSpeed));
            DeactivateTrap();
        }
    }
    
    public override void ActivateTrap()
    {
        if (isActive && !isPressing)
        {
            isPressing = true;
            
            if (animator != null)
                animator.SetTrigger("Press"); // 조금 더 자연스러운 연출을 위해 애니메이션은 차차 생각해봐야함
            else
                StartCoroutine(PressCrush());
        }
    }
    
    public override void DeactivateTrap()
    {
        if (isPressing)
        {
            isPressing = false;
            
            if (animator != null)
                animator.SetTrigger("Return");
            else
                StartCoroutine(PressReturn());
        }
    }

    public override void ToggleAutoTrap()
    {   
        if (!autoActivate)
        {
            autoActivate = true;
            StartCoroutine(AutoActivationCycle());
        }
        else
        {
            autoActivate = false;
            StopCoroutine(AutoActivationCycle());
        }
    }

    private IEnumerator PressCrush()
    {
        float t = 0;
        Vector3 startPos = transform.position;
        
        // 경고 사운드 또는 시각 효과
        if (activationSound != null)
            AudioSource.PlayClipAtPoint(activationSound, transform.position, 0.5f);
            
        yield return new WaitForSeconds(0.5f); // 경고 시간
        
        while (t < 1)
        {
            t += Time.deltaTime * pressSpeed;
            transform.position = Vector3.Lerp(startPos, pressedPosition, t);
            yield return null;
        }
        
        // 프레스 닿은 상태에서 잠시 대기
        yield return new WaitForSeconds(pauseTime);
    }
    
    private IEnumerator PressReturn()
    {
        float t = 0;
        Vector3 startPos = transform.position;
        
        while (t < 1)
        {
            t += Time.deltaTime * returnSpeed;
            transform.position = Vector3.Lerp(startPos, startPosition, t);
            yield return null;
        }
    }
    
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (isPressing && triggeredByPlayer && other.CompareTag("Player"))
        {
            OnTrapContact(other.gameObject);
        }
    }
    
    // BaseTrap 추상 메서드 구현
    protected override void OnInteract(GameObject interactor)
    {
        // 필요한 경우 구현
    }
}