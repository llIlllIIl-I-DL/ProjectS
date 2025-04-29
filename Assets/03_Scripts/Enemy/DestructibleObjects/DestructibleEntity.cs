using System.Collections;
using UnityEngine;

/// <summary>
/// 파괴 가능한 오브젝트의 기본 클래스
/// 상속하여 다양한 파괴 가능한 오브젝트를 구현할 수 있음
/// </summary>
public abstract class DestructibleEntity : MonoBehaviour, IDestructible
{
    #region Variables

    [Header("기본 속성")]
    [SerializeField] protected float maxHealth;
    [SerializeField] protected float currentHealth;
    
    [Header("드롭 아이템")]
    [SerializeField] protected GameObject[] possibleDrops;
    [SerializeField] protected float dropChance = 0.3f;
    
    protected bool isDestroyed = false;
    protected SpriteRenderer spriteRenderer;
    protected Coroutine flashCoroutine; // 코루틴 참조를 저장할 변수 추가
    
    #endregion

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    /// <summary>
    /// 피격 시 깜박임 효과 - 개선된 버전
    /// </summary>
    protected virtual IEnumerator FlashEffect()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
        
        // 코루틴 참조 초기화
        flashCoroutine = null;
    }

    /// <summary>
    /// 피격 처리 - 외부에서 호출
    /// </summary>
    public virtual void TakeDamage(float damage)
    {
        if (isDestroyed) return;
        
        currentHealth -= damage;
        
        // 피격 효과 관리 개선
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white; // 기본 색상으로 리셋
            }
        }
        
        // 새 코루틴 시작 및 참조 저장
        flashCoroutine = StartCoroutine(FlashEffect());
        
        // 파괴 체크
        if (currentHealth <= 0)
        {
            Destroy();
        }
    }
    
    // <summary>
    // 파괴 처리
    // </summary>
    protected virtual void Destroy()
    {
        isDestroyed = true;
        PlayDestructionEffect();
        DropItem();
        
        // 콜라이더 비활성화
        GetComponent<Collider2D>().enabled = false;
        
        // 지연 파괴
        Destroy(gameObject, 1f);
        Debug.Log($"{gameObject.name} 파괴 됨");
    }
    
    // <summary>
    // 드롭 아이템 생성
    // </summary>
    public virtual void DropItem()
    {
        if (possibleDrops == null || possibleDrops.Length == 0) return;
        
        if (Random.value <= dropChance)
        {
            int dropIndex = Random.Range(0, possibleDrops.Length);
            Instantiate(possibleDrops[dropIndex], transform.position, Quaternion.identity);
        }
    }
    
    // <summary>
    // 파괴 이펙트 재생
    // </summary>
    // 하위 클래스에서 구현
    public abstract void PlayDestructionEffect();
}