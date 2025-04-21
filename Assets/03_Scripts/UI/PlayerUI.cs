using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [SerializeField] private int healHP;

    [Header("HP 바 깨짐")]
    [SerializeField] public GameObject Basic;
    [SerializeField] public GameObject Hurt;
    [SerializeField] public GameObject VeryHurt;
    [SerializeField] public GameObject killme;

    public float shakeTime;
    public float shakeRange;

    [Header("플레이어 속성 아이콘 업데이트")]
    [SerializeField] public ItemData attributeType;
    [SerializeField] public TextMeshProUGUI typeName;
    [SerializeField] public Image typeIcon;

    [Header("Utility Point")]
    [SerializeField] public TextMeshProUGUI utilityPointText;
    static int utilityPoint;

    static PlayerHP playerHP;
    public TypeItemSlotList typeItemSlotList;

    static int currentTypeIndex = 0;

    public Dictionary<ItemData, Sprite> TypeItemDic = new Dictionary<ItemData, Sprite>();

    public void Start()
    {
        playerHP = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHP>();

        float maxHP = playerHP.MaxHP;
        float currentHP = playerHP.CurrentHP;

        Vector3 realPosition = healthBar.transform.position;
        Debug.Log($"{realPosition}");

        healthBarImage.fillAmount = 1f;

        // 인벤토리 매니저 이벤트 구독 - 무기 속성 변경 시 UI 업데이트
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnWeaponAttributeChanged += UpdateWeaponAttributeUI;
        }

        // 초기 무기 속성 설정
        UpdateWeaponAttributeUI(InventoryManager.Instance.EquippedWeaponAttribute);
    }

    public void Voscuro(Vector3 realPosition, float maxHP, float currentHP)
    {
        HealHP();
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
            float x = Random.value * shakeRange - (shakeRange / 2);
            float y = Random.value * shakeRange - (shakeRange / 2);

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
        utilityPoint += utilityPointForOneWay;

        utilityPointText.text = utilityPoint.ToString();
    }
}