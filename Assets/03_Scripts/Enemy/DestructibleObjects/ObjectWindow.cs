using System.Collections.Generic;
using UnityEngine;

public class ObjectWindow : DestructibleObject
{
    [Header("유리 속성")]
    [SerializeField] private Material glassMaterial; // 유리 재질
    
    [Header("유리 파편 효과")]
    [SerializeField] private int minShardCount; // 최소 파편 개수
    [SerializeField] private int maxShardCount; // 최대 파편 개수
    [SerializeField] private float shardForce; // 파편 튕김 힘
    [SerializeField] private float shardLifetime; // 파편 지속 시간
    
    private void Start()
    {
        // 유리 재질 적용
        if (glassMaterial != null && spriteRenderer != null)
        {
            spriteRenderer.material = glassMaterial;
        }
    }
    
    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
    }
    
    public override void PlayDestructionEffect()
    {
        // 유리 파편 생성
        CreateGlassShards();
        
        // 유리 깨지는 효과음 재생
        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, transform.position, 1.0f);
        }
        
        // 파괴된 유리창은 충돌체와 렌더러를 비활성화
        if (GetComponent<Collider2D>() != null)
            GetComponent<Collider2D>().enabled = false;
            
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }
    
    private void CreateGlassShards()
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
}