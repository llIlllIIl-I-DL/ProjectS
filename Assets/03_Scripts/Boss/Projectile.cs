using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float lifeTime = 3f;
    public float damage = 10f;
    public bool isPiercing = false;  // 관통 여부

    private void Start()
    {
        Destroy(gameObject, lifeTime);
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
        else if (((1 << other.gameObject.layer) & (LayerMask.GetMask("Ground", "Wall"))) != 0)
        {
            Destroy(gameObject);
        }
    }
}
