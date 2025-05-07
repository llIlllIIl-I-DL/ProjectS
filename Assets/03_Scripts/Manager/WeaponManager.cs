using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // Action 사용을 위해 추가

public class WeaponManager : Singleton<WeaponManager>
{
    [Header("총알 설정")]
    [SerializeField] private Transform firePoint;           // 총알이 발사되는 위치
    [SerializeField] private float bulletSpeed = 15f; //읽기전용
    [SerializeField] private float bulletLifetime = 3f;
    [SerializeField] private Vector3 normalBulletScale = new Vector3(0.5f, 0.5f, 0.5f);  // 일반 총알 크기
    [SerializeField] private float fireRate = 0.3f;         // 발사 속도 (초)

    [Header("무기 상태")]
    public int currentAmmo = 30;
    public int maxAmmo = 30;
    public float reloadTime = 1.5f;
    public bool isReloading = false;
    private float nextFireTime = 0f;                        // 다음 발사 가능 시간

    [Header("스팀 압력 차징 설정")]
    [SerializeField] private float chargingLevel1Time = 1.0f; // 1단계 차지에 필요한 시간
    [SerializeField] private float chargingLevel2Time = 2.5f; // 2단계 차지에 필요한 시간
    [SerializeField] private float level1Damage = 2f;      // 1단계 차지샷 데미지
    [SerializeField] private float level2Damage = 5f;      // 2단계 차지샷 데미지
    [SerializeField] private Vector3 level1BulletScale = new Vector3(0.7f, 0.7f, 0.7f);  // 1단계 차지샷 크기
    [SerializeField] private Vector3 level2BulletScale = new Vector3(1.0f, 1.0f, 1.0f);  // 2단계 차지샷 크기

    [Header("스팀 압력 이펙트")]
    [SerializeField] private GameObject steamPressureEffectPrefab; // 압력 이펙트 프리팹
    [SerializeField] private AudioClip pressureBuildSound;      // 압력 증가 사운드
    [SerializeField] private AudioClip pressureReleaseSound;    // 압력 방출 사운드
    [SerializeField] private AudioClip steamHissSound;          // 증기 분출 사운드

    [Header("불릿 팩토리 설정")]
    [SerializeField] private BulletFactory bulletFactory;    // 불릿 팩토리 참조
    [SerializeField] private ElementType currentBulletType = ElementType.Normal; // 현재 총알 속성

    // 차징 상태 관리
    private bool isCharging = false;
    private float currentChargeTime = 0f;
    private int currentChargeLevel = 0;
    private SteamPressureEffect pressureEffect;
    private AudioSource audioSource;
    private Coroutine effectCoroutine; // 이펙트 코루틴 참조 저장

    private PlayerMovement playerMovement;

    // 탄약 변경 시 호출되는 이벤트 선언
    public event Action<int, int> OnAmmoChanged;

    private void Start()
    {
        // 총알 발사 위치가 설정되지 않은 경우 플레이어의 위치로 설정
        if (firePoint == null)
        {
            firePoint = transform;
            Debug.LogWarning("WeaponManager: firePoint가 설정되지 않아 플레이어 위치로 기본 설정됩니다.");
        }

        // 플레이어 컴포넌트 참조 가져오기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            audioSource = GetComponent<AudioSource>();

            // 오디오 소스가 없으면 추가
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.volume = 0.7f;
                audioSource.pitch = 1.0f;
            }
        }

        // BulletFactory를 찾아서 할당 (아직 할당되지 않은 경우)
        if (bulletFactory == null)
        {
            bulletFactory = FindObjectOfType<BulletFactory>();
            if (bulletFactory == null)
            {
                GameObject factoryObj = new GameObject("BulletFactory");
                bulletFactory = factoryObj.AddComponent<BulletFactory>();
                Debug.LogWarning("WeaponManager: BulletFactory가 씬에 없어 새로 생성합니다.");
            }
        }

        // 스팀 압력 이펙트 생성
        CreatePressureEffect();
    }

    private void Update()
    {
        // 차징 시간 누적 및 이펙트 처리
        if (isCharging && !isReloading && currentAmmo > 0)
        {
            UpdateCharging();
        }
    }

    private void CreatePressureEffect()
    {
        if (steamPressureEffectPrefab != null)
        {
            // 플레이어 객체 찾기
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("WeaponManager: 플레이어를 찾을 수 없습니다.");
                return;
            }

            // 이펙트를 플레이어의 자식으로 생성
            GameObject effectObj = Instantiate(steamPressureEffectPrefab, player.transform.position, Quaternion.identity);
            effectObj.transform.SetParent(player.transform); // 플레이어의 자식으로 설정
            effectObj.SetActive(false); // 초기에는 비활성화 상태로 설정
            
            pressureEffect = effectObj.GetComponent<SteamPressureEffect>();

            // 압력 이펙트에 사운드 설정
            if (pressureEffect != null)
            {
                // 스크립트에 접근하여 사운드 클립 설정
                var effectAudio = effectObj.GetComponent<AudioSource>();
                if (effectAudio != null)
                {
                    // 사운드 클립 전달
                    effectAudio.clip = pressureBuildSound;
                }
            }
        }
        else
        {
            Debug.LogWarning("WeaponManager: steamPressureEffectPrefab이 할당되지 않아 압력 이펙트가 없습니다.");
        }
    }

    private void UpdateCharging()
    {
        float previousChargeTime = currentChargeTime;
        currentChargeTime += Time.deltaTime;

        // 이전 레벨과 현재 레벨 확인하여 변화가 있으면 이펙트 업데이트
        int previousLevel = currentChargeLevel;

        // 차징 레벨 결정
        if (currentChargeTime >= chargingLevel2Time)
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

        // 압력 이펙트 업데이트
        if (pressureEffect != null)
        {
            // 현재 충전 시간에 비례하여 압력 설정 (0-1 사이 값)
            float pressure = Mathf.Clamp01(currentChargeTime / chargingLevel2Time);
            pressureEffect.SetPressure(pressure);
        }

        // 레벨이 변경되었을 때 사운드 재생
        if (previousLevel != currentChargeLevel)
        {
            if (currentChargeLevel == 1)
            {
                Debug.Log("1단계 압력 충전 완료!");
                if (audioSource != null && pressureBuildSound != null)
                {
                    audioSource.pitch = 1.0f;
                    audioSource.PlayOneShot(pressureBuildSound, 0.5f);
                }
            }
            else if (currentChargeLevel == 2)
            {
                Debug.Log("2단계 압력 충전 완료!");
                if (audioSource != null && pressureBuildSound != null)
                {
                    audioSource.pitch = 1.2f;
                    audioSource.PlayOneShot(pressureBuildSound, 0.7f);
                }
            }
        }
    }

    public void StartCharging()
    {
        // 재장전 중이거나 탄약이 없으면 차징 불가
        if (isReloading || currentAmmo <= 0) return;

        isCharging = true;
        currentChargeTime = 0f;
        currentChargeLevel = 0;

        // 압력 이펙트 활성화
        if (pressureEffect != null)
        {
            // 진행 중인 이펙트 코루틴이 있다면 중지
            if (effectCoroutine != null)
            {
                StopCoroutine(effectCoroutine);
                effectCoroutine = null;
            }
            
            pressureEffect.gameObject.SetActive(true); // 이펙트 활성화
            pressureEffect.SetPressure(0f);
        }

        Debug.Log("압력 충전 시작");
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
        FireSteamPressureBullet();

        // 차징 상태 초기화
        isCharging = false;
        currentChargeTime = 0f;
        currentChargeLevel = 0;

        // 압력 방출 효과를 코루틴으로 처리
        // 이전 코루틴이 있다면 중지하고 새로운 코루틴 시작
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }
        effectCoroutine = StartCoroutine(ReleasePressureEffect());
    }

    // 압력 방출 효과를 처리하는 코루틴
    private IEnumerator ReleasePressureEffect()
    {
        if (pressureEffect != null && pressureEffect.gameObject.activeSelf)
        {
            // 압력 게이지를 0으로 설정하고 방출 효과 준비
            pressureEffect.SetPressure(0f);
            
            // 증기 파티클 방출 효과를 직접 처리
            StartCoroutine(BurstSteamParticles());
            
            // 압력 방출 사운드를 재생하기 위해 약간의 지연
            yield return new WaitForSeconds(0.1f);
            
            // 압력 방출 애니메이션 완료를 위한 대기 시간
            yield return new WaitForSeconds(1.5f);
            
            // 차징 중이 아닐 때만 이펙트 비활성화
            if (!isCharging)
            {
                pressureEffect.gameObject.SetActive(false);
            }
        }
        
        effectCoroutine = null;
    }

    // 압력 방출 시 증기 폭발 효과
    private IEnumerator BurstSteamParticles()
    {
        if (pressureEffect != null && pressureEffect.gameObject.activeSelf)
        {
            int burstAmount = 20; // 기본 증기 파티클 수
            float currentPressure = pressureEffect.GetComponent<SteamPressureEffect>().GetCurrentPressure();
            burstAmount = Mathf.RoundToInt(burstAmount * (1f + currentPressure));

            // 압력 방출 사운드 재생
            if (audioSource != null && pressureReleaseSound != null && currentPressure > 0.3f)
            {
                audioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
                audioSource.PlayOneShot(pressureReleaseSound, Mathf.Min(1.0f, currentPressure));
            }

            for (int i = 0; i < burstAmount; i++)
            {
                pressureEffect.EmitSteamParticleExternal();
                yield return new WaitForSeconds(0.03f);
            }
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

        // 발사 쿨다운 체크
        if (Time.time < nextFireTime)
        {
            return; // 아직 발사할 수 없음
        }

        // 다음 발사 시간 설정
        nextFireTime = Time.time + fireRate;

        Vector2 direction = GetAimDirection();
        Vector3 spawnPosition = firePoint.position + new Vector3(direction.x * 0.2f, 0, 0);

        // BulletFactory를 사용하여 총알 생성
        GameObject bullet = bulletFactory.CreateBullet(currentBulletType, spawnPosition, Quaternion.identity);
        
        // 총알 크기 설정
        bullet.transform.localScale = normalBulletScale;

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
        // 탄약 변경 이벤트 호출
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        // 탄약 소진 시 재장전
        if (currentAmmo <= 0)
        {
            StartCoroutine(Reload());
        }
    }

    private void FireSteamPressureBullet()
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

        // 발사 쿨다운 체크 - 차징샷은 쿨다운 없이 항상 발사 가능
        // 대신 차징 후에는 다음 발사 시간을 설정하여 연속 발사 방지
        nextFireTime = Time.time + fireRate;

        // 압력 차징 레벨에 따른 총알 타입 결정
        Vector3 scale;
        Color bulletColor = Color.white;
        bool isOvercharged = false; // 2단계에서만 오버차지 상태 설정

        if (currentChargeLevel == 2)
        {
            // 2단계 차징샷 (강력한 증기압) - 오버차지 상태
            scale = level2BulletScale;
            bulletColor = new Color(1.0f, 0.5f, 0.1f, 1.0f); // 주황색 (고온 증기)
            isOvercharged = true; // 최대 압력일 때 오버차지 상태로 설정
            Debug.Log("최대 증기압 발사!");

            // 최대 압력 방출 사운드
            if (audioSource != null && pressureReleaseSound != null)
            {
                audioSource.pitch = 0.8f;
                audioSource.PlayOneShot(pressureReleaseSound, 1.0f);
            }
        }
        else if (currentChargeLevel == 1)
        {
            // 1단계 차징샷 (중간 증기압)
            scale = level1BulletScale;
            bulletColor = new Color(0.7f, 0.7f, 0.7f, 1.0f); // 회색 (일반 증기)
            Debug.Log("중간 증기압 발사!");

            // 중간 압력 방출 사운드
            if (audioSource != null && pressureReleaseSound != null)
            {
                audioSource.pitch = 1.0f;
                audioSource.PlayOneShot(pressureReleaseSound, 0.7f);
            }
        }
        else
        {
            // 차징이 충분하지 않으면 일반 총알
            scale = normalBulletScale;
            bulletColor = Color.white;
            Debug.Log("일반 공격 발사!");

            // 일반 발사 사운드
            if (audioSource != null && steamHissSound != null)
            {
                audioSource.pitch = 1.2f;
                audioSource.PlayOneShot(steamHissSound, 0.5f);
            }
        }

        Vector2 direction = GetAimDirection();
        Vector3 spawnPosition = firePoint.position + new Vector3(direction.x * 0.2f, 0, 0);

        // BulletFactory를 사용하여 총알 생성 (오버차지 여부 전달)
        GameObject bullet = bulletFactory.CreateBullet(currentBulletType, spawnPosition, Quaternion.identity, isOvercharged);
        
        // 총알 크기 설정
        bullet.transform.localScale = scale;

        // 총알 색상 설정 (증기 색상)
        SpriteRenderer bulletRenderer = bullet.GetComponent<SpriteRenderer>();
        if (bulletRenderer != null)
        {
            bulletRenderer.color = bulletColor;
        }

        // 총알 속도 설정 (압력에 따라 더 빠름)
        float finalBulletSpeed = bulletSpeed * (1f + currentChargeLevel * 0.3f);

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.velocity = direction * finalBulletSpeed;
        }

        // 총알 회전 설정
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 증기 파티클 효과 추가 (총알에 트레일 효과)
        if (currentChargeLevel > 0)
        {
            TrailRenderer trail = bullet.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = bullet.AddComponent<TrailRenderer>();
                trail.startWidth = 0.1f * scale.x;
                trail.endWidth = 0.01f;
                trail.time = 0.1f;

                // 증기 트레일 색상 설정
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(bulletColor, 0.0f),
                        new GradientColorKey(new Color(0.8f, 0.8f, 0.8f, 1f), 1.0f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0.7f, 0.0f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
                trail.colorGradient = gradient;
            }
        }

        // 총알 소멸 처리
        Destroy(bullet, bulletLifetime);

        // 탄약 감소
        currentAmmo--;
        // 탄약 변경 이벤트 호출
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
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

            // 압력 이펙트 비활성화
            if (pressureEffect != null)
            {
                pressureEffect.SetPressure(0f);
                
                // 이펙트 코루틴 중지 및 이펙트 비활성화
                if (effectCoroutine != null)
                {
                    StopCoroutine(effectCoroutine);
                    effectCoroutine = null;
                }
                pressureEffect.gameObject.SetActive(false);
            }
        }

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
        Debug.Log("재장전 완료!");
        // 탄약 변경 이벤트 호출
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
    }

    // 현재 사용 중인 총알 속성 설정
    public void SetBulletType(ElementType type)
    {
        currentBulletType = type;
        Debug.Log($"무기 속성이 {type}으로 변경되었습니다.");
    }

    // 총알 속성 가져오기
    public ElementType GetBulletType()
    {
        return currentBulletType;
    }
}