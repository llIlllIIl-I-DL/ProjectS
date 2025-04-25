using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "UtilityItemList", menuName = "ScriptableObjects/UtilityItemList")]

public class UtilityItemList : ScriptableObject
{
    public List<ItemData> utilityItemList = new List<ItemData>();
    static Player player;

    public void GetUtility(Player player)
    {
        for (int i = 1; i < 16; i++)
        {
            ItemData utilityItemData100n = GetUtilityItemDataForList(1000 + i);

            CreatSlotSystem.Instance.AddItem(utilityItemData100n, player);
        }
    }

    public ItemData GetUtilityItemDataForList(int id)
    {
        ItemData utilityData = utilityItemList.Find(Data => Data.id == id);
        ItemData cloneData = Instantiate(utilityData);

        return cloneData;
    }
}