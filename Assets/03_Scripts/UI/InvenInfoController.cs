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

    //private bool isAableToDecrease = false;

    Player player;



    public void Start()
    {
        player = FindObjectOfType<Player>();
        utilityChangedStatController = GetComponent<UtilityChangedStatController>();
    }

    public void slotInteract(string ItemDescription, string ItemName, Sprite ItemIcon, float effectValue, AttributeType attributeType, int id, int utilityPointForUnLock)
    {
        descriptionTitle.text = ItemName;
        itemDescription.text = ItemDescription;

        utilityEquipBtn.onClick.RemoveAllListeners();
        utilityEquipBtn.onClick.AddListener(() => UtilityEquipped(ItemIcon, effectValue, attributeType, id, utilityPointForUnLock));


    }

    public void UtilityEquipped(Sprite ItemIcon, float effectValue, AttributeType attributeType, int id, int utilityPointForUnLock) //장착 시 실행 함수
    {
        //플레이어 쪽의 현재 장착 중인 특성 아이콘 업데이트
        for (int i = 0; i < currentEquippedUtility.Count; i++)
        {
            if (currentEquippedUtility[i].sprite == null)
            {
                Color temp = currentEquippedUtility[i].color;
                temp.a = 1f;
                currentEquippedUtility[i].color = temp;

                currentEquippedUtility[i].sprite = ItemIcon;

                player.utilityPoint -= utilityPointForUnLock;
                PlayerUI.Instance.utilityPointText.text = player.utilityPoint.ToString();
                player.UpdateCurrentInventory();


                utilityRemoveBtn.onClick.AddListener(() => UtilityRemoved(ItemIcon, effectValue, attributeType, id));

                switch (id)
                {
                    case 1001:

                        utilityChangedStatController.MaxHPUP(effectValue);
                        break;


                    case 1002:

                        maxAmmo = WeaponManager.Instance.maxAmmo;
                        utilityChangedStatController.MaxMPUP(effectValue, maxAmmo);

                        break;

                    case 1003:

                        utilityChangedStatController.ATKUP(effectValue, bulletDamage);
                        break;

                    case 1004:

                        utilityChangedStatController.ATKSUP(effectValue, bulletSpeed);

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
        switch (id)
        {
            case 1001:
                utilityChangedStatController.RemovedMaxHPUP();
                break;
        }
    }
}