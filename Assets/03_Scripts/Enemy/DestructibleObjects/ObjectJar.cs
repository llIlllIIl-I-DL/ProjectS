using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유리창 오브젝트 - 파괴 시 유리 파편 생성 일반 공격으로 파괴 가능
/// </summary>
public class ObjectJar : DestructibleObject
{
    #region Variables
    
    [Header("파편 효과")]
    [SerializeField] private int minShardCount; // 최소 파편 개수
    [SerializeField] private int maxShardCount; // 최대 파편 개수
    [SerializeField] private float shardForce; // 파편 튕김 힘
    [SerializeField] private float shardLifetime; // 파편 지속 시간    

    #endregion

    private void Start()
    {

    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        Debug.Log("항아리가 피해를 입었습니다.");
    }
    
    public override void PlayDestructionEffect()
    {
        // 항아리 파편 생성
        CreateShards();
        
        // 효과음 재생
        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, transform.position, 1.0f);
        }
        
        // 충돌체 비활성화
        if (GetComponent<Collider2D>() != null)
        {
            GetComponent<Collider2D>().enabled = false;
        }
        
        // 파괴된 스프라이트로 변경
        if (spriteRenderer != null && destroyedSprite != null)
        {
            spriteRenderer.sprite = destroyedSprite;
            // 알파값을 조정하여 약간 흐리게 표현
            // spriteRenderer.color = new Color(1f, 1f, 1f, 0.8f);
            // 정렬 레이어를 조정하여 뒤쪽에 표시
            spriteRenderer.sortingOrder -= 1;
        }
    }
    
    private void CreateShards()
    {
        if (destroyEffectPrefab == null) return;
        
        // 랜덤하게 파편 개수 결정
        int shardCount = Random.Range(minShardCount, maxShardCount + 1);
        
        for (int i = 0; i < shardCount; i++)
        {
            // 파편 생성 위치에 약간의 랜덤성 추가
            Vector3 spawnPos = transform.position + new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f),
                0f
            );
            
            // 파편 회전에 랜덤성 추가
            Quaternion rotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
            
            // 파편 생성
            GameObject shard = Instantiate(destroyEffectPrefab, spawnPos, rotation);
            
            // 파편에 물리 효과 적용
            Rigidbody2D shardRb = shard.GetComponent<Rigidbody2D>();
            if (shardRb != null)
            {
                // 사방으로 튕겨나가는 효과
                Vector2 randomDir = Random.insideUnitCircle.normalized;
                shardRb.AddForce(randomDir * shardForce, ForceMode2D.Impulse);
                
                // 랜덤한 회전 적용
                shardRb.AddTorque(Random.Range(-10f, 10f), ForceMode2D.Impulse);
            }
            
            // 파편 지속 시간 설정
            Destroy(shard, shardLifetime);
        }
    }

    protected override void DestroyEntity()
    {
        isDestroyed = true;
        PlayDestructionEffect();
        DropItem();
        
        Debug.Log($"{gameObject.name} 파괴된 상태로 변경됨");
    }
}