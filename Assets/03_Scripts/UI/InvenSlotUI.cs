using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InvenSlotUI : MonoBehaviour
{
    [Header("슬롯 정보")]
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNeedPoint;
    [SerializeField] private TextMeshProUGUI itemOwnPoint;
    //[SerializeField] private TextMeshProUGUI itemDescription;

    /*
    [Header("특성 해금")]
    [SerializeField] private Button unLockButton;
    [SerializeField] private TextMeshProUGUI pointForUnLock;
    */

    private ItemData utilityItemData;
    private Player player; //player의 특성 포인트 현황을 받아오기 위함 

    private int slotIndex = 0;

    public void Start()
    {
        //unLockButton.onClick.AddListener(() => UnLockUtilitySlot());
    }


    public ItemData GetUtilityItemData()
    {
        return utilityItemData; //ItemData를 슬롯에 각각 할당해줘야 하기 때문에 이 함수에서 ItemData를 utilityItemData라는 이름으로 리턴
    }

    public void SetItem(ItemData item, Player player)
    {
        utilityItemData = item;
        this.player = player;

        RefreshUI();
    }


    public void RefreshUI()
    {
        if (utilityItemData != null)
        {
            itemName.text = utilityItemData.ItemName;

            //itemDescription.text = utilityItemData.ItemDescription;

            itemOwnPoint.text = player.utilityPoint.ToString();
            itemNeedPoint.text = utilityItemData.utilityPointForUnLock.ToString();
            
            itemIcon.sprite = utilityItemData.Icon;
        }
    }



    public void UnLockUtilitySlot()
    {

    }
}
