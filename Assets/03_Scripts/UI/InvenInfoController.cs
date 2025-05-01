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
    private ItemData selectedItem; //선택한 슬롯에 담겨있는 특성 SO
    private Player player;

    [HideInInspector] public float maxAmmo;

    [HideInInspector] public float bulletDamage;
    [HideInInspector] public float bulletSpeed;

    public void Start()
    {
        player = FindObjectOfType<Player>();
        utilityChangedStatController = GetComponent<UtilityChangedStatController>();
    }

    public void SlotInteract(ItemData itemData)
    {
        utilityEquipBtn.onClick.RemoveAllListeners(); //장착 버튼 리스너 초기화
        utilityRemoveBtn.onClick.RemoveAllListeners(); //해제 버튼 리스너 초기화

        descriptionTitle.text = itemData.ItemName;
        itemDescription.text = itemData.ItemDescription;

        selectedItem = itemData; //선택한 슬롯 데이터를 selectedItem변수에 할당


        utilityEquipBtn.onClick.AddListener(() => UtilityEquipped(itemData));

        utilityRemoveBtn.onClick.AddListener(UtilityRemoved); //버튼들 리스너 등록


    }

    public void UtilityEquipped(ItemData itemData) //장착 시 실행 함수
    {
        utilityChangedStatController.EquippedUtility(itemData); //특성 장착시 UI 업데이트

        if (player.CurrentUtilityPoint >= itemData.utilityPointForUnLock)
        {
            switch (itemData.id) //선택한 슬롯 내의 특성 데이터 속 id값을 받아옴
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
    }

    public void UtilityRemoved() //특성 해제
    {
        if (selectedItem == null) return;

        utilityChangedStatController.RemovedUtility(selectedItem.id); //특성 장착시 UI 업데이트


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

        //해제 버튼 비활성화
        //utilityRemoveBtn.interactable = false;
    }
}