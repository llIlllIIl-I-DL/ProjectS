using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using VInspector.Libs;

public class CreatSlotSystem : MonoBehaviour
{
    public static CreatSlotSystem Instance { get; private set; }

    public List<InvenSlotUI> slotList = new List<InvenSlotUI>();
    public List<ItemData> utilityItemList = new List<ItemData>();

    [SerializeField] public int slotCount;

    private int slotIndex = 0;

    [Header("슬롯 생성")]
    [SerializeField] private InvenSlotUI slotPrefab;
    [SerializeField] private Transform slotParent;

    Player player;
    private ItemData utilityItemData;


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


        InitUtilityItemDataList();
    }


    public void Start()
    {
        //UnLockUtilitySlot();
 

        InitInventoryUI();
        RefreshSlotUI();
    }

    public void UnLockUtilitySlot() //각각의 특성 해금까지의 필요 포인트 도달 시 아이콘 활성화. 기본은 어둡게 깔려있음
    {
        foreach (var slot in slotList)
        {
            slot.itemIcon.color = Color.black;
        }
    }


    public void RefreshSlotUI()
    {
        foreach (var slot in slotList)
        {
            slot.RefreshUI();
        }
    }

    public ItemData GetItemData()
    {
        return utilityItemData;
    }

    public void InitUtilityItemDataList()
    {
        foreach (var slot in slotList)
        {
            var utilityItemData = GetItemData();

            utilityItemList.Add(utilityItemData);
            //var utilityItemData에 데이터를 넣어준 뒤, List<InvenSlotUI> slotList의 갯수만큼 데이터를 Add
        }
    }

    public void InitInventoryUI()
    {
        for (int i = 0; i < slotCount; i++)
        {
            Debug.Log($"슬롯 생성 중: {i}");
            InvenSlotUI newSlot = Instantiate(slotPrefab, slotParent);
            slotList.Add(newSlot);
        }
    }

    public void AddItem(ItemData ItemData, Player player)
    {
        if (slotIndex < slotList.Count)
        {
            slotList[slotIndex].SetItem(ItemData, player);
            slotIndex++;
        }
    }

    public void RefreshAllOwnPoints()
    {
        foreach (var slot in slotList)
        {
            slot.UpdateOwnPoint();
        }
    }
}
