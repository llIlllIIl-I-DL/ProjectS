using UnityEngine;

/// <summary>
/// 파괴 가능한 오브젝트 - 파괴 시 이펙트 및 힘 적용
/// </summary>
/// <remarks>
/// 이 클래스는 파괴 가능한 오브젝트의 기본 기능을 구현
/// 상속하여 다양한 파괴 가능한 오브젝트를 구현할 수 있음
public class DestructibleObject : DestructibleEntity
{
    #region Variables

    [Header("파괴 이펙트")]
    [SerializeField] protected GameObject destroyEffectPrefab; // 파괴 이펙트 프리팹
    [SerializeField] protected GameObject dustEffectPrefab; // 먼지 이펙트 프리팹(선택 사항)

    // 사운드 관련 변수들은 추후에 AudioManager에서 관리하게 될 수도 있어 선택사항으로 붙여 놓았음
    [SerializeField] protected AudioClip destroySound; // 파괴 사운드(선택 사항)
    [SerializeField] protected AudioClip hitSound; // 피격 사운드(선택 사항)
    [SerializeField] protected Sprite damagedSprite; // 손상된 외형(선택 사항)
    
    [Header("파괴 속성")]
    [SerializeField] protected bool enableDestructionForce = false; // 파괴 힘 적용 여부
    [SerializeField] protected float destructionForceRadius; // 파괴 힘 반경
    [SerializeField] protected float destructionForce; // 파괴 힘 세기
    
    private float damageThreshold;
    private bool isDamaged = false;
    
    #endregion

    protected override void Awake()
    {
        base.Awake();
        
        // 50% 손상되었을 때 외형 변경 기준점
        damageThreshold = maxHealth * 0.5f;
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        // 피격 사운드 재생
        if (hitSound != null)
        {
            // AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
        
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
            // AudioSource.PlayClipAtPoint(destroySound, transform.position);
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