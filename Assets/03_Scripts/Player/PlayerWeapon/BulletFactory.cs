using UnityEngine;
using System.Collections.Generic;

// 총알 팩토리 클래스
public class BulletFactory : MonoBehaviour
{
    [SerializeField] private GameObject[] bulletPrefabs; // Enum 순서와 일치
    private Dictionary<ElementType, GameObject> bulletPrefabMap;
    private Dictionary<ElementType, ObjectPoolingManager.PoolType> playerBulletTypeMap;

    private void Awake()
    {
        bulletPrefabMap = new Dictionary<ElementType, GameObject>();
        for (int i = 0; i < bulletPrefabs.Length; i++)
        {
            bulletPrefabMap[(ElementType)i] = bulletPrefabs[i];
        }

        // 플레이어 총알 타입 매핑 초기화
        playerBulletTypeMap = new Dictionary<ElementType, ObjectPoolingManager.PoolType>
        {
            { ElementType.Normal, ObjectPoolingManager.PoolType.NormalBullet },
            { ElementType.Rust, ObjectPoolingManager.PoolType.RustBullet },
            { ElementType.Iron, ObjectPoolingManager.PoolType.IronBullet },
            { ElementType.Poison, ObjectPoolingManager.PoolType.PoisonBullet },
            { ElementType.Water, ObjectPoolingManager.PoolType.WaterBullet },
            { ElementType.Flame, ObjectPoolingManager.PoolType.FlameBullet },
            { ElementType.Ice, ObjectPoolingManager.PoolType.IceBullet }
        };
    }

    public GameObject CreateBullet(ElementType type, Vector3 pos, Quaternion rot, GameObject shooter)
    {
        // 발사자에 따른 총알 타입 체크
        ObjectPoolingManager.PoolType poolType;
        
        // 플레이어가 발사하는 경우
        if (shooter.CompareTag("Player"))
        {
            if (!playerBulletTypeMap.TryGetValue(type, out poolType))
            {
                Debug.LogWarning($"플레이어용 총알 타입이 아닙니다: {type}");
                return null;
            }
        }
        // 적이 발사하는 경우
        else if (shooter.CompareTag("Enemy") || shooter.CompareTag("Boss"))
        {
            poolType = ObjectPoolingManager.PoolType.EnemyBullet;
        }
        else
        {
            Debug.LogWarning($"알 수 없는 발사자 타입입니다: {shooter.tag}");
            return null;
        }

        // 해당 풀 타입의 총알 가져오기
        GameObject bulletObj = ObjectPoolingManager.Instance.GetObject(poolType, null);
        if (bulletObj == null)
        {
            Debug.LogWarning($"Failed to get bullet from pool for type: {poolType}");
            return null;
        }

        // 총알 위치와 회전 설정
        bulletObj.transform.position = pos;
        bulletObj.transform.rotation = rot;

        // 발사자 정보 설정
        var bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Shooter = shooter;
        }

        return bulletObj;
    }

    public GameObject GetBulletPrefab(ElementType type)
    {
        if (bulletPrefabMap.TryGetValue(type, out var prefab))
        {
            return prefab;
        }
        else
        {
            Debug.LogError($"Bullet prefab not found for type: {type}. Please check if all bullet prefabs are properly assigned in the inspector.");
            if (bulletPrefabMap.TryGetValue(ElementType.Normal, out var normalPrefab))
            {
                return normalPrefab;
            }
            else
            {
                Debug.LogError("Normal bullet prefab is also missing! Please assign at least the Normal bullet prefab in the inspector.");
                return null;
            }
        }
    }
}