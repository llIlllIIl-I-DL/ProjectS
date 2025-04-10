using System.Collections;
using UnityEngine;

public abstract class DestructibleEntity : MonoBehaviour, IDestructible
{
    [Header("기본 속성")]
    [SerializeField] protected float maxHealth;
    [SerializeField] protected float currentHealth;
    
    [Header("드롭 아이템")]
    [SerializeField] protected GameObject[] possibleDrops;
    [SerializeField] protected float dropChance = 0.3f;
    
    protected bool isDestroyed = false;
    protected SpriteRenderer spriteRenderer;
    
    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    public virtual void TakeDamage(float damage)
    {
        if (isDestroyed) return;
        
        currentHealth -= damage;
        
        // 피격 효과
        StartCoroutine(FlashEffect());
        
        // 파괴 체크
        if (currentHealth <= 0)
        {
            Destroy();
        }
    }
    
    protected virtual void Destroy()
    {
        isDestroyed = true;
        PlayDestructionEffect();
        DropItem();
        
        // 콜라이더 비활성화
        GetComponent<Collider2D>().enabled = false;
        
        // 지연 파괴
        Destroy(gameObject, 1f);
    }
    
    public virtual void DropItem()
    {
        if (possibleDrops == null || possibleDrops.Length == 0) return;
        
        if (Random.value <= dropChance)
        {
            int dropIndex = Random.Range(0, possibleDrops.Length);
            Instantiate(possibleDrops[dropIndex], transform.position, Quaternion.identity);
        }
    }
    
    public abstract void PlayDestructionEffect();
    
    // 피격 효과
    protected virtual IEnumerator FlashEffect()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
}