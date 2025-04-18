using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float damage = 10f;
    public float knockbackForce = 5f;
    public ElementType bulletType = ElementType.Normal;
    
    private bool hasHitEnemy = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 적 레이어 확인
        if (other.CompareTag("Enemy"))
        {
            // 적에게 데미지 주기
            BaseEnemy enemy = other.GetComponent<BaseEnemy>();//Enemy로 수정 필요

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                hasHitEnemy = true;

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
    
    // 총알이 파괴될 때 호출되는 함수
    private void OnDestroy()
    {
        // 과열 공격이고 적에게 명중했을 경우, PlayerWeapon에서 이미 처리됩니다.
        // 여기서는 추가적인 처리가 필요 없습니다.
    }
}