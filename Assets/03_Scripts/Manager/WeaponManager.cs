using UnityEngine;
using System.Collections.Generic;

public class WeaponManager : Singleton<WeaponManager>
{
    [Header("무기 관리")]
    [SerializeField] private WeaponData currentWeapon;  // 현재 장착된 무기
    [SerializeField] private List<WeaponData> availableWeapons = new List<WeaponData>();  // 보유 무기 목록
    [SerializeField] private Transform firePoint;     // 총알이 발사되는 위치
    public Transform FirePoint
    {
        get => firePoint;
        set => firePoint = value;
    }  // 외부에서 접근할 수 있도록 프로퍼티 제공

    [Header("무기 상태")]
    public int currentAmmo;
    public bool isReloading = false;
    private float nextFireTime = 0f;  // 발사 속도 제어용

    [Header("차징 상태")]
    private float currentChargeTime = 0f;
    private bool isCharging = false;
    private bool isOvercharged = false;

    private void Start()
    {
        // 총알 발사 위치가 설정되지 않은 경우 플레이어의 위치로 설정
        if (firePoint == null)
        {
            firePoint = transform;
            Debug.LogWarning("WeaponManager: firePoint가 설정되지 않아 플레이어 위치로 기본 설정됩니다.");
        }
        
        // 기본 무기 설정
        if (currentWeapon == null && availableWeapons.Count > 0)
        {
            EquipWeapon(availableWeapons[0]);
        }
        else if (currentWeapon != null)
        {
            currentAmmo = currentWeapon.maxAmmo;
        }
        else
        {
            Debug.LogError("WeaponManager: 사용 가능한 무기가 없습니다!");
        }
    }
    
    // 무기 장착 메서드
    public void EquipWeapon(WeaponData weaponData)
    {
        if (weaponData == null) return;
        
        currentWeapon = weaponData;
        currentAmmo = weaponData.maxAmmo;
        isReloading = false;
        ResetCharge();
        
        Debug.Log($"{currentWeapon.weaponName} 무기를 장착했습니다!");
    }
    
    // 다음 무기로 교체하는 메서드
    public void SwitchToNextWeapon()
    {
        if (availableWeapons.Count <= 1) return;
        
        int currentIndex = availableWeapons.IndexOf(currentWeapon);
        int nextIndex = (currentIndex + 1) % availableWeapons.Count;
        EquipWeapon(availableWeapons[nextIndex]);
    }
    
    // 무기 발사 가능 여부 확인
    public bool CanFire()
    {
        return !isReloading && currentAmmo > 0 && Time.time >= nextFireTime && currentWeapon != null;
    }

    public void FireWeapon(Vector2 direction)
    {
        // 무기가 없거나 발사 불가능한 상태면 발사 불가
        if (currentWeapon == null || !CanFire())
        {
            if (currentAmmo <= 0 && !isReloading)
            {
                StartCoroutine(Reload());
            }
            return;
        }
        
        // 다음 발사 시간 설정
        nextFireTime = Time.time + (1f / currentWeapon.fireRate);

        // 총알 프리팹이 없으면 발사 불가
        if (currentWeapon.bulletPrefab == null)
        {
            Debug.LogError($"WeaponManager: {currentWeapon.weaponName}의 총알 프리팹이 할당되지 않았습니다!");
            return;
        }

        // 총알 발사 위치 계산 (플레이어 앞쪽)
        Vector3 spawnPosition = firePoint.position + new Vector3(direction.x * 0.2f, 0, 0);

        // 총알 생성 및 방향 설정
        GameObject bullet = Instantiate(currentWeapon.bulletPrefab, spawnPosition, Quaternion.identity);

        // 차징 상태에 따른 총알 크기 및 데미지 설정
        float damageMultiplier = 1f;
        float sizeMultiplier = 1f;
        bool isOverchargeShot = false;

        if (isCharging && currentWeapon.canCharge)
        {
            if (isOvercharged)
            {
                damageMultiplier = currentWeapon.overchargeDamageMultiplier;
                sizeMultiplier = currentWeapon.overchargeSizeMultiplier;
                isOverchargeShot = true;
            }
            else if (currentChargeTime > 0)
            {
                float chargePercent = currentChargeTime / currentWeapon.maxChargeTime;
                damageMultiplier = 1f + (currentWeapon.chargedDamageMultiplier - 1f) * chargePercent;
                sizeMultiplier = 1f + (currentWeapon.chargedSizeMultiplier - 1f) * chargePercent;
            }

            // 차징 상태 초기화
            ResetCharge();
        }

        // 총알의 크기 설정
        bullet.transform.localScale *= sizeMultiplier;

        // Bullet 컴포넌트가 있다면 데미지 설정
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
        {
            // 기본 무기 데미지에 차징 배율 적용
            bulletComponent.SetDamage(currentWeapon.damage * damageMultiplier);
            bulletComponent.SetIsOvercharge(isOverchargeShot);
        }

        // 총알 속도 설정
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.velocity = direction * currentWeapon.bulletSpeed;
        }
        else
        {
            Debug.LogWarning($"WeaponManager: {currentWeapon.weaponName}의 총알 프리팹에 Rigidbody2D 컴포넌트가 없습니다.");
        }

        // 총알 회전 설정
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 총알 소멸 처리
        Destroy(bullet, currentWeapon.bulletLifetime);

        // 탄약 감소
        currentAmmo--;

        // 탄약 소진 시 재장전
        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
        }
    }

    // 차징 시작 함수
    public void StartCharging()
    {
        if (currentWeapon == null) return;
        
        if (!isReloading && currentAmmo > 0 && !isCharging && currentWeapon.canCharge)
        {
            isCharging = true;
            currentChargeTime = 0f;
            isOvercharged = false;
        }
    }

    // 차징 업데이트 함수 (Update 메서드에서 호출)
    public void UpdateCharging(float deltaTime)
    {
        if (currentWeapon == null) return;
        
        if (isCharging && currentWeapon.canCharge)
        {
            currentChargeTime += deltaTime;
            
            // 오버차지 상태 확인
            if (currentChargeTime >= currentWeapon.overchargeThreshold && !isOvercharged)
            {
                isOvercharged = true;
                Debug.Log("오버차지 상태!");
            }
        }
    }

    // 차징 종료 및 발사 함수
    public void ReleaseCharge(Vector2 direction)
    {
        if (isCharging)
        {
            FireWeapon(direction);
        }
    }

    // 차징 상태 초기화
    private void ResetCharge()
    {
        isCharging = false;
        currentChargeTime = 0f;
        isOvercharged = false;
    }

    // 오버차지 공격이 적에게 명중 시 플레이어에게 데미지를 입히는 함수
    public void ApplyOverchargeDamageToPlayer()
    {
        if (currentWeapon == null) return;
        
        PlayerHP playerHP = FindObjectOfType<PlayerHP>();
        if (playerHP != null)
        {
            float damage = playerHP.MaxHP * (currentWeapon.overchargePlayerDamagePercent / 100f);
            playerHP.TakeDamage(damage);
            Debug.Log($"오버차지 반동 데미지: {damage}");
        }
    }

    private System.Collections.IEnumerator Reload()
    {
        if (currentWeapon == null) yield break;
        
        isReloading = true;
        Debug.Log("재장전 중...");

        yield return new WaitForSeconds(currentWeapon.reloadTime);

        currentAmmo = currentWeapon.maxAmmo;
        isReloading = false;
        Debug.Log("재장전 완료!");
    }
    
    // 현재 무기 정보 반환 메서드들
    public WeaponData GetCurrentWeapon() => currentWeapon;
    public bool CanWeaponCharge() => currentWeapon != null && currentWeapon.canCharge;
    public float GetCurrentChargePercent() => currentWeapon != null ? currentChargeTime / currentWeapon.maxChargeTime : 0f;
    public bool IsOvercharged() => isOvercharged;
}