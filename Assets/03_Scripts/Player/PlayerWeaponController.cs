using UnityEngine;
using UnityEngine.UI;

public class PlayerWeaponController : MonoBehaviour
{
    [Header("무기 제어")]
    [SerializeField] private Transform firePoint;
    
    [Header("무기 인터페이스")]
    [SerializeField] private Text currentWeaponText;
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Text ammoText;
    [SerializeField] private Slider chargeSlider;
    
    private Camera mainCamera;
    private WeaponManager weaponManager;
    private InventoryManager inventoryManager;  // 추가 필요한 인벤토리 매니저 참조
    
    private bool isCharging = false;
    
    private void Start()
    {
        mainCamera = Camera.main;
        weaponManager = WeaponManager.Instance;
        inventoryManager = InventoryManager.Instance;
        
        // 발사 지점 할당
        if (firePoint != null)
        {
            weaponManager.FirePoint = firePoint;
        }
        
        // 무기 UI 업데이트
        UpdateWeaponUI();
    }
    
    private void Update()
    {
        // 마우스 위치를 기반으로 발사 방향 계산
        Vector2 shootDirection = GetShootDirection();
        
        // 마우스 입력 처리
        if (Input.GetMouseButtonDown(0))  // 좌클릭 시작
        {
            HandleMouseDown(shootDirection);
        }
        else if (Input.GetMouseButton(0))  // 좌클릭 유지 중
        {
            HandleMouseHold(shootDirection);
        }
        else if (Input.GetMouseButtonUp(0))  // 좌클릭 종료
        {
            HandleMouseUp(shootDirection);
        }
        
        // 무기 교체 처리
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwitchWeapon();
        }
        
        // 재장전 처리
        if (Input.GetKeyDown(KeyCode.R))
        {
            // 재장전 로직은 WeaponManager 내부에서 처리
        }
        
        // 차징 중이면 차징 슬라이더 업데이트
        if (isCharging && weaponManager.CanWeaponCharge())
        {
            UpdateChargeUI();
        }
        
        // 탄약 UI 업데이트
        UpdateAmmoUI();
    }
    
    // 발사 방향 계산 (마우스 포인터 위치 기준)
    private Vector2 GetShootDirection()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = -mainCamera.transform.position.z;
        Vector3 worldMousePosition = mainCamera.ScreenToWorldPoint(mousePosition);
        
        Vector2 direction = (worldMousePosition - transform.position).normalized;
        return direction;
    }
    
    // 좌클릭 시작 처리
    private void HandleMouseDown(Vector2 direction)
    {
        WeaponData currentWeapon = weaponManager.GetCurrentWeapon();
        
        // 차징 가능한 무기면 차징 시작
        if (currentWeapon != null && currentWeapon.canCharge)
        {
            weaponManager.StartCharging();
            isCharging = true;
            
            // 차징 슬라이더 표시
            if (chargeSlider != null)
            {
                chargeSlider.gameObject.SetActive(true);
                chargeSlider.value = 0f;
            }
        }
        else
        {
            // 일반 무기면 즉시 발사
            weaponManager.FireWeapon(direction);
        }
    }
    
    // 좌클릭 유지 처리
    private void HandleMouseHold(Vector2 direction)
    {
        // 차징 중이면 차징 업데이트
        if (isCharging)
        {
            weaponManager.UpdateCharging(Time.deltaTime);
        }
    }
    
    // 좌클릭 종료 처리
    private void HandleMouseUp(Vector2 direction)
    {
        // 차징 중이면 차징 릴리즈 (발사)
        if (isCharging)
        {
            weaponManager.ReleaseCharge(direction);
            isCharging = false;
            
            // 차징 슬라이더 숨기기
            if (chargeSlider != null)
            {
                chargeSlider.gameObject.SetActive(false);
            }
        }
    }
    
    // 무기 교체
    private void SwitchWeapon()
    {
        // 인벤토리에서 다음 무기로 변경
        WeaponData nextWeapon = inventoryManager.GetNextWeapon();
        if (nextWeapon != null)
        {
            weaponManager.EquipWeapon(nextWeapon);
            UpdateWeaponUI();
        }
    }
    
    // 무기 아이콘과 이름 업데이트
    private void UpdateWeaponUI()
    {
        WeaponData currentWeapon = weaponManager.GetCurrentWeapon();
        if (currentWeapon == null) return;
        
        if (currentWeaponText != null)
        {
            currentWeaponText.text = currentWeapon.weaponName;
        }
        
        if (weaponIcon != null && currentWeapon.weaponSprite != null)
        {
            weaponIcon.sprite = currentWeapon.weaponSprite;
            weaponIcon.enabled = true;
        }
    }
    
    // 탄약 정보 업데이트
    private void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{weaponManager.currentAmmo} / {weaponManager.GetCurrentWeapon()?.maxAmmo ?? 0}";
        }
    }
    
    // 차징 슬라이더 업데이트
    private void UpdateChargeUI()
    {
        if (chargeSlider != null)
        {
            float chargePercent = weaponManager.GetCurrentChargePercent();
            chargeSlider.value = chargePercent;
            
            // 오버차지 상태면 슬라이더 색상 변경
            if (weaponManager.IsOvercharged())
            {
                chargeSlider.fillRect.GetComponent<Image>().color = Color.red;
            }
            else
            {
                // 차징 정도에 따라 색상 변경 (파란색에서 녹색으로)
                chargeSlider.fillRect.GetComponent<Image>().color = Color.Lerp(Color.blue, Color.green, chargePercent);
            }
        }
    }
} 