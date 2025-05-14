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
        if (!bulletPrefabMap.TryGetValue(type, out var prefab)) prefab = bulletPrefabMap[ElementType.Normal];
        var bulletObj = Instantiate(prefab, pos, rot);
        var bullet = bulletObj.GetComponent<Bullet>();
        bullet.Shooter = shooter; // 발사자 정보 주입
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
            Debug.LogWarning($"Bullet prefab not found for type: {type}");
            return bulletPrefabMap[ElementType.Normal];
        }
    }
} 