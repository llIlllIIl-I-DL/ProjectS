using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; // WeaponManager.OnAmmoChanged 구독을 위해 추가

public class PlayerUI : MonoBehaviour
{
    public static PlayerUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    [Header("HP 바")]
    [SerializeField] private Scrollbar healthBar;
    [SerializeField] private Image healthBarImage;
    [SerializeField] private Image healLight;

    [SerializeField] private TextMeshProUGUI hpText;

    [Header("HP 바 깨짐")]
    [SerializeField] private GameObject Basic;
    [SerializeField] private GameObject Hurt;
    [SerializeField] private GameObject VeryHurt;
    [SerializeField] private GameObject killme;

    private float shakeTime;
    private float shakeRange;

    [Header("Ammo 바 업데이트")]
    [SerializeField] public Scrollbar ammoBar;
    [SerializeField] public Image ammoBarImage;
    [SerializeField] public Image ammoBarLight;
    


    [Header("플레이어 속성 아이콘 업데이트")]
    [SerializeField] public ItemData attributeType;
    [SerializeField] public TextMeshProUGUI typeName;
    [SerializeField] public Image typeIcon;

    [Header("Utility Point")]
    [SerializeField] public TextMeshProUGUI utilityPointText;


    private Player player;
    private PlayerHP playerHP;
    private TypeItemSlotList typeItemSlotList;

    public Dictionary<ItemData, Sprite> TypeItemDic = new Dictionary<ItemData, Sprite>();

    public void Start()
    {
        player = FindObjectOfType<Player>();
        playerHP = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHP>();

        float maxHP = playerHP.MaxHP;
        float currentHP = playerHP.CurrentHP;

        int ammo = WeaponManager.Instance.currentAmmo;
        int maxAmmo = WeaponManager.Instance.maxAmmo;

        

        // 탄약 변경 이벤트 구독
        if (WeaponManager.Instance != null)
        {
            WeaponManager.Instance.OnAmmoChanged += UpdateAmmoUI;
        }
        // 초기 Ammo UI 업데이트
        UpdateAmmoUI(ammo, maxAmmo);

        Vector3 realPosition = healthBar.transform.position;
        Debug.Log($"{realPosition}");

        healthBarImage.fillAmount = 1f;

        // 인벤토리 매니저 이벤트 구독 - 무기 속성 변경 시 UI 업데이트
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnWeaponAttributeChanged += UpdateWeaponAttributeUI;
        }

        UpdatePlayerHPInUItext();
        // 초기 무기 속성 설정
        UpdateWeaponAttributeUI(InventoryManager.Instance.EquippedWeaponAttribute);
    }

    public void Voscuro(Vector3 realPosition, float maxHP, float currentHP)
    {
        HealHP();
    }

    public void UpdatePlayerHPInUItext()
    {
        hpText.text = playerHP.CurrentHP.ToString();
        //hpText.text = player.CurrentMaxHP.ToString();
    }

    public void SetHealthBar(float maxHP, float currentHP) //여기 언젠가 리팩토리 필요...
    {
        StartCoroutine(ShakingHPBar());

        float currentHPAmount = (float)currentHP / maxHP;

        healthBarImage.fillAmount = currentHPAmount;

        float hpBalance = (float)currentHP / maxHP;

        if (hpBalance > 0.9f)
        {
            Basic.SetActive(true);
            Hurt.SetActive(false);
            VeryHurt.SetActive(false);
            killme.SetActive(false);
        }

        else if (hpBalance > 0.7f && hpBalance < 0.9f)
        {
            Basic.SetActive(false);
            Hurt.SetActive(true);
            VeryHurt.SetActive(false);
            killme.SetActive(false);
        }

        else if (hpBalance > 0.3f && hpBalance < 0.7f)
        {
            Basic.SetActive(false);
            Hurt.SetActive(false);
            VeryHurt.SetActive(true);
            killme.SetActive(false);
        }

        else if (hpBalance < 0.3f)
        {
            Basic.SetActive(false);
            Hurt.SetActive(false);
            VeryHurt.SetActive(false);
            killme.SetActive(true);
        }

    }

    public IEnumerator ShakingHPBar()
    {

        float elapsed = 0.0f;
        Vector3 originalPosition = healthBar.transform.position;


        while (elapsed < shakeTime)
        {
            elapsed += Time.deltaTime;
            float x = UnityEngine.Random.value * shakeRange - (shakeRange / 2);
            float y = UnityEngine.Random.value * shakeRange - (shakeRange / 2);

            healthBar.transform.position = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);
            yield return null;
        }

        healthBar.transform.position = originalPosition;
    }

    public void HealHP()
    {
        float maxHP = playerHP.MaxHP;
        float currentHP = playerHP.CurrentHP;

        Vector3 realPosition = healthBar.transform.position;


        healthBar.transform.position = new Vector3(realPosition.x, realPosition.y, realPosition.z);


        if (currentHP > maxHP)
        {
            return;
        }

        StartCoroutine(HealHPBarLighting());
        SetHealthBar(maxHP, currentHP);
    }

    public IEnumerator HealHPBarLighting()
    {
        byte alpha = 0;

        for (int i = 0; i < 5; i++)
        {
            alpha += 51;
            yield return new WaitForSeconds(0.05f);
            healLight.color = new Color32(255, 255, 255, alpha);
        }

        healLight.color = new Color32(255, 255, 255, 0);
    }

    // 무기 속성 UI 업데이트 메소드
    private void UpdateWeaponAttributeUI(ItemData weaponAttribute)
    {
        if (weaponAttribute != null)
        {
            attributeType = weaponAttribute;
            typeName.text = weaponAttribute.ItemName;
            typeIcon.sprite = weaponAttribute.Icon;

            typeIcon.preserveAspect = true;
        }
        else
        {
            // 기본 노말 속성으로 되돌리기
            // 기본 아이템 데이터 참조 필요
        }
    }


    public void MovetoLeftType()
    {
        if (typeItemSlotList == null) return;
        
        // 인벤토리에서 사용 가능한 무기 속성 목록 가져오기
        List<ItemData> availableTypes = null;
        if (InventoryManager.Instance != null)
        {
            availableTypes = InventoryManager.Instance.GetWeaponAttributes();
        }
        
        if (availableTypes == null || availableTypes.Count <= 1) return;
        
        // 현재 장착된 무기 속성 찾기
        ItemData currentType = attributeType;
        int currentIndex = -1;
        
        for (int i = 0; i < availableTypes.Count; i++)
        {
            if (availableTypes[i].elementType == currentType.elementType)
            {
                currentIndex = i;
                break;
            }
        }
        
        // 이전 인덱스 계산 (순환)
        int prevIndex = (currentIndex - 1 + availableTypes.Count) % availableTypes.Count;
        
        // 이전 무기 속성 장착
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.EquipWeaponAttribute(availableTypes[prevIndex]);
            Debug.Log($"무기 속성 변경: {availableTypes[prevIndex].ItemName}");
        }
    }

    public void MovetoRightType()
    {
        if (typeItemSlotList == null) return;
        
        // 인벤토리에서 사용 가능한 무기 속성 목록 가져오기
        List<ItemData> availableTypes = null;
        if (InventoryManager.Instance != null)
        {
            availableTypes = InventoryManager.Instance.GetWeaponAttributes();
        }
        
        if (availableTypes == null || availableTypes.Count <= 1) return;
        
        // 현재 장착된 무기 속성 찾기
        ItemData currentType = attributeType;
        int currentIndex = -1;
        
        for (int i = 0; i < availableTypes.Count; i++)
        {
            if (availableTypes[i].elementType == currentType.elementType)
            {
                currentIndex = i;
                break;
            }
        }
        
        // 다음 인덱스 계산 (순환)
        int nextIndex = (currentIndex + 1) % availableTypes.Count;
        
        // 다음 무기 속성 장착
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.EquipWeaponAttribute(availableTypes[nextIndex]);
            Debug.Log($"무기 속성 변경: {availableTypes[nextIndex].ItemName}");
        }
    }

    public void AddUtilityPoint(int utilityPointForOneWay)
    {
        player.utilityPoint += utilityPointForOneWay;
        utilityPointText.text = player.utilityPoint.ToString();
    }

    // 탄약 UI를 업데이트하는 메서드
    private void UpdateAmmoUI(int ammo, int maxAmmo)
    {
        float ratio = (float)ammo / maxAmmo;
        ammoBar.value = ratio;
        ammoBarImage.fillAmount = ratio;
    }
}