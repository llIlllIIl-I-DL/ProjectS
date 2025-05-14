using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
// using VInspector.Libs;

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

        player = FindObjectOfType<Player>();

        InitInventoryUI();
        InitUtilityItemDataList();
    }


    public void Start()
    {
        RefreshSlotUI();
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
            // 나쁜건 아니지만 이미 만들어두고 사용하면 굳이 List 로 쓰는 이유가??
            // 배열이면 맞는 동작, 리스트면 좀 번거로울 것 같다. - 다만 메모리점으로는 이점이 있음
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

        player.UpdateCurrentInventory();
    }
}
