using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    private static InventoryManager instance;
    public static InventoryManager Instance => instance;

    [Header("인벤토리 아이템")]
    [SerializeField] private List<ItemData> allItems = new List<ItemData>();
    [SerializeField] private List<ItemData> weaponAttributes = new List<ItemData>();
    [SerializeField] private List<ItemData> costumeParts = new List<ItemData>();
    [SerializeField] private List<ItemData> usableItems = new List<ItemData>();

    [Header("이벤트 통지")]
    // UI 업데이트를 위한 참조
    [SerializeField] private CostumeSetManager costumeSetManager;

    // 현재 장착된 복장 아이템
    [SerializeField] private ItemData[] equippedCostumeParts = new ItemData[4]; // 헤드, 바디, 레그, 액세서리

    // 현재 장착된 무기 속성
    [SerializeField] private ItemData equippedWeaponAttribute;

    // 복장 세트 참조 추가
    [SerializeField] private List<CostumeSetData> availableCostumeSets = new List<CostumeSetData>();
    [SerializeField] private CostumeSetData activeCostumeSet;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // ItemManager와 연동 (기존 아이템 로드)
        LoadItemsFromItemManager();
    }

    // ItemManager에서 아이템 로드
    private void LoadItemsFromItemManager()
    {
        if (ItemManager.Instance != null)
        {
            foreach (ItemData item in ItemManager.Instance.PlayerItems)
            {
                AddItemToCollection(item);
            }
        }
    }

    // 아이템 추가 (아이템 획득 시 호출)
    public void AddItem(ItemData item)
    {
        if (item == null) return;

        // 전체 아이템 목록에 추가
        if (!allItems.Contains(item))
        {
            allItems.Add(item);
            AddItemToCollection(item);
            
            // 해당 아이템이 ItemManager에도 추가되도록 함
            if (ItemManager.Instance != null)
            {
                ItemManager.Instance.AddItem(item);
            }
            
            // 코스튬 매니저에 알림
            if (costumeSetManager != null)
            {
                costumeSetManager.OnItemAcquired(item);
            }

            Debug.Log($"인벤토리에 {item.ItemName} 아이템이 추가되었습니다.");
        }
    }

    // 아이템 타입에 따라 해당 컬렉션에 추가
    private void AddItemToCollection(ItemData item)
    {
        switch (item.itemType)
        {
            case ItemType.WeaponAttribute:
                if (!weaponAttributes.Contains(item))
                    weaponAttributes.Add(item);
                break;
            case ItemType.CostumeParts:
                if (!costumeParts.Contains(item))
                    costumeParts.Add(item);
                break;
            case ItemType.UsableItem:
                if (!usableItems.Contains(item))
                    usableItems.Add(item);
                break;
        }
    }

    // 아이템 사용 (소모성 아이템)
    public void UseItem(ItemData item)
    {
        if (item == null || !allItems.Contains(item)) return;

        // 사용 아이템 로직
        if (item.itemType == ItemType.UsableItem)
        {
            if (ItemManager.Instance != null)
            {
                ItemManager.Instance.UseItem(item);
                
                // 소모성 아이템이면 제거
                if (item.isConsumable)
                {
                    RemoveItem(item);
                }
            }
        }
    }

    // 아이템 제거
    public void RemoveItem(ItemData item)
    {
        if (item == null) return;

        allItems.Remove(item);
        
        switch (item.itemType)
        {
            case ItemType.WeaponAttribute:
                weaponAttributes.Remove(item);
                // 장착된 무기 속성이면 장착 해제
                if (equippedWeaponAttribute == item)
                    equippedWeaponAttribute = null;
                break;
            case ItemType.CostumeParts:
                costumeParts.Remove(item);
                // 장착된 코스튬이면 장착 해제
                for (int i = 0; i < equippedCostumeParts.Length; i++)
                {
                    if (equippedCostumeParts[i] == item)
                        equippedCostumeParts[i] = null;
                }
                break;
            case ItemType.UsableItem:
                usableItems.Remove(item);
                break;
        }
        
        // 코스튬 매니저에 알림
        if (costumeSetManager != null)
        {
            costumeSetManager.OnItemRemoved(item);
        }
    }

    // 무기 속성 장착
    public void EquipWeaponAttribute(ItemData weaponAttribute)
    {
        if (weaponAttribute == null || !weaponAttributes.Contains(weaponAttribute)) return;
        
        equippedWeaponAttribute = weaponAttribute;
        Debug.Log($"{weaponAttribute.ItemName} 무기 속성을 장착했습니다.");
    }

    // 코스튬 파츠 장착
    public void EquipCostumePart(ItemData costumePart, int slotIndex)
    {
        if (costumePart == null || !costumeParts.Contains(costumePart)) return;
        if (slotIndex < 0 || slotIndex >= equippedCostumeParts.Length) return;
        
        equippedCostumeParts[slotIndex] = costumePart;
        Debug.Log($"슬롯 {slotIndex}에 {costumePart.ItemName} 코스튬을 장착했습니다.");
        
        // 복장 세트 완성 여부 확인
        CheckCostumeSetCompletion();
    }

    // 복장 세트 완성 여부 확인 메서드 추가
    private void CheckCostumeSetCompletion()
    {
        foreach (CostumeSetData setData in availableCostumeSets)
        {
            if (IsSetComplete(setData))
            {
                Debug.Log($"{setData.SetName} 세트가 완성되었습니다!");
                activeCostumeSet = setData;
                
                // 세트 보상이 있으면 지급
                if (setData.SetReward != null)
                {
                    AddItem(setData.SetReward);
                }
                
                return;
            }
        }
        
        // 완성된 세트가 없음
        activeCostumeSet = null;
    }

    // 세트 완성 여부 확인 헬퍼 메서드
    private bool IsSetComplete(CostumeSetData setData)
    {
        int matchCount = 0;
        
        for (int i = 0; i < equippedCostumeParts.Length; i++)
        {
            if (equippedCostumeParts[i] != null && i < setData.CostumeParts.Length)
            {
                if (equippedCostumeParts[i].id == setData.CostumeParts[i].id)
                {
                    matchCount++;
                }
            }
        }
        
        return matchCount == 4; // 4개 파츠가 모두 일치해야 세트 완성
    }

    // 현재 장착된 무기 속성 반환
    public ItemData GetEquippedWeaponAttribute()
    {
        return equippedWeaponAttribute;
    }

    // 인벤토리에 있는 모든 아이템 반환
    public List<ItemData> GetAllItems()
    {
        return allItems;
    }

    // 복장 파츠 목록 반환
    public List<ItemData> GetCostumeParts()
    {
        return costumeParts;
    }

    // 무기 속성 목록 반환
    public List<ItemData> GetWeaponAttributes()
    {
        return weaponAttributes;
    }

    // 소비 아이템 목록 반환
    public List<ItemData> GetUsableItems()
    {
        return usableItems;
    }

    // 특정 유형의 아이템 찾기
    public List<ItemData> GetItemsByType(ItemType itemType)
    {
        return allItems.Where(item => item.itemType == itemType).ToList();
    }

    // ID로 아이템 찾기
    public ItemData GetItemById(int id)
    {
        return allItems.Find(item => item.id == id);
    }

    // 현재 장착된 복장 파츠 반환 메서드 추가
    public ItemData[] GetEquippedCostumeParts()
    {
        return equippedCostumeParts;
    }

    // 현재 활성화된 복장 세트 반환 메서드 추가  
    public CostumeSetData GetActiveCostumeSet()
    {
        return activeCostumeSet;
    }
} 