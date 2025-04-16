using System.Collections;
using UnityEngine;


public abstract class BaseTrap : BaseObject
{
    [Header("트랩 기본 설정")]
    [SerializeField] protected bool isActive = true; // 트랩 활성화 여부
    [SerializeField] protected float damage = 100f; // 트랩이 입히는 데미지
    [SerializeField] protected bool triggeredByPlayer = true; // 플레이어에 의해 트랩이 작동하는지 여부
    [SerializeField] protected bool resetAfterTrigger = false; // 트랩이 작동 후 리셋되는지 여부
    [SerializeField] protected float resetDelay = 3f; // 리셋 지연 시간
    
    [Header("효과")]
    [SerializeField] protected ParticleSystem activationEffect;
    [SerializeField] protected AudioClip activationSound;
    
    // 트랩이 작동하는 메서드
    public abstract void ActivateTrap();
    
    // 트랩이 비활성화되는 메서드
    public abstract void DeactivateTrap();

    // 트랩의 오토상태를 변경하는 메서드
    public abstract void ToggleAutoTrap();
    
    // 플레이어와 접촉 시 호출
    protected virtual void OnTrapContact(GameObject target)
    {
        if (!isActive) return;
        
        // IDamageable 인터페이스를 구현한 오브젝트에 데미지 적용
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            
            // 효과 재생
            if (activationEffect != null)
                activationEffect.Play();
                
            if (activationSound != null)    
                AudioSource.PlayClipAtPoint(activationSound, transform.position);
                
            if (resetAfterTrigger)
            {
                StartCoroutine(ResetAfterDelay());
            }
        }
    }
    
    protected IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);
        DeactivateTrap();
    }
}