using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "UtilityItemList", menuName = "ScriptableObjects/UtilityItemList")]

public class UtilityItemList : ScriptableObject
{
    public List<ItemData> utilityItemList = new List<ItemData>();

    public void GetUtility()
    {
        for (int i = 1; i < 16; i++)
        {
            ItemData utilityItemData10n = GetUtilityItemDataForList(100 + i);

            CreatSlotSystem.Instance.AddItem(utilityItemData10n);
        }
    }

    public ItemData GetUtilityItemDataForList(int id)
    {
        ItemData utilityData = utilityItemList.Find(Data => Data.id == id);
        ItemData cloneData = Instantiate(utilityData);

        return cloneData;
    }
}