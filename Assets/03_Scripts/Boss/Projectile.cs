using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("기본 설정")]
    public float lifeTime = 10f;          // 투사체가 살아있는 시간
    public float damage = 10f;           // 기본 데미지
    public bool isPiercing = false;      // 관통 여부

    private void Start()
    {
        // 일정 시간이 지나면 자동으로 파괴
        // 프로젝타일은 자주 사용되는 객체인데 파괴보다 재활용이 좋지 않을까?? - 오브젝트폴
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 충돌한 경우
        if (other.CompareTag("Player"))
        {
            // 데미지를 받을 수 있는 인터페이스를 가진 컴포넌트가 있다면 데미지 부여
            IDamageable player = other.GetComponent<IDamageable>();
            if (player != null)
            {
                player.TakeDamage(damage);
                Debug.Log($"플레이어에게 {damage} 데미지!");
            }

            // 관통 투사체가 아닐 경우 파괴
            if (!isPiercing)
                Destroy(gameObject);
        }
        // 벽이나 장애물 충돌 (LayerMask: Ground, Wall)
        else if (((1 << other.gameObject.layer) & LayerMask.GetMask("Ground", "Wall")) != 0)
        {
            Destroy(gameObject);
        }
    }
}
