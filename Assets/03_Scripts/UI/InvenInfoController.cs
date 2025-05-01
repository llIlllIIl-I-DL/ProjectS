using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using Unity.VisualScripting;

public class InvenInfoController : MonoBehaviour
{
    public static InvenInfoController Instance { get; private set; }

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

    [Header("아이템 설명과 장착/해제 버튼")]
    [SerializeField] private TextMeshProUGUI descriptionTitle;
    [SerializeField] private TextMeshProUGUI itemDescription;

    [SerializeField] private Button utilityEquipBtn;
    [SerializeField] private Button utilityRemoveBtn;


    [Header("현재 장착 중인 특성 이미지")]
    [SerializeField] public List<Image> currentEquippedUtility;


    private UtilityChangedStatController utilityChangedStatController;
    private ItemData selectedItem;


    [HideInInspector] public float maxAmmo;

    [HideInInspector] public float bulletDamage;
    [HideInInspector] public float bulletSpeed;

    Player player;



    public void Start()
    {
        player = FindObjectOfType<Player>();
        utilityChangedStatController = GetComponent<UtilityChangedStatController>();
    }

    public void slotInteract(ItemData itemData)
    {
        utilityEquipBtn.onClick.RemoveAllListeners();
        utilityRemoveBtn.onClick.RemoveAllListeners();

        // (2) UI에 아이템 정보 세팅
        descriptionTitle.text = itemData.ItemName;
        itemDescription.text = itemData.ItemDescription;

        // (3) selectedItem 갱신
        selectedItem = itemData;

        // (4) 장착 버튼 리스너 등록
        utilityEquipBtn.onClick.AddListener(() => UtilityEquipped(itemData));

        // (5) 제거 버튼 리스너 등록 (한 번만!)
        utilityRemoveBtn.onClick.AddListener(UtilityRemoved);


    }

    public void UtilityEquipped(ItemData itemData) //장착 시 실행 함수
    {
        utilityChangedStatController.EquippedUtility(itemData);

        switch (itemData.id)
        {
            case 1001:

                utilityChangedStatController.MaxHPUP(itemData.effectValue);
                break;


            case 1002:

                maxAmmo = WeaponManager.Instance.maxAmmo;
                utilityChangedStatController.MaxMPUP(itemData.effectValue, maxAmmo);

                break;

            case 1003:

                utilityChangedStatController.ATKUP(itemData.effectValue, bulletDamage);
                break;

            case 1004:

                utilityChangedStatController.ATKSUP(itemData.effectValue, bulletSpeed);

                break;

            case 1005:

                Debug.Log("저는 1005번입니다");
                break;

            case 1006:

                Debug.Log("저는 1006번입니다");
                break;

            case 1007:

                Debug.Log("저는 1007번입니다");
                break;

            case 1008:

                Debug.Log("저는 1008번입니다");
                break;

            case 1009:

                Debug.Log("저는 1009번입니다");
                break;

            case 1010:

                Debug.Log("저는 1010번입니다");
                break;

            case 1011:

                Debug.Log("저는 1011번입니다");
                break;

            case 1012:

                Debug.Log("저는 1012번입니다");
                break;

            case 1013:

                Debug.Log("저는 1013번입니다");
                break;

            case 1014:

                Debug.Log("저는 1014번입니다");
                break;

            case 1015:

                Debug.Log("저는 1015번입니다");
                break;

        }

        return;


    }

    public void UtilityRemoved()
    {
        if (selectedItem == null) return;

        // 1) 데이터/UI 실제 해제
        utilityChangedStatController.RemovedUtility(selectedItem.id);

        switch (selectedItem.id)
        {
            case 1001:
                utilityChangedStatController.RemovedMaxHPUP();
                break;

            case 1002:
                utilityChangedStatController.RemovedMaxMPUP();
                break;

            case 1003:
                utilityChangedStatController.RemovedATKUP();
                break;

            case 1004:
                utilityChangedStatController.RemovedATKSUP();
                break;

        }

        utilityRemoveBtn.onClick.RemoveAllListeners();
        selectedItem = null;

        // (옵션) 제거 버튼 비활성화
        utilityRemoveBtn.interactable = false;
    }
}