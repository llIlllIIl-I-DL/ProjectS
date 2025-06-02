using System.Collections.Generic;
using UnityEngine;


public enum ItemAttributeType // 즉시사용 아이템 효과 종류
{
    HealItem,
    MaxHPUpItem
}
public enum ItemType // 아이템 주요 분류
{
    WeaponAttribute, // 무기속성
    CostumeParts,   // 복장파츠
    UsableItem,      // 사용 아이템
    UtilityItem,    //특성 아이템(해금 필요)
    UtilityPoint    //특성 해금용 포인트
}
public enum ItemUsageType // 사용 아이템 사용 방식
{
    InstantUse,     // 즉시사용 아이템
    StoredInInventory // 인벤토리에 저장되는 아이템
}

public enum PartsType
{
    None,
    Head,
    Body,
    Arms,
    Legs
}
public enum ElementType
{
    None,
    Normal,
    Rust,
    Iron,
    Poison,
    Water,
    Flame,
    Ice
}

public enum AttributeType
{
    MaxHPUP,
    MPUP,
}


[CreateAssetMenu(fileName = "New Item", menuName = "ScriptableObjects/Item")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    [SerializeField] private string itemName;
    public string ItemName { get { return itemName; } set { itemName = value; } }

    [SerializeField] private Sprite itemIcon;
    public Sprite Icon { get { return itemIcon; } set { itemIcon = value; } }

    [SerializeField] private Sprite unLockedItemIcon;
    public Sprite UnLockedIcon { get { return unLockedItemIcon; } set { unLockedItemIcon = value; } }

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

    [Header("파츠 아이템 설정")]
    [Tooltip("아이템 타입이 CostumeParts일 경우에만 사용")]
    public PartsType partsType = PartsType.None;
    public string costumeSetId; // 이 파츠가 속한 복장 세트 ID

    [Header("무기 아이템 설정")]
    [Tooltip("아이템 타입이 WeaponAttribute일 경우에만 사용")]
    public ElementType elementType; // 무기 속성
    public int damage;
    public float attackSpeed;

    [Header("특성 포인트 획득 수치")]
    [Tooltip("아이템 타입이 UtilityPoint일 경우에만 사용")]
    public int utilityPointForNow;

    [Header("특성 종류")]
    public AttributeType attributeType;

    [Header("특성 해금 포인트")]
    [Tooltip("아이템 타입이 UtilityItem일 경우에만 사용")]
    public int utilityPointForUnLock;
}