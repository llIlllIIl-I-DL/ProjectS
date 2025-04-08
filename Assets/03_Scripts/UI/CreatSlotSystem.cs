using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CreatSlotSystem : Singleton<CreatSlotSystem>
{
    public List<InvenSlotUI> slotList = new List<InvenSlotUI>();

    [Header("슬롯 생성 부모")]
    [SerializeField] private InvenSlotUI slotPrefab;
    [SerializeField] private Transform slotParent;

    void Start()
    {
        InitInventoryUI();
    }


    public void InitInventoryUI()
    {
        int slotCount = 8;

        for (int i = 0; i < slotCount; i++)
        {
            Debug.Log($"슬롯 생성 중: {i}");
            InvenSlotUI newSlot = Instantiate(slotPrefab, slotParent);
            slotList.Add(newSlot);
        }
    }

}
