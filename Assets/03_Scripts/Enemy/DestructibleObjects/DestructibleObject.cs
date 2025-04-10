using UnityEngine;

public class DestructibleObject : DestructibleEntity
{
    [SerializeField] private GameObject destroyEffectPrefab;
    [SerializeField] private AudioClip destroySound;
    [SerializeField] private Sprite damagedSprite; // 손상된 외형(선택 사항)
    
    [Header("파괴 속성")]
    [SerializeField] private bool enableDestructionForce = false;
    [SerializeField] private float destructionForceRadius = 2f;
    [SerializeField] private float destructionForce = 5f;
    
    private float damageThreshold;
    private bool isDamaged = false;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 50% 손상되었을 때 외형 변경 기준점
        damageThreshold = maxHealth * 0.5f;
    }
    
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        
        // 외형 손상 체크
        if (!isDamaged && currentHealth <= damageThreshold && damagedSprite != null)
        {
            spriteRenderer.sprite = damagedSprite;
            isDamaged = true;
        }
    }
    
    public override void PlayDestructionEffect()
    {
        // 파괴 이펙트 생성
        if (destroyEffectPrefab != null)
        {
            Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // 사운드 재생
        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, transform.position);
        }
        
        // 파괴 힘 적용
        if (enableDestructionForce)
        {
            ApplyDestructionForce();
        }
    }
    
    private void ApplyDestructionForce()
    {
        // 주변 오브젝트에 힘 적용
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, destructionForceRadius);
        
        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject) continue;
            
            Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 direction = (col.transform.position - transform.position).normalized;
                rb.AddForce(direction * destructionForce, ForceMode2D.Impulse);
            }
        }
    }
    
    // 에디터 시각화
    protected void OnDrawGizmosSelected()
    {
        if (enableDestructionForce)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, destructionForceRadius);
        }
    }
}