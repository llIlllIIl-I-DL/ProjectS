using UnityEngine;

public class WeaponManager : Singleton<WeaponManager>
{
    [Header("총알 설정")]
    [SerializeField] private GameObject bulletPrefab; // 인스펙터에서 할당
    [SerializeField] private Transform firePoint;     // 총알이 발사되는 위치
    [SerializeField] private float bulletSpeed = 15f;
    [SerializeField] private float bulletLifetime = 3f;  // 총알 지속 시간

    [Header("무기 상태")]
    public int currentAmmo = 30;
    public int maxAmmo = 30;
    public float reloadTime = 1.5f;
    public bool isReloading = false;

    private void Start()
    {
        // 총알 발사 위치가 설정되지 않은 경우 플레이어의 위치로 설정
        if (firePoint == null)
        {
            firePoint = transform;
            Debug.LogWarning("WeaponManager: firePoint가 설정되지 않아 플레이어 위치로 기본 설정됩니다.");
        }
        
        // 총알 프리팹이 할당되었는지 검사
        if (bulletPrefab == null)
        {
            Debug.LogError("WeaponManager: bulletPrefab이 인스펙터에서 할당되지 않았습니다. " +
                          "Assets/02_Prefab 폴더에서 Bullet 프리팹을 WeaponManager의 bulletPrefab 필드에 할당해주세요.");
        }
    }

    public void FireWeapon(Vector2 direction)
    {
        // 재장전 중이거나 탄약 없으면 발사 불가
        if (isReloading || currentAmmo <= 0)
        {
            if (currentAmmo <= 0 && !isReloading)
            {
                StartCoroutine(Reload());
            }
            return;
        }
        
        // 총알 프리팹이 없으면 발사 불가
        if (bulletPrefab == null)
        {
            Debug.LogError("WeaponManager: 총알 프리팹이 할당되지 않아 발사할 수 없습니다.");
            return;
        }

        // 총알 발사 위치 계산 (플레이어 앞쪽)
        Vector3 spawnPosition = firePoint.position + new Vector3(direction.x * 0.2f, 0, 0);

        // 총알 생성 및 방향 설정
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

        // 총알 속도 설정
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.velocity = direction * bulletSpeed;
        }
        else
        {
            Debug.LogWarning("WeaponManager: 총알 프리팹에 Rigidbody2D 컴포넌트가 없습니다.");
        }

        // 총알 회전 설정
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 총알 소멸 처리
        Destroy(bullet, bulletLifetime);

        // 탄약 감소
        currentAmmo--;

        // 탄약 소진 시 재장전
        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
        }
    }

    private System.Collections.IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("재장전 중...");

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
        Debug.Log("재장전 완료!");
    }
}