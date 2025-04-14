using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ItemAttrivuteType
{
    HealItem,
    MaxHPUpItem
}

public enum ItemType
{
    Equip_Type,
    Attribute_Type
}

[CreateAssetMenu(fileName = "New Item", menuName = "ScriptableObjects/Item")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string itemName;
    public string ItemName { get { return itemName; } set { itemName = value; } }
    
    [SerializeField] private Sprite itemIcon;
    public Sprite Icon { get { return itemIcon; } set { itemIcon = value; } }
    
    public ItemAttrivuteType itemAttrivuteType;
    public ItemType itemType;
    
    [TextArea(3, 5)]
    [SerializeField] private string description;
    public string ItemDescription { get { return description; } set { description = value; } }
    
    public int id;

    [Header("효과 수치")]
    public float effectValue;

    [Header("아이템 설정")]
    public bool isConsumable = true;
    public bool isStackable = true;
    public int maxStackAmount = 10;
} 