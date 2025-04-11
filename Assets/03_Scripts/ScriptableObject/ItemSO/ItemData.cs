using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemAttrivuteType
{
    HealItem,
    MaxHPUpItem
}


[CreateAssetMenu(fileName = "New Item", menuName = "ScriptableObjects/Item")]
public class ItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemName;
    public Sprite itemIcon;
    public ItemAttrivuteType itemAttrivuteType;
    [TextArea(3, 5)]
    public string description;

    [Header("효과 수치")]
    public float effectValue;

    [Header("아이템 설정")]
    public bool isConsumable = true;
    public bool isStackable = true;
    public int maxStackAmount = 10;
} 