using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ItemAttributeType // 즉시사용 아이템 효과 종류
{
    HealItem,
    MaxHPUpItem
}

public enum ItemType // 아이템 주요 분류
{
    WeaponAttribute, // 무기속성
    CostumeParts,   // 복장파츠
    UsableItem      // 사용 아이템
}

public enum ItemUsageType // 사용 아이템 사용 방식
{
    InstantUse,     // 즉시사용 아이템
    StoredInInventory // 인벤토리에 저장되는 아이템
}

[CreateAssetMenu(fileName = "New Item", menuName = "ScriptableObjects/Item")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string itemName;
    public string ItemName { get { return itemName; } set { itemName = value; } }
    
    [SerializeField] private Sprite itemIcon;
    public Sprite Icon { get { return itemIcon; } set { itemIcon = value; } }
    
    public ItemType itemType;
    
    [Header("사용 아이템 설정")]
    [Tooltip("아이템 타입이 UsableItem일 경우에만 사용")]
    public ItemUsageType itemUsageType;
    [Tooltip("즉시사용 아이템의 효과 종류")]
    public ItemAttributeType itemAttributeType;
    
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