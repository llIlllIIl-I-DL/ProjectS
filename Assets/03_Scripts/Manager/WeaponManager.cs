using System.Collections;
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

    [Header("차징 공격 설정")]
    [SerializeField] private GameObject chargedBulletPrefab;
    [SerializeField] private float chargedBulletDamage = 20f;
    [SerializeField] private float chargingTime = 0.7f; // 차지에 필요한 시간
    private bool isCharging = false;
    private float currentChargeTime = 0f;
    [SerializeField] private bool debugCharging = false; // 디버그용 변수 추가

    [Header("과열 공격 설정")]
    [SerializeField] private GameObject overchargeBulletPrefab;
    [SerializeField] private float overchargeBulletDamage = 30f; // 일반 공격의 3배
    [SerializeField] private float overchargeHealthCost = 0.05f; // 최대 체력의 5%
    [SerializeField] private float bulletSpeedMultiplier = 1.5f; // 플레이어 속도의 1.5배

    private PlayerMovement playerMovement;
    private PlayerHP playerHP;

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
            Debug.LogError("WeaponManager: bulletPrefab이 인스펙터에서 할당되지 않았습니다");
        }

        // 차징 공격 프리팹 검사
        if (chargedBulletPrefab == null)
        {
            chargedBulletPrefab = bulletPrefab;
            Debug.LogWarning("WeaponManager: chargedBulletPrefab이 할당되지 않아 일반 총알로 대체됩니다.");
        }

        // 과열 공격 프리팹 검사
        if (overchargeBulletPrefab == null)
        {
            overchargeBulletPrefab = bulletPrefab;
            Debug.LogWarning("WeaponManager: overchargeBulletPrefab이 할당되지 않아 일반 총알로 대체됩니다.");
        }

        // 플레이어 컴포넌트 참조 가져오기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            playerHP = player.GetComponent<PlayerHP>();
        }
    }

    private void Update()
    {
        // 차징 시간 누적
        if (isCharging)
        {
            currentChargeTime += Time.deltaTime;
            
            // 디버그 로그 출력 (필요시 활성화)
            if (debugCharging && currentChargeTime % 0.5f < 0.02f)
            {
                Debug.Log($"현재 차징 시간: {currentChargeTime}초");
            }
            
            // 차징 단계 로그
            if (currentChargeTime >= chargingTime * 2 && currentChargeTime < chargingTime * 2 + Time.deltaTime)
                Debug.Log("과열 공격 차징 완료!");
            else if (currentChargeTime >= chargingTime && currentChargeTime < chargingTime + Time.deltaTime)
                Debug.Log("차징 공격 차징 완료!");
        }
    }

    public void StartCharging()
    {
        if (isReloading || currentAmmo <= 0) return; // 재장전 중이거나 탄약이 없으면 차징 불가
        
        isCharging = true;
        currentChargeTime = 0f;
        Debug.Log("차징 시작 - isCharging: " + isCharging);
    }
    
    public void StopCharging()
    {
        Debug.Log("StopCharging 호출됨 - 현재 isCharging: " + isCharging + ", 차징 시간: " + currentChargeTime);
        
        // 차징 상태가 아니거나 매우 짧은 클릭은 일반 공격으로 처리
        if (!isCharging || currentChargeTime < 0.1f)
        {
            Debug.Log("일반 탭 공격 발사!");
            FireWeapon(GetAimDirection());
            isCharging = false; // 상태 초기화 추가
            currentChargeTime = 0f; // 시간 초기화 추가
            return;
        }
        
        // 차징 상태였다면 차징 시간에 따른 공격 발사
        Debug.Log($"차징 종료 - 누적 시간: {currentChargeTime}초");
        FireChargedBullet();
        isCharging = false;
        currentChargeTime = 0f;
    }

    private Vector2 GetAimDirection()
    {
        // 플레이어가 바라보는 방향 반환 (우측이 기본)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.flipX)
            {
                return Vector2.left;
            }
        }
        return Vector2.right;
    }

    private void FireChargedBullet()
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

        GameObject bulletToSpawn;
        float damage;
        Vector3 scale = Vector3.one;

        // 차징 시간에 따라 총알 타입 결정
        if (currentChargeTime >= chargingTime * 2)
        {
            Debug.Log("과열 공격 발사! 차징 시간: " + currentChargeTime);
            bulletToSpawn = overchargeBulletPrefab != null ? overchargeBulletPrefab : bulletPrefab;
            damage = overchargeBulletDamage;
            scale = new Vector3(2f, 2f, 2f);
            
            // 플레이어에게 최대 체력의 일정 비율만큼 데미지
            if (playerHP != null)
            {
                float healthCost = playerHP.GetMaxHealth() * overchargeHealthCost;
                playerHP.TakeDamage(healthCost);
            }
        }
        else if (currentChargeTime >= chargingTime)
        {
            Debug.Log("차징 공격 발사! 차징 시간: " + currentChargeTime);
            bulletToSpawn = chargedBulletPrefab != null ? chargedBulletPrefab : bulletPrefab;
            damage = chargedBulletDamage;
            scale = new Vector3(1.5f, 1.5f, 1.5f);
        }
        else
        {
            Debug.Log("일반 공격 발사! 차징 시간: " + currentChargeTime);
            bulletToSpawn = bulletPrefab;
            damage = 10f;
        }

        Vector2 direction = GetAimDirection();
        
        // 총알 발사 위치 계산 (플레이어 앞쪽)
        Vector3 spawnPosition = firePoint.position + new Vector3(direction.x * 0.2f, 0, 0);

        // 총알 생성 및 방향 설정
        GameObject bullet = Instantiate(bulletToSpawn, spawnPosition, Quaternion.identity);
        bullet.transform.localScale = scale;

        // 총알 속도 설정
        float bulletSpeedFinal = bulletSpeed;
        if (playerMovement != null)
        {
            bulletSpeedFinal = playerMovement.GetCurrentMoveSpeed() * bulletSpeedMultiplier;
        }
        
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.velocity = direction * bulletSpeedFinal;
        }
        else
        {
            Debug.LogWarning("WeaponManager: 총알 프리팹에 Rigidbody2D 컴포넌트가 없습니다.");
        }

        // 총알 회전 설정
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 총알 데미지 설정
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
        {
            bulletComponent.damage = damage;
        }

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