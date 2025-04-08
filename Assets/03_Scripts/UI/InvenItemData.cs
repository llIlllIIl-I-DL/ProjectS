using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ItemType
{
    Equip_Type,
    Attribute_Type
}

[CreateAssetMenu(fileName = "Item Data", menuName = "Scriptable Object/Item Data")]

public class InvenItemData : ScriptableObject
{
    public ItemType ItemType;

    [SerializeField]
    private string itemName;
    public string ItemName { get { return itemName; } }

    public Sprite Icon;

    [SerializeField]
    private string itemdescription;
    public string ItemDescription { get { return itemdescription; } }

    public int id;
}
