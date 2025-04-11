using UnityEngine;

public class ExplosiveObject : DestructibleObject
{
    [Header("폭발 속성")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionDamage = 5f;
    [SerializeField] private GameObject explosionEffectPrefab;
    
    public override void PlayDestructionEffect()
    {
        base.PlayDestructionEffect();
        
        // 폭발 이펙트 생성
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // 주변 오브젝트에 데미지 주기
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach(var collider in colliders)
        {
            if (collider.gameObject == gameObject) continue;
            
            IDamageable damageable = collider.GetComponent<IDamageable>();
            if(damageable != null)
            {
                damageable.TakeDamage(explosionDamage);
            }
        }
    }
    
    private new void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        // 폭발 범위 시각화
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }
}