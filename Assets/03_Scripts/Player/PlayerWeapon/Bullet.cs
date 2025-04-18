using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float baseDamage = 10f;
    private float damageMultiplier = 1f;
    private float knockbackForce = 5f;
    private bool isOvercharge = false;

    public void SetDamage(float multiplier)
    {
        damageMultiplier = multiplier;
    }

    // 오버차지 상태 설정
    public void SetIsOvercharge(bool overcharge)
    {
        isOvercharge = overcharge;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 적 레이어 확인
        if (other.CompareTag("Enemy"))
        {
            // 적에게 데미지 주기
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float damage = baseDamage * damageMultiplier;
                damageable.TakeDamage(damage);

                // 오버차지 상태이고 적에게 명중했다면 플레이어에게 데미지
                if (isOvercharge)
                {
                    WeaponManager.Instance.ApplyOverchargeDamageToPlayer();
                }

                // 넉백 적용
                Rigidbody2D enemyRb = other.GetComponent<Rigidbody2D>();
                if (enemyRb != null)
                {
                    Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
                    enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
                }
            }

            // 총알 제거
            Destroy(gameObject);
        }
        // 벽이나 다른 장애물과 충돌
        else if (((1 << other.gameObject.layer) & (LayerMask.GetMask("Ground", "Wall"))) != 0)
        {
            // 총알 제거
            Destroy(gameObject);
        }
    }
}