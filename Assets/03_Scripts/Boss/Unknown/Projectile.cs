using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("기본 설정")]
    public float lifeTime = 10f;
    public float damage = 10f;
    public bool isPiercing = false;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 충돌한 경우
        if (other.CompareTag(GameConstants.Tags.PLAYER))
        {
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
        else if (((1 << other.gameObject.layer) & 
                LayerMask.GetMask(GameConstants.Layers.GROUND, GameConstants.Layers.WALL)) != 0)
        {
            Destroy(gameObject);
        }
    }
}