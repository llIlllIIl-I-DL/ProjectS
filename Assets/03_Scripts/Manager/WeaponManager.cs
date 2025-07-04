using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class WeaponManager : Singleton<WeaponManager>
{
    [Header("무기 구성 요소")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private BulletFactory bulletFactory;
    [SerializeField] private AmmoManager ammoManager;
    [SerializeField] private ChargeManager chargeManager;
    [SerializeField] private EffectManager effectManager;

    [Header("총알 설정")]
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float bulletLifetime = 2f;
    [SerializeField] private float bulletDamage = 1f;
    [SerializeField] private int ammo = 30;
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private Vector3 normalBulletScale = new Vector3(0.5f, 0.5f, 0.5f);
    [SerializeField] private Vector3 level1BulletScale = new Vector3(0.7f, 0.7f, 0.7f);
    [SerializeField] private Vector3 level2BulletScale = new Vector3(1.0f, 1.0f, 1.0f);
    
    [Header("쿨다운 설정")]
    [SerializeField] private float level0ChargeCooldown = 0.3f; // 차징 없는 차지샷 쿨다운
    [SerializeField] private float level1ChargeCooldown = 0.3f; // 1단계 차지샷 쿨다운
    [SerializeField] private float level2ChargeCooldown = 0.5f; // 2단계 차지샷 쿨다운

    [Header("유틸리티 효과")]
    private float atkUpPercent = 0f;
    private float speedUpPercent = 0f;

    // 발사 관련 변수
    private float nextFireTime = 0f;
    private ElementType currentBulletType = ElementType.Normal;
    private PlayerMovement playerMovement;
    private CollisionDetector collisionDetector;

    private bool isWallSliding = false;
    private int wallDirection = 1; // -1: 왼쪽, 1: 오른쪽

    private void Awake()
    {
        base.Awake();
        if (Instance == this)
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            InitializeComponents();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 로드될 때마다 firePoint 찾기
        FindFirePoint();
        
        // 이펙트 매니저 재초기화
        if (effectManager != null)
        {
            effectManager.Initialize();
        }
        
        // 이벤트 리스너 재등록
        RegisterEventListeners();
    }

    private void FindFirePoint()
    {
        // Player 태그를 가진 오브젝트 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Player의 자식 중에서 "FirePoint" 이름을 가진 Transform 찾기
            Transform foundFirePoint = player.transform.Find("FirePoint");
            if (foundFirePoint != null)
            {
                firePoint = foundFirePoint;
                Debug.Log("FirePoint 참조가 업데이트되었습니다.");
            }
            else
            {
                Debug.LogWarning("Player 오브젝트에서 FirePoint를 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogWarning("씬에서 Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // 이벤트 구독 해제
        if (ammoManager != null)
        {
            ammoManager.OnAmmoChanged -= HandleAmmoChanged;
        }
        
        if (chargeManager != null)
        {
            chargeManager.OnChargeLevelChanged -= effectManager.PlayChargeLevelSound;
            chargeManager.OnChargePressureChanged -= effectManager.UpdatePressureEffect;
        }
    }

    private void HandleAmmoChanged(int current, int max)
    {
        if (PlayerUI.Instance != null)
        {
            PlayerUI.Instance.UpdateAmmoUI(current, max);
            if (ammoManager != null)
            {
                ammoManager.OnAmmoChanged -= (current, max) =>
                {
                    if (PlayerUI.Instance != null)
                    {
                        PlayerUI.Instance.UpdateAmmoUI(current, max);
                    }
                };
            }
        }
    }

    private void Start()
    {
        if (Instance == this)
        {
            RegisterEventListeners();
        }
    }

    private void Update()
    {
        // 차징 시간 누적 및 이펙트 처리
        if (chargeManager.IsCharging && !ammoManager.IsReloading && ammoManager.CurrentAmmo > 0)
        {
            chargeManager.UpdateCharging();
        }
        
        // 쿨다운 디버깅
        if (Time.time < nextFireTime)
        {
            float remainingCooldown = nextFireTime - Time.time;
            if (remainingCooldown > 0.01f) // 작은 값은 무시
            {
                Debug.Log($"발사 쿨다운 중: {remainingCooldown:F2}초 남음");
            }
        }
    }

    // 컴포넌트 초기화
    private void InitializeComponents()
    {
        // 총알 발사 위치가 설정되지 않은 경우 플레이어의 위치로 설정
        if (firePoint == null)
        {
            firePoint = transform;
            Debug.LogWarning("WeaponManager: firePoint가 설정되지 않아 플레이어 위치로 기본 설정됩니다.");
        }

        // 필요한 컴포넌트들 찾거나 생성
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // PlayerMovement 컴포넌트 찾기 (직접 또는 부모에서)
            playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                playerMovement = player.GetComponentInParent<PlayerMovement>();
                if (playerMovement == null)
                {
                    Debug.LogWarning("WeaponManager: PlayerMovement 컴포넌트를 찾을 수 없습니다.");
                }
                else
                {
                    Debug.Log("WeaponManager: PlayerMovement 컴포넌트를 부모 오브젝트에서 찾았습니다.");
                }
            }
            else
            {
                Debug.Log("WeaponManager: PlayerMovement 컴포넌트를 찾았습니다.");
            }
            
            // CollisionDetector 컴포넌트 찾기 (직접 또는 부모에서)
            collisionDetector = player.GetComponent<CollisionDetector>();
            if (collisionDetector == null)
            {
                collisionDetector = player.GetComponentInParent<CollisionDetector>();
                if (collisionDetector == null)
                {
                    Debug.LogWarning("WeaponManager: CollisionDetector 컴포넌트를 찾을 수 없습니다.");
                }
                else
                {
                    Debug.Log("WeaponManager: CollisionDetector 컴포넌트를 부모 오브젝트에서 찾았습니다.");
                }
            }
            else
            {
                Debug.Log("WeaponManager: CollisionDetector 컴포넌트를 찾았습니다.");
            }
        }

        // 매니저 컴포넌트들 초기화 (인스펙터에서 할당되지 않은 경우)
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

        if (ammoManager == null)
        {
            ammoManager = gameObject.GetComponent<AmmoManager>();
            if (ammoManager == null)
            {
                ammoManager = gameObject.AddComponent<AmmoManager>();
            }
        }

        if (chargeManager == null)
        {
            chargeManager = gameObject.GetComponent<ChargeManager>();
            if (chargeManager == null)
            {
                chargeManager = gameObject.AddComponent<ChargeManager>();
            }
        }

        if (effectManager == null)
        {
            effectManager = gameObject.GetComponent<EffectManager>();
            if (effectManager == null)
            {
                effectManager = gameObject.AddComponent<EffectManager>();
            }
        }
    }

    // 이벤트 리스너 등록
    private void RegisterEventListeners()
    {
        // 차징 관련 이벤트 등록
        if (chargeManager != null && effectManager != null)
        {
            chargeManager.OnChargeLevelChanged -= effectManager.PlayChargeLevelSound;
            chargeManager.OnChargePressureChanged -= effectManager.UpdatePressureEffect;
            
            chargeManager.OnChargeLevelChanged += effectManager.PlayChargeLevelSound;
            chargeManager.OnChargePressureChanged += effectManager.UpdatePressureEffect;
        }

        // 탄약 관련 이벤트 전달
        ammoManager.OnAmmoChanged += (current, max) => OnAmmoChanged?.Invoke(current, max);
        if (ammoManager != null)
        {
            ammoManager.OnAmmoChanged -= HandleAmmoChanged;
            ammoManager.OnAmmoChanged += HandleAmmoChanged;
        }
    }

    // 일반 총알 발사
    public void FireNormalBullet()
    {
        Debug.Log("FireNormalBullet 호출됨");

        // 매니저 참조 체크
        if (!IsValid())
        {
            Debug.LogWarning("WeaponManager: 필요한 참조가 없어 발사를 할 수 없습니다.");
            return;
        }

        // 재장전 중이거나 탄약 없으면 발사 불가
        if (ammoManager.IsReloading || ammoManager.CurrentAmmo <= 0)
        {
            if (ammoManager.CurrentAmmo <= 0 && !ammoManager.IsReloading)
            {
                Debug.Log("탄약 없음 - 재장전 시작");
                ammoManager.StartReload();
            }
            else
            {
                Debug.Log("발사 불가: 재장전 중이거나 탄약 없음");
            }
            return;
        }

        // 발사 쿨다운 체크
        float currentCooldown = nextFireTime - Time.time;
        if (currentCooldown > 0)
        {
            Debug.Log($"아직 발사할 수 없음: 쿨다운 {currentCooldown:F2}초 남음");
            return;
        }

        // 다음 발사 시간 설정
        nextFireTime = Time.time + level0ChargeCooldown;
        Debug.Log($"일반 공격 발사: 쿨다운 {level0ChargeCooldown:F2}초 적용");

        // 총알 생성 및 발사
        FireBullet(false);

        // 발사 사운드
        effectManager.PlayFireSound();

        // 탄약 감소
        ammoManager.UseAmmo();
    }

    // 차징 시작
    public void StartCharging()
    {
        // 재장전 중이거나 탄약이 없으면 차징 불가
        if (ammoManager.IsReloading || ammoManager.CurrentAmmo <= 0)
        {
            Debug.Log("차징 불가: 재장전 중이거나 탄약 없음");
            return;
        }
        
        // 현재 쿨다운 상태 확인
        float currentCooldown = nextFireTime - Time.time;
        if (currentCooldown > 0)
        {
            Debug.Log($"차징 시작 시 쿨다운 초기화: {currentCooldown:F2}초 남음");
        }
        
        // 쿨다운 초기화 (연타 후 차징 가능하도록)
        //nextFireTime = 0f;

        // 차징 시작하고 압력 이펙트 즉시 활성화
        chargeManager.StartCharging();
        effectManager.UpdatePressureEffect(0.01f); // 최소값으로 설정하여 즉시 활성화
        
        Debug.Log("차징 시작됨");
    }

    // 차징 중단 (발사)
    public void StopCharging()
    {
        // 매니저 참조 체크
        if (!IsValid())
        {
            Debug.LogWarning("WeaponManager: 필요한 참조가 없어 차징을 중단할 수 없습니다.");
            return;
        }

        // 차징 중이 아니었으면 일반 공격으로 처리
        if (!chargeManager.IsCharging)
        {
            Debug.Log("차징 중이 아님 - 일반 공격으로 처리");
            FireNormalBullet();
            return;
        }

        // 재장전 중이거나 탄약 없으면 발사 불가
        if (ammoManager.IsReloading || ammoManager.CurrentAmmo <= 0)
        {
            if (ammoManager.CurrentAmmo <= 0 && !ammoManager.IsReloading)
            {
                Debug.Log("탄약 없음 - 재장전 시작");
                ammoManager.StartReload();
            }
            // 차징 상태 초기화 및 이펙트 즉시 종료
            Debug.Log("차징 취소: 재장전 중이거나 탄약 없음");
            chargeManager.StopCharging();
            effectManager.StopPressureEffect();
            return;
        }

        // 발사 쿨다운 체크 (차징샷도 쿨다운 적용)
        float currentCooldown = nextFireTime - Time.time;
        if (currentCooldown > 0)
        {
            Debug.Log($"아직 발사할 수 없음: 쿨다운 {currentCooldown:F2}초 남음");
            // 차징 상태 초기화 및 이펙트 즉시 종료
            chargeManager.StopCharging();
            effectManager.StopPressureEffect();
            return;
        }

        // 차징 레벨에 따라 다른 쿨다운 적용
        float cooldown;
        int chargeLevel = chargeManager.CurrentChargeLevel;
        
        if (chargeLevel == 2)
        {
            cooldown = level2ChargeCooldown;
            Debug.Log($"레벨 2 차징샷 발사: 쿨다운 {cooldown:F2}초 적용");
        }
        else if (chargeLevel == 1)
        {
            cooldown = level1ChargeCooldown;
            Debug.Log($"레벨 1 차징샷 발사: 쿨다운 {cooldown:F2}초 적용");
        }
        else
        {
            cooldown = level0ChargeCooldown;
            Debug.Log($"레벨 0 차징샷 발사: 쿨다운 {cooldown:F2}초 적용");
        }
        
        // 다음 발사 시간 설정
        nextFireTime = Time.time + cooldown;

        // 차징 레벨에 따른 총알 발사
        FireBullet(true);

        // 차징 정보에 따른 사운드 재생
        effectManager.PlayChargeShotSound(chargeManager.CurrentChargeLevel);

        // 차징 이펙트 종료
        effectManager.StopPressureEffect();

        // 차징 상태 초기화
        chargeManager.StopCharging();

        // 탄약 감소
        ammoManager.UseAmmo();
    }

    // 매니저의 유효성 검사
    private bool IsValid()
    {
        if (chargeManager == null || ammoManager == null || effectManager == null || bulletFactory == null)
        {
            Debug.LogWarning("WeaponManager: 일부 매니저 참조가 없습니다.");
            return false;
        }
        return true;
    }

    // 총알 실제 발사 처리 (일반 및 차징샷 공통)
    private void FireBullet(bool isCharged)
    {
        // 매니저 참조 체크
        if (!IsValid())
        {
            Debug.LogWarning("WeaponManager: 필요한 참조가 없어 총알을 발사할 수 없습니다.");
            return;
        }

        // firePoint 체크
        if (firePoint == null)
        {
            Debug.LogWarning("WeaponManager: firePoint가 없어 총알을 발사할 수 없습니다.");
            return;
        }

        Vector2 direction = GetAimDirection();
        Debug.Log($"FireBullet - 발사 방향: {direction}, 벽타기: {isWallSliding}, 바닥: {collisionDetector?.IsGrounded}");
        
        // 차징 레벨에 따른 총알 설정
        Vector3 bulletScale;
        bool isOvercharged = isCharged && chargeManager.CurrentChargeLevel == 2;
        float finalBulletSpeed = bulletSpeed;

        // 러스트 속성 총알인 경우 랜덤 방향으로 살짝 틀어줌
        if (currentBulletType == ElementType.Rust)
        {
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float randomAngle = baseAngle + UnityEngine.Random.Range(-65f, 65f);
            float rad = randomAngle * Mathf.Deg2Rad;
            if (isOvercharged)
            {
                direction = new Vector2(direction.x, direction.y);
            }
            else
            {
                direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
            }
        }
        
        // 발사 위치 조정 (벽타기 상태일 때는 약간 바깥쪽으로 조정)
        Vector3 spawnPosition = firePoint.position;
        if (isWallSliding)
        {
            spawnPosition += new Vector3(direction.x * 0.35f, 0, 0);
        }
        else
        {
            spawnPosition += new Vector3(direction.x * 0.2f, 0, 0);
        }
        Debug.Log($"발사 위치: {spawnPosition}, 방향: {direction}");

        // 차징 레벨별 발사로직
        if (isCharged)
        {
            int chargeLevel = chargeManager.CurrentChargeLevel;

            if (chargeLevel == 2)
            {
                bulletScale = level2BulletScale;
                isOvercharged = true;
                finalBulletSpeed = bulletSpeed * 1.6f;
            }
            else if (chargeLevel == 1)
            {
                bulletScale = level1BulletScale;
                finalBulletSpeed = bulletSpeed * 1.3f;
            }
            else
            {
                bulletScale = normalBulletScale;
            }
        }
        else
        {
            bulletScale = normalBulletScale;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("WeaponManager: Player를 찾을 수 없어 총알을 발사할 수 없습니다.");
            return;
        }

        // BulletFactory를 사용하여 총알 생성
        GameObject bullet = bulletFactory.CreateBullet(currentBulletType, spawnPosition, Quaternion.identity, player);
        if (bullet == null)
        {
            Debug.LogWarning("WeaponManager: 총알 생성에 실패했습니다.");
            return;
        }

        // 총알 초기화
        var bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript == null)
        {
            Debug.LogWarning("WeaponManager: 총알에 Bullet 컴포넌트가 없습니다.");
            ObjectPoolingManager.Instance.ReturnBullet(bullet, currentBulletType);
            return;
        }

        // 총알 크기 설정
        bullet.transform.localScale = bulletScale;

        // 총알 속도 설정
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.velocity = direction * finalBulletSpeed;
        }

        // 총알 데미지 설정
        float damage = bulletDamage * (1f + atkUpPercent);
        if (isCharged)
        {
            int chargeLevel = chargeManager.CurrentChargeLevel;
            if (chargeLevel == 2)
            {
                damage *= 3f; // 2단계 차징 데미지 증가
            }
            else if (chargeLevel == 1)
            {
                damage *= 1.5f; // 1단계 차징 데미지 증가
            }
        }
        bulletScript.Damage = damage;
        
        // 총알의 오버차지 상태 설정
        bulletScript.IsOvercharged = isOvercharged;
        if (isOvercharged) {
            Debug.Log("오버차지 상태로 총알 발사!");
            ApplyOverchargeRecoilDamage();
        }

        // 총알 회전 설정
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 차징샷인 경우 추가 효과 적용
        if (isCharged && chargeManager.CurrentChargeLevel > 0)
        {
            // 트레일 효과 추가
            TrailRenderer trail = bullet.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = bullet.AddComponent<TrailRenderer>();
            }

            // 트레일 설정 업데이트
            trail.startWidth = 0.1f * bulletScale.x;
            trail.endWidth = 0.01f;
            trail.time = 0.1f;
            trail.emitting = true;
            trail.Clear(); // 이전 트레일 제거

            // 트레일 색상 설정
            Gradient gradient = new Gradient();
            Color bulletColor = chargeManager.CurrentChargeLevel == 2 ?
                new Color(1.0f, 0.5f, 0.1f, 1.0f) : // 주황색 (고온 증기)
                new Color(0.7f, 0.7f, 0.7f, 1.0f);  // 회색 (일반 증기)

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

    // 발사 방향 계산
    private Vector2 GetAimDirection()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        bool isGrounded = false;
        
        // 플레이어 컴포넌트에서 바닥 접촉 상태 확인
        if (collisionDetector != null)
        {
            isGrounded = collisionDetector.IsGrounded;
        }
        
        // 벽 슬라이딩 중이고 바닥에 닿지 않은 경우에만 벽 반대 방향으로 발사
        if (isWallSliding && !isGrounded)
        {
            // 벽의 반대 방향으로 발사
            return wallDirection == 1 ? Vector2.left : Vector2.right;
        }
        
        // PlayerMovement 컴포넌트의 방향 정보 항상 우선 사용 (점프 중에도 올바른 방향 유지)
        if (playerMovement != null)
        {
            int direction = playerMovement.FacingDirection;
            Debug.Log($"발사 방향 계산: playerMovement.FacingDirection = {direction}");
            return direction > 0 ? Vector2.right : Vector2.left;
        }
        
        // 플레이어 컴포넌트가 없는 경우 기존 로직으로 대체
        if (player != null)
        {
            SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Debug.Log($"발사 방향 계산: SpriteRenderer.flipX = {spriteRenderer.flipX}");
                return spriteRenderer.flipX ? Vector2.left : Vector2.right;
            }
            else if (player.transform.localScale.x < 0)
            {
                Debug.Log("발사 방향 계산: transform.localScale.x < 0");
                return Vector2.left;
            }
            else
            {
                Debug.Log("발사 방향 계산: 기본값 Vector2.right 사용");
                return Vector2.right;
            }
        }
        return Vector2.right;
    }

    // 탄약 변경 시 호출되는 이벤트 선언
    public event Action<int, int> OnAmmoChanged;

    // 현재 사용 중인 총알 속성 설정
    public void SetBulletType(ElementType type)
    {
        currentBulletType = type;

        GameObject bulletPrefab = bulletFactory.GetBulletPrefab(type);
        var bullet = bulletPrefab.GetComponent<Bullet>();
        if (bullet != null)
        {
            bulletSpeed = bullet.BulletSpeed * (1f + speedUpPercent);
            bulletDamage = bullet.Damage * (1f + atkUpPercent);
        }
    }

    // 외부에서 값 변경 가능
    public void SetSpeedUpPercent(float percent) => speedUpPercent = percent;
    public float SpeedUpPercent => speedUpPercent;
    public void SetAtkUpPercent(float percent) => atkUpPercent = percent;
    public float AtkUpPercent => atkUpPercent;
    public void SetBulletSpeed(float speed) => bulletSpeed = speed;
    public void SetBulletDamage(float damage) => bulletDamage = damage;
    public void SetBulletLifetime(float lifetime) => bulletLifetime = lifetime;
    public void SetNormalBulletScale(Vector3 scale) => normalBulletScale = scale;
    //public void SetAmmo(int ammo) => ammoManager.SetAmmo(ammo);
    public void SetMaxAmmo(int maxAmmo) => ammoManager.SetMaxAmmo(maxAmmo);

    public AmmoManager AmmoManager => ammoManager;
    public ChargeManager ChargeManager => chargeManager;
    public EffectManager EffectManager => effectManager;
    public BulletFactory BulletFactory => bulletFactory;

    // 쿨다운 설정 메서드들
    public void SetLevel0ChargeCooldown(float cooldown) => level0ChargeCooldown = Mathf.Max(0.05f, cooldown);
    public void SetLevel1ChargeCooldown(float cooldown) => level1ChargeCooldown = Mathf.Max(0.1f, cooldown);
    public void SetLevel2ChargeCooldown(float cooldown) => level2ChargeCooldown = Mathf.Max(0.2f, cooldown);
    
    // 모든 쿨다운을 한 번에 설정 (기존 SetFireRate 대체)
    public void SetAllCooldowns(float multiplier)
    {
        level0ChargeCooldown = Mathf.Max(0.05f, level0ChargeCooldown * multiplier);
        level1ChargeCooldown = Mathf.Max(0.1f, level1ChargeCooldown * multiplier);
        level2ChargeCooldown = Mathf.Max(0.2f, level2ChargeCooldown * multiplier);
    }

    // 총알 속성 가져오기
    public ElementType GetBulletType()
    {
        return currentBulletType;
    }

    // 파이어 포인트 방향 제어 메서드 추가
    public void SetFirePointDirection(int direction)
    {
        if (firePoint != null)
        {
            Vector3 scale = firePoint.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            firePoint.localScale = scale;
        }
    }

    public void ResetFirePointDirection()
    {
        if (firePoint != null)
        {
            Vector3 scale = firePoint.localScale;
            scale.x = Mathf.Abs(scale.x);
            firePoint.localScale = scale;
        }
    }

    public void SetWallSlideInfo(bool isSliding, int wallDir)
    {
        isWallSliding = isSliding;
        wallDirection = wallDir;
    }

    // 오버차지 데미지를 플레이어에게 즉시 적용
    private void ApplyOverchargeRecoilDamage()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerHP playerHP = player.GetComponent<PlayerHP>();
            if (playerHP != null)
            {
                float selfDamage = playerHP.MaxHP * 0.05f;
                Debug.Log($"오버차지 반동 즉시 적용: 플레이어 최대 체력 = {playerHP.MaxHP}, 데미지 = {selfDamage}");
                playerHP.TakeDamage(selfDamage);
                Debug.Log($"과열 공격 반동으로 플레이어가 {selfDamage} 데미지를 즉시 입었습니다! 현재 체력: {playerHP.CurrentHP}");
            }
            else
            {
                Debug.LogError("PlayerHP 컴포넌트를 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
        }
    }
}