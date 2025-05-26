using UnityEngine;
using System.Collections.Generic;

// 총알 팩토리 클래스
public class BulletFactory : MonoBehaviour
{
    [SerializeField] private GameObject[] bulletPrefabs; // Enum 순서와 일치
    private Dictionary<ElementType, GameObject> bulletPrefabMap;

    private void Awake()
    {
        bulletPrefabMap = new Dictionary<ElementType, GameObject>();
        for (int i = 0; i < bulletPrefabs.Length; i++)
        {
            bulletPrefabMap[(ElementType)i] = bulletPrefabs[i];
        }
    }

    public GameObject CreateBullet(ElementType type, Vector3 pos, Quaternion rot, GameObject shooter)
    {
        // ObjectPoolingManager를 통해 총알 가져오기
        GameObject bulletObj = ObjectPoolingManager.Instance.GetBullet(type, shooter.transform);
        if (bulletObj == null)
        {
            Debug.LogWarning($"Failed to get bullet from pool for type: {type}");
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