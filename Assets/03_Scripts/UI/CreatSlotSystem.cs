using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

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

        InitInventoryUI();
    }


    void Start()
    {
        InitUtilityItemDataList();

        RefreshSlotUI();
    }

    public void RefreshSlotUI()
    {
        foreach (var slot in slotList)
        {
            slot.RefreshUI();
        }
    }

    public void InitUtilityItemDataList()
    {
        foreach (var slot in slotList)
        {
            var utilityItemData = slot.GetUtilityItemData();

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



  

    public void AddItem(ItemData ItemData)
    {
        if (slotIndex < slotList.Count)
        {
            slotList[slotIndex].SetItem(ItemData);
            slotIndex++;
        }
    }
}
