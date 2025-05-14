using UnityEngine;

// 총알 팩토리 클래스
public class BulletFactory : MonoBehaviour
{
    // 총알 프리팹들
    [SerializeField] private GameObject normalBulletPrefab;
    [SerializeField] private GameObject rustBulletPrefab;
    [SerializeField] private GameObject ironBulletPrefab;
    [SerializeField] private GameObject poisonBulletPrefab;
    [SerializeField] private GameObject waterBulletPrefab;
    [SerializeField] private GameObject flameBulletPrefab;
    [SerializeField] private GameObject iceBulletPrefab;
    
    private GameObject[] BulletPrefabs;

    // 총알 생성 메서드
    // 리턴형이 Bullet 이나 제네릭인 경우 활용이 더 편함
    public GameObject CreateBullet(ElementType type, Vector3 position, Quaternion rotation, bool isOvercharged = false)
    {
        GameObject bulletObject = null;

        
        bulletObject = Instantiate(BulletPrefabs[(int)type], position, rotation);
        
        // 배열이나 딕셔너리에 보관하는 것도 방법
        // 속성 타입에 따른 총알 생성
        switch (type)
        {
            case ElementType.Normal:
                bulletObject = Instantiate(normalBulletPrefab, position, rotation);
                break;
            case ElementType.Rust:
                bulletObject = Instantiate(rustBulletPrefab, position, rotation);
                break;
            case ElementType.Iron:
                bulletObject = Instantiate(ironBulletPrefab, position, rotation);
                break;
            case ElementType.Poison:
                bulletObject = Instantiate(poisonBulletPrefab, position, rotation);
                break;
            case ElementType.Water:
                bulletObject = Instantiate(waterBulletPrefab, position, rotation);
                break;
            case ElementType.Flame:
                bulletObject = Instantiate(flameBulletPrefab, position, rotation);
                break;
            case ElementType.Ice:
                bulletObject = Instantiate(iceBulletPrefab, position, rotation);
                break;
            default:
                bulletObject = Instantiate(normalBulletPrefab, position, rotation);
                break;
        }

        // 총알이 생성됐다면 과열 상태 설정
        if (bulletObject != null)
        {
            Bullet bulletComponent = bulletObject.GetComponent<Bullet>();
            if (bulletComponent != null)
            {
                bulletComponent.IsOvercharged = isOvercharged;
            }
        }

        return bulletObject;
    }
    public GameObject GetBulletPrefab(ElementType type)
    {
        switch (type)
        {
            case ElementType.Normal:
                return normalBulletPrefab;
            case ElementType.Rust:
                return rustBulletPrefab;
            case ElementType.Iron:
                return ironBulletPrefab;
            case ElementType.Poison:
                return poisonBulletPrefab;
            case ElementType.Water:
                return waterBulletPrefab;
            case ElementType.Flame:
                return flameBulletPrefab;
            case ElementType.Ice:
                return iceBulletPrefab;
            default:
                return normalBulletPrefab;
        }
    }
} 