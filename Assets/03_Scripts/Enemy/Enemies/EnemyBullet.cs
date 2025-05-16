using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] public float speed = 10f;
    [SerializeField] public float lifetime = 5f;
    [SerializeField] public bool isPiercing = false;  // 관통 여부

    /// <summary>
    /// 총알 데미지 설정
    /// </summary>
    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
    
    private void Start()
    {
        // 일정 시간 후 자동 파괴
        Destroy(gameObject, lifetime);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 충돌한 경우
        if (other.CompareTag("Player"))
        {   
            // 플레이어에게 데미지 주기
            IDamageable player = other.GetComponent<IDamageable>();
            if (player != null)
            {
                player.TakeDamage(damage);
                Debug.Log($"플레이어에게 {damage} 데미지!");
            }
            
            // 관통이 아니면 파괴
            if (!isPiercing)
                Destroy(gameObject);
        }
        // 벽이나 장애물과 충돌
        else if (((1 << other.gameObject.layer) & (LayerMask.GetMask("Ground", "Wall", "NoCollision"))) != 0)
        {
            Destroy(gameObject);
        }
    }
}