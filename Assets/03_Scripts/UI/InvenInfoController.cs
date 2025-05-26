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

    [Header("특성 해금 버튼")]
    [SerializeField] private Button utilityUnLockBtn;

    [Header("특성 초기화")]
    [SerializeField] private Button utilityResetBtn;

    [SerializeField] private Transform isRealUtilityResetPopUpTransform;
    [SerializeField] private GameObject isRealUtilityResetPopUp;

    [SerializeField] private Button utilityResetYesBtn;
    [SerializeField] private Button utilityResetNoBtn;

    [Header("아이템 설명과 장착/해제 버튼")]
    [SerializeField] private TextMeshProUGUI descriptionTitle;
    [SerializeField] private TextMeshProUGUI itemDescription;


    [SerializeField] private Button utilityEquipBtn;
    [SerializeField] private Button utilityRemoveBtn;


    [Header("현재 장착 중인 특성 이미지")]
    [SerializeField] public List<Image> currentEquippedUtility;

    private UtilityChangedStatController utilityChangedStatController;
    private Player player;
    private int revertUtilityPoint;

    [HideInInspector] public float maxAmmo;

    [HideInInspector] public float bulletDamage;
    [HideInInspector] public float bulletSpeed;


    public void Start()
    {
        player = FindObjectOfType<Player>();
        utilityChangedStatController = GetComponent<UtilityChangedStatController>();

        utilityResetBtn.onClick.AddListener(() => IsRealResetUtility());

        utilityResetYesBtn.onClick.AddListener(() => ResetUtility());
        utilityResetNoBtn.onClick.AddListener(() => isRealUtilityResetPopUp.SetActive(false));
    }

    public void ClearDescriptionTitle()
    {
        descriptionTitle.text = "";
    }

    public void ClearDescription()
    {
        itemDescription.text = "";
    }

    public void IsRealResetUtility() //초기화하시겠습니까?
    {
        if (player.UnLockedUtility.Count > 0)
        {
            isRealUtilityResetPopUp.SetActive(true);
        }
    }

    public void ResetUtility() //특성 초기화
    {
        player.UnLockedUtility.Clear();

        player.utilityPoint += revertUtilityPoint;

        player.UpdateCurrentInventory();

        CreatSlotSystem.Instance.RefreshAllOwnPoints();
        PlayerUI.Instance.TempAddUtilityPoint();

        foreach (var icon in currentEquippedUtility)
        {
            icon.sprite = null;
            var color = icon.color;
            color.a = 0f;
            icon.color = color;
        }

        utilityChangedStatController.currentUtilityList.Clear();

        isRealUtilityResetPopUp.SetActive(false);

        revertUtilityPoint = 0;
    }


    public void UnLockedUtility(ItemData utilityItemData) //특성 해금
    {
        utilityUnLockBtn.onClick.RemoveAllListeners();

        descriptionTitle.text = utilityItemData.ItemName;
        itemDescription.text = utilityItemData.ItemDescription;

        utilityUnLockBtn.gameObject.SetActive(true);

        utilityEquipBtn.gameObject.SetActive(false);
        utilityRemoveBtn.gameObject.SetActive(false);

        if (player.CurrentUtilityPoint >= utilityItemData.utilityPointForUnLock)
        {
            utilityUnLockBtn.onClick.AddListener(() => SlotInteract(utilityItemData));
        }
    }


    public void SlotInteract(ItemData itemData) //해금 후 특성 슬롯 상호작용
    {
        if (!player.UnLockedUtility.Contains(itemData.id))
        {
            player.utilityPoint -= itemData.utilityPointForUnLock;
            revertUtilityPoint += itemData.utilityPointForUnLock;

            PlayerUI.Instance.utilityPointText.text = player.utilityPoint.ToString();

            player.UpdateCurrentInventory(); //현재는 플레이어 포인트 현황만 업데이트 중
            player.UpdateCurrentUnLockedUtility(itemData);

            CreatSlotSystem.Instance.RefreshAllOwnPoints();

        }

        utilityUnLockBtn.gameObject.SetActive(false);

        utilityEquipBtn.gameObject.SetActive(true);
        utilityRemoveBtn.gameObject.SetActive(true);


        utilityEquipBtn.onClick.RemoveAllListeners(); //장착 버튼 리스너 초기화
        utilityRemoveBtn.onClick.RemoveAllListeners(); //해제 버튼 리스너 초기화

        descriptionTitle.text = itemData.ItemName;
        itemDescription.text = itemData.ItemDescription;

        utilityEquipBtn.onClick.AddListener(() => UtilityEquipped(itemData));
        utilityRemoveBtn.onClick.AddListener(() => UtilityRemoved(itemData));


    }

    public void UtilityEquipped(ItemData itemData) //장착 시 실행 함수
    {
        if (!utilityChangedStatController.currentUtilityList.Contains(itemData)) //특성 개당 하나씩만 추가할 수 있도록 만듦
        {
            utilityChangedStatController.EquippedUtility(itemData); //특성 장착시 UI 업데이트


            switch (itemData.id) //선택한 슬롯 내의 특성 데이터 속 id값을 받아옴
            {
                case 1001:

                    utilityChangedStatController.MaxHPUP(itemData.effectValue);
                    break;


                case 1002:

                    maxAmmo = WeaponManager.Instance.AmmoManager.MaxAmmo;
                    utilityChangedStatController.MaxMPUP(itemData.effectValue, maxAmmo);

                    break;

                case 1003:

                    utilityChangedStatController.ATKUP(itemData.effectValue);
                    break;

                case 1004:

                    utilityChangedStatController.ATKSUP(itemData.effectValue);

                    break;

                case 1005:

                    utilityChangedStatController.MSUP(itemData.effectValue);
                    break;

                case 1006:

                    utilityChangedStatController.RSUP(itemData.effectValue);
                    break;

                case 1007:

                    utilityChangedStatController.RDUP(itemData.effectValue);
                    break;

                case 1008:

                    utilityChangedStatController.DDUP(itemData.effectValue);
                    break;

                case 1009:

                    utilityChangedStatController.WeighSpeed(itemData.effectValue);
                    break;

                case 1010:

                    utilityChangedStatController.WeighPower(itemData.effectValue);
                    break;

                case 1011:

                    utilityChangedStatController.WeighHealth(itemData.effectValue);
                    break;

                case 1012:

                    utilityChangedStatController.BestDefenceIsAttack(itemData.effectValue);
                    break;

                case 1013:

                    utilityChangedStatController.SpeedRacer(itemData.effectValue);
                    break;

                case 1014:
                    maxAmmo = WeaponManager.Instance.AmmoManager.MaxAmmo;
                    utilityChangedStatController.Trinity(itemData.effectValue, maxAmmo);
                    break;

                case 1015:

                    utilityChangedStatController.InvincibleWhenDash();
                    break;

            }

            return;
        }
    }

    public void UtilityRemoved(ItemData itemdata) //특성 해제
    {
        if (itemdata == null) return;

        utilityChangedStatController.RemovedUtility(itemdata.id); //특성 해제시 UI 업데이트


        switch (itemdata.id)
        {
            case 1001:
                utilityChangedStatController.RemovedMaxHPUP();
                break;

            case 1002:
                maxAmmo = WeaponManager.Instance.AmmoManager.MaxAmmo;
                utilityChangedStatController.RemovedMaxMPUP(maxAmmo);
                break;

            case 1003:
                utilityChangedStatController.RemovedATKUP();
                break;

            case 1004:
                utilityChangedStatController.RemovedATKSUP();
                break;

            case 1005:
                utilityChangedStatController.RemovedMSUP();
                break;

            case 1006:
                utilityChangedStatController.RemovedRSUP();
                break;

            case 1007:
                utilityChangedStatController.RemovedRDUP();
                break;

            case 1008:
                utilityChangedStatController.RemovedDDUP();
                break;

            case 1009:
                utilityChangedStatController.RemovedWeighSpeed(itemdata.effectValue);
                break;

            case 1010:
                utilityChangedStatController.RemovedWeighPower();
                break;

            case 1011:
                utilityChangedStatController.RemovedWeighHealth();
                break;

            case 1012:
                utilityChangedStatController.RemovedBestDefenceIsAttack(itemdata.effectValue);
                break;

            case 1013:
                utilityChangedStatController.RemovedSpeedRacer(itemdata.effectValue);
                break;

            case 1014:
                maxAmmo = WeaponManager.Instance.AmmoManager.MaxAmmo;
                utilityChangedStatController.RemovedTrinity(maxAmmo);
                break;

            case 1015:
                utilityChangedStatController.RemovedInvincibleWhenDash();
                break;

        }
    }
}