using System.Collections;
using UnityEngine;

public class WeaponManager : Singleton<WeaponManager>
{
    [Header("총알 설정")]
    [SerializeField] private GameObject bulletPrefab;       // 일반 총알
    [SerializeField] private Transform firePoint;           // 총알이 발사되는 위치
    [SerializeField] private float bulletSpeed = 15f;
    [SerializeField] private float bulletLifetime = 3f;

    [Header("무기 상태")]
    public int currentAmmo = 30;
    public int maxAmmo = 30;
    public float reloadTime = 1.5f;
    public bool isReloading = false;

    [Header("차징 공격 설정")]
    [SerializeField] private GameObject level1BulletPrefab; // 1단계 차지샷 (중간 차지)
    [SerializeField] private GameObject level2BulletPrefab; // 2단계 차지샷 (최대 차지)
    [SerializeField] private GameObject overchargeBulletPrefab; // 오버차지샷 (과열)
    [SerializeField] private float chargingLevel1Time = 1.0f; // 1단계 차지에 필요한 시간
    [SerializeField] private float chargingLevel2Time = 2.5f; // 2단계 차지에 필요한 시간
    [SerializeField] private float overchargeTime = 4.0f;    // 오버차지에 필요한 시간
    [SerializeField] private float level1Damage = 15f;      // 1단계 차지샷 데미지
    [SerializeField] private float level2Damage = 30f;      // 2단계 차지샷 데미지

    [Header("차징 이펙트")]
    [SerializeField] private GameObject chargingLevel1Effect;    // 1단계 차징 이펙트
    [SerializeField] private GameObject chargingLevel2Effect;    // 2단계 차징 이펙트
    [SerializeField] private GameObject overchargeEffect;        // 오버차지 이펙트
    [SerializeField] private AudioClip chargingStartSound;       // 차징 시작 소리
    [SerializeField] private AudioClip chargingLevel1Sound;      // 1단계 차징 도달 소리
    [SerializeField] private AudioClip chargingLevel2Sound;      // 2단계 차징 도달 소리
    [SerializeField] private AudioClip overchargeSound;          // 오버차지 도달 소리
    [SerializeField] private AudioClip chargingShotSound;        // 차징샷 발사 소리

    // 차징 상태 관리
    private bool isCharging = false;
    private float currentChargeTime = 0f;
    private int currentChargeLevel = 0;
    private GameObject activeChargingEffect = null;
    private AudioSource audioSource;

    private PlayerMovement playerMovement;

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

        // 차징 공격 프리팹 검사 및 대체
        if (level1BulletPrefab == null)
        {
            level1BulletPrefab = bulletPrefab;
            Debug.LogWarning("WeaponManager: level1BulletPrefab이 할당되지 않아 일반 총알로 대체됩니다.");
        }

        if (level2BulletPrefab == null)
        {
            level2BulletPrefab = bulletPrefab;
            Debug.LogWarning("WeaponManager: level2BulletPrefab이 할당되지 않아 일반 총알로 대체됩니다.");
        }
        
        if (overchargeBulletPrefab == null)
        {
            overchargeBulletPrefab = level2BulletPrefab;
            Debug.LogWarning("WeaponManager: overchargeBulletPrefab이 할당되지 않아 2단계 차지샷으로 대체됩니다.");
        }

        // 플레이어 컴포넌트 참조 가져오기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            audioSource = player.GetComponent<AudioSource>();

            // 오디오 소스가 없으면 추가
            if (audioSource == null && (chargingStartSound != null || chargingLevel1Sound != null || chargingLevel2Sound != null || overchargeSound != null))
            {
                audioSource = player.AddComponent<AudioSource>();
                audioSource.volume = 0.7f;
                audioSource.pitch = 1.0f;
            }
        }
    }

    private void Update()
    {
        // 차징 시간 누적 및 이펙트 처리
        if (isCharging && !isReloading && currentAmmo > 0)
        {
            UpdateCharging();
        }
    }

    private void UpdateCharging()
    {
        float previousChargeTime = currentChargeTime;
        currentChargeTime += Time.deltaTime;

        // 이전 레벨과 현재 레벨 확인하여 변화가 있으면 이펙트 업데이트
        int previousLevel = currentChargeLevel;

        // 차징 레벨 결정
        if (currentChargeTime >= overchargeTime)
        {
            currentChargeLevel = 3; // 오버차지 레벨
        }
        else if (currentChargeTime >= chargingLevel2Time)
        {
            currentChargeLevel = 2;
        }
        else if (currentChargeTime >= chargingLevel1Time)
        {
            currentChargeLevel = 1;
        }
        else
        {
            currentChargeLevel = 0;
        }

        // 레벨이 변경되었을 때 이펙트 업데이트
        if (previousLevel != currentChargeLevel)
        {
            StartCoroutine(UpdateChargingEffectsCoroutine(previousLevel, currentChargeLevel));
        }
    }

    // 코루틴을 사용하여 이펙트 전환 시 시간차를 둠
    private IEnumerator UpdateChargingEffectsCoroutine(int previousLevel, int newLevel)
    {
        // 이전 이펙트 제거를 안전하게 처리
        if (activeChargingEffect != null)
        {
            // 이전 이펙트를 제거하기 전에 부모 연결 해제 (Transform 이슈 방지)
            activeChargingEffect.transform.SetParent(null);
            
            // 렌더러 비활성화로 시각적으로 즉시 사라지게 함
            SpriteRenderer[] renderers = activeChargingEffect.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
            
            // 이전 이펙트 제거
            Destroy(activeChargingEffect);
            activeChargingEffect = null;
            
            // 이펙트 전환 사이에 약간의 딜레이 추가
            yield return new WaitForSeconds(0.05f);
        }

        // 안전한 스폰 위치 설정 (Z값은 항상 0으로 고정)
        Vector3 safeSpawnPosition = firePoint.position;
        safeSpawnPosition.z = 0f;

        // 새 이펙트 생성 및 사운드 재생
        GameObject effectPrefab = null;
        AudioClip soundToPlay = null;
        string logMessage = "";

        if (newLevel == 1)
        {
            effectPrefab = chargingLevel1Effect;
            soundToPlay = chargingLevel1Sound;
            logMessage = "1단계 차징 완료!";
        }
        else if (newLevel == 2)
        {
            effectPrefab = chargingLevel2Effect;
            soundToPlay = chargingLevel2Sound;
            logMessage = "2단계 차징 완료!";
        }
        else if (newLevel == 3)
        {
            effectPrefab = overchargeEffect;
            soundToPlay = overchargeSound;
            logMessage = "오버차지 완료! 반동 데미지 주의!";
        }

        // 이펙트 프리팹이 있는 경우에만 생성
        if (effectPrefab != null)
        {
            try
            {
                // 이펙트 생성
                activeChargingEffect = Instantiate(effectPrefab, safeSpawnPosition, Quaternion.identity);
                
                // 바로 부모를 설정하지 않고 한 프레임 기다림
                yield return null;
                
                // 이제 부모 설정 (부모-자식 관계로 인한 Transform 문제 방지)
                if (activeChargingEffect != null)
                {
                    activeChargingEffect.transform.SetParent(firePoint);
                    
                    // Z 위치를 명시적으로 0으로 설정 (정렬 문제 방지)
                    Vector3 localPos = activeChargingEffect.transform.localPosition;
                    localPos.z = 0f;
                    activeChargingEffect.transform.localPosition = localPos;
                    
                    // 모든 자식 오브젝트의 Z 위치도 0으로 설정
                    foreach (Transform child in activeChargingEffect.transform)
                    {
                        if (child != null)
                        {
                            Vector3 childLocalPos = child.localPosition;
                            childLocalPos.z = 0f;
                            child.localPosition = childLocalPos;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"이펙트 생성 중 오류 발생: {e.Message}");
                if (activeChargingEffect != null)
                {
                    Destroy(activeChargingEffect);
                    activeChargingEffect = null;
                }
            }
        }

        // 사운드 재생
        if (audioSource != null && soundToPlay != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }

        Debug.Log(logMessage);
    }

    public void StartCharging()
    {
        // 재장전 중이거나 탄약이 없으면 차징 불가
        if (isReloading || currentAmmo <= 0) return;

        isCharging = true;
        currentChargeTime = 0f;
        currentChargeLevel = 0;

        // 이미 활성화된 이펙트가 있다면 제거
        if (activeChargingEffect != null)
        {
            Destroy(activeChargingEffect);
            activeChargingEffect = null;
        }

        // 차징 시작 사운드 재생
        if (audioSource != null && chargingStartSound != null)
        {
            audioSource.PlayOneShot(chargingStartSound);
        }

        Debug.Log("차징 시작");
    }

    public void StopCharging()
    {
        // 차징 중이 아니었으면 일반 공격으로 처리
        if (!isCharging)
        {
            FireNormalBullet();
            return;
        }

        // 차징 상태였다면 차징 레벨에 따른 공격 발사
        FireChargedBullet();

        // 차징 상태 및 이펙트 초기화
        isCharging = false;
        currentChargeTime = 0f;
        currentChargeLevel = 0;

        // 이펙트 제거 - 안전하게 처리
        if (activeChargingEffect != null)
        {
            // 부모 연결 해제
            activeChargingEffect.transform.SetParent(null);
            
            // 렌더러 비활성화
            SpriteRenderer[] renderers = activeChargingEffect.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
            
            Destroy(activeChargingEffect);
            activeChargingEffect = null;
        }
    }

    private Vector2 GetAimDirection()
    {
        // 플레이어 참조 얻기 (캐싱된 참조 사용 가능하면 활용)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 플레이어의 로컬 스케일 또는 스프라이트 방향 확인
            SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // 스프라이트가 X축으로 플립되었는지 확인
                if (spriteRenderer.flipX)
                {
                    Debug.Log("플레이어가 왼쪽을 바라보고 있습니다. 왼쪽으로 발사합니다.");
                    return Vector2.left;
                }
                else
                {
                    Debug.Log("플레이어가 오른쪽을 바라보고 있습니다. 오른쪽으로 발사합니다.");
                    return Vector2.right;
                }
            }

            // 스프라이트 렌더러가 없는 경우 Transform 스케일로 확인
            else if (player.transform.localScale.x < 0)
            {
                Debug.Log("플레이어 스케일이 음수입니다. 왼쪽으로 발사합니다.");
                return Vector2.left;
            }
            else
            {
                Debug.Log("플레이어 스케일이 양수입니다. 오른쪽으로 발사합니다.");
                return Vector2.right;
            }
        }

        // 플레이어가 없는 경우 기본값 반환
        Debug.LogWarning("플레이어를 찾을 수 없습니다. 기본 방향(오른쪽)으로 발사합니다.");
        return Vector2.right;
    }

    public void FireNormalBullet()
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

        Vector2 direction = GetAimDirection();
        
        // 총알 스폰 위치를 플레이어 콜라이더 밖으로 더 멀리 설정 (0.2f에서 0.5f로 증가)
        Vector3 spawnPosition = firePoint.position + new Vector3(direction.x * 0.5f, 0, 0);
        
        Debug.Log($"총알 발사 - 방향: {direction}, 생성 위치: {spawnPosition}");

        // 일반 총알 생성
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        
        // 기본 총알 크기 약간 키우기 (기존 크기가 너무 작았음)
        bullet.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

        // 총알 콜라이더 크기 로깅
        Collider2D bulletCollider = bullet.GetComponent<Collider2D>();
        if (bulletCollider != null)
        {
            if (bulletCollider is CircleCollider2D)
            {
                CircleCollider2D circleCollider = bulletCollider as CircleCollider2D;
                Debug.Log($"총알 원형 콜라이더 - 반지름: {circleCollider.radius}, 오프셋: {circleCollider.offset}");
            }
            else if (bulletCollider is BoxCollider2D)
            {
                BoxCollider2D boxCollider = bulletCollider as BoxCollider2D;
                Debug.Log($"총알 박스 콜라이더 - 크기: {boxCollider.size}, 오프셋: {boxCollider.offset}");
            }
            else if (bulletCollider is CapsuleCollider2D)
            {
                CapsuleCollider2D capsuleCollider = bulletCollider as CapsuleCollider2D;
                Debug.Log($"총알 캡슐 콜라이더 - 크기: {capsuleCollider.size}, 오프셋: {capsuleCollider.offset}");
            }
        }

        // 총알 속도 설정
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.velocity = direction * bulletSpeed;
            Debug.Log($"총알 속도 설정: {bulletRb.velocity}, 속력: {bulletSpeed}");
        }
        else
        {
            Debug.LogWarning("WeaponManager: 총알 프리팹에 Rigidbody2D 컴포넌트가 없습니다.");
        }

        // 총알 회전 설정
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 총알 데미지 설정 (기본 10)
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
        {
            bulletComponent.damage = 10f;
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

        // 차징 레벨에 따른 총알 타입 결정
        GameObject bulletToSpawn;
        float damage;
        Vector3 scale = Vector3.one;
        bool isOvercharged = false;

        if (currentChargeLevel == 3)
        {
            // 오버차지샷 (과열)
            bulletToSpawn = overchargeBulletPrefab;
            damage = level2Damage * 3f; // 기본 데미지의 3배
            scale = new Vector3(1.0f, 1.0f, 1.0f); // 크기 조정 (이전 2.0f에서 더 작게)
            isOvercharged = true;
            Debug.Log("과열 상태 차징샷 발사! 주의: 적중 시 반동 데미지!");
        }
        else if (currentChargeLevel == 2)
        {
            // 2단계 차징샷
            bulletToSpawn = level2BulletPrefab;
            damage = level2Damage;
            scale = new Vector3(0.7f, 0.7f, 0.7f); // 크기 조정 (이전 1.5f에서 더 작게)
            Debug.Log("2단계 차징샷 발사!");
        }
        else if (currentChargeLevel == 1)
        {
            // 1단계 차징샷
            bulletToSpawn = level1BulletPrefab;
            damage = level1Damage;
            scale = new Vector3(0.4f, 0.4f, 0.4f); // 크기 조정 (이전 1.2f에서 더 작게)
            Debug.Log("1단계 차징샷 발사!");
        }
        else
        {
            // 차징이 충분하지 않으면 일반 총알
            bulletToSpawn = bulletPrefab;
            damage = 10f;
            scale = new Vector3(0.25f, 0.25f, 0.25f); // 기본 총알과 동일한 크기
            Debug.Log("일반 공격 발사!");
        }

        // 차징샷 사운드 재생
        if (audioSource != null && chargingShotSound != null && currentChargeLevel > 0)
        {
            audioSource.PlayOneShot(chargingShotSound);
        }

        Vector2 direction = GetAimDirection();
        
        // 총알 스폰 위치를 플레이어 콜라이더 밖으로 더 멀리 설정 (0.2f에서 0.5f로 증가)
        Vector3 spawnPosition = firePoint.position + new Vector3(direction.x * 0.5f, 0, 0);
        
        Debug.Log($"차징 총알 발사 - 레벨: {currentChargeLevel}, 방향: {direction}, 생성 위치: {spawnPosition}, 크기: {scale}");

        // 총알 생성 및 설정
        GameObject bullet = Instantiate(bulletToSpawn, spawnPosition, Quaternion.identity);
        bullet.transform.localScale = scale;

        // 총알 속도 설정 (차징샷은 약간 더 빠름)
        float finalBulletSpeed = bulletSpeed * (1f + currentChargeLevel * 0.2f);

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.velocity = direction * finalBulletSpeed;
            Debug.Log($"차징 총알 속도 설정: {bulletRb.velocity}, 속력: {finalBulletSpeed}");
        }

        // 총알 회전 설정
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 총알 데미지 설정
        Bullet bulletComponent = bullet.GetComponent<Bullet>();
        if (bulletComponent != null)
        {
            bulletComponent.damage = damage;
            
            // 과열 상태 설정
            bulletComponent.isOvercharged = isOvercharged;
        }

        // 총알 소멸 처리
        Destroy(bullet, bulletLifetime);

        // 탄약 감소 (차징샷도 한 발만 소모)
        currentAmmo--;

        // 탄약 소진 시 재장전
        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("재장전 중...");

        // 차징 상태 초기화
        if (isCharging)
        {
            isCharging = false;
            currentChargeTime = 0f;
            currentChargeLevel = 0;

            // 이펙트 제거 - 안전하게 처리
            if (activeChargingEffect != null)
            {
                // 부모 연결 해제
                activeChargingEffect.transform.SetParent(null);
                
                // 렌더러 비활성화
                SpriteRenderer[] renderers = activeChargingEffect.GetComponentsInChildren<SpriteRenderer>();
                foreach (SpriteRenderer renderer in renderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                    }
                }
                
                Destroy(activeChargingEffect);
                activeChargingEffect = null;
            }
        }

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
        Debug.Log("재장전 완료!");
    }
}