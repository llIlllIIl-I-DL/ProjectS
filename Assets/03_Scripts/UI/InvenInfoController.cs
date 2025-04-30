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


    [Header("현재 장착 중인 특성")]
    [SerializeField] private List<Image> currentEquippedUtility; 


    private UtilityChangedStatController utilityChangedStatController;


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
        descriptionTitle.text = itemData.ItemName;
        itemDescription.text = itemData.ItemDescription;

        utilityEquipBtn.onClick.RemoveAllListeners();
        utilityEquipBtn.onClick.AddListener(() => UtilityEquipped(itemData));


    }

    public void UtilityEquipped(ItemData itemData) //장착 시 실행 함수
    {
        //플레이어 쪽의 현재 장착 중인 특성 아이콘 업데이트

        for (int i = 0; i < currentEquippedUtility.Count; i++)
        {
            if (currentEquippedUtility[i].sprite == null)
            {
                Color temp = currentEquippedUtility[i].color;
                temp.a = 1f;
                currentEquippedUtility[i].color = temp;

                currentEquippedUtility[i].sprite = itemData.Icon;

                player.utilityPoint -= itemData.utilityPointForUnLock;
                PlayerUI.Instance.utilityPointText.text = player.utilityPoint.ToString();
                player.UpdateCurrentInventory();


                utilityRemoveBtn.onClick.AddListener(() => UtilityRemoved(itemData.Icon, itemData.effectValue, itemData.attributeType, itemData.id));

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
        }
    }

    public void UtilityRemoved(Sprite ItemIcon, float effectValue, AttributeType attributeType, int id)
    {
        //선택한 슬롯의 특성 아이콘이 remove되어야 하며 장착 된 특성 슬롯 중간에 빈칸이 생기면 남은 이미지들은 빈칸 없이 재배치 됨!
        
        for (int i = 0; i < currentEquippedUtility.Count; i++)
        {
            if (currentEquippedUtility[i].sprite != null)
            {
                
                Color temp = currentEquippedUtility[i].color;
                temp.a = 0f;
                currentEquippedUtility[i].color = temp;

                utilityChangedStatController.RemovedUtility(id);
        

                switch (id)
                {
                    case 1001:
                        utilityChangedStatController.RemovedMaxHPUP();
                        break;
                }
            }
        }
    }
}