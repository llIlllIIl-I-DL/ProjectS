using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float lifeTime = 3f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 예: 플레이어와 충돌 시 데미지 처리
        if (collision.CompareTag("Player"))
        {
            Debug.Log("플레이어 적중!");
            Destroy(gameObject);
        }
    }
}
