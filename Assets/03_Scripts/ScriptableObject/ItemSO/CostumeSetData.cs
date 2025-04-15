using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Costume Set", menuName = "ScriptableObjects/Costume Set")]
public class CostumeSetData : ScriptableObject
{
    [Header("세트 기본 정보")]
    [SerializeField] private string setName;
    public string SetName { get { return setName; } set { setName = value; } }
    
    [TextArea(3, 5)]
    [SerializeField] private string description;
    public string Description { get { return description; } set { description = value; } }
    
    [Header("세트 구성 파츠")]
    [Tooltip("이 세트를 구성하는 복장 파츠 아이템들 (4개)")]
    [SerializeField] private ItemData[] costumeParts = new ItemData[4];
    public ItemData[] CostumeParts { get { return costumeParts; } }
    
    [Header("세트 보상")]
    [Tooltip("이 세트를 완성했을 때 받는 보상")]
    [SerializeField] private ItemData setReward;
    public ItemData SetReward { get { return setReward; } }
    
    // 모든 파츠가 있는지 확인
    public bool IsComplete(List<ItemData> playerItems)
    {
        foreach (ItemData part in costumeParts)
        {
            if (part == null) continue;
            
            bool hasItem = false;
            foreach (ItemData playerItem in playerItems)
            {
                if (playerItem.id == part.id)
                {
                    hasItem = true;
                    break;
                }
            }
            
            if (!hasItem) return false;
        }
        
        return true;
    }
    
    // 특정 슬롯의 파츠 반환
    public ItemData GetPartAtSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= costumeParts.Length)
            return null;
            
        return costumeParts[slotIndex];
    }
} 