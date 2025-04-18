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

    [Header("복장 및 세트 관리")]
    // 현재 장착된 복장 아이템
    [SerializeField] private ItemData[] equippedCostumeParts = new ItemData[4]; // 헤드, 바디, 암즈, 레그

    public ItemData[] EquippedCostumeParts => equippedCostumeParts;

    // 현재 장착된 무기 속성
    [SerializeField] private ItemData equippedWeaponAttribute;

    public ItemData EquippedWeaponAttribute => equippedWeaponAttribute;
    // 복장 세트 참조
    [SerializeField] private List<CostumeSetData> availableCostumeSets = new List<CostumeSetData>();

    public List<CostumeSetData> AvailableCostumeSets => availableCostumeSets;
    [SerializeField] private CostumeSetData activeCostumeSet;

    public CostumeSetData ActiveCostumeSet => activeCostumeSet;

    [Header("연결된 매니저")]
    [SerializeField] private CostumeManager costumeManager;

    [Header("무기 인벤토리")]
    [SerializeField] private List<WeaponData> playerWeapons = new List<WeaponData>();
    [SerializeField] private int currentWeaponIndex = 0;
    
    [Header("인벤토리 설정")]
    [SerializeField] private int maxWeapons = 6;  // 최대 소지 가능 무기 수

    // 이벤트 델리게이트
    public delegate void ItemEventHandler(ItemData item);
    public event ItemEventHandler OnItemAdded;
    public event ItemEventHandler OnItemRemoved;
    public event ItemEventHandler OnItemUsed;
    public event ItemEventHandler OnItemEquipped;
    public event ItemEventHandler OnWeaponAttributeChanged;// 무기 속성 변경 이벤트
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

        // CostumeManager 참조 가져오기 (없을 경우)
        if (costumeManager == null)
        {
            costumeManager = CostumeManager.Instance;
        }

        // 복장 세트 정보 초기화
        InitializeCostumeSets();

        // 무기 목록이 비어있으면 경고
        if (playerWeapons.Count == 0)
        {
            Debug.LogWarning("InventoryManager: 플레이어가 소지한 무기가 없습니다!");
        }
        else
        {
            // 기본 무기 장착
            EquipWeapon(currentWeaponIndex);
        }
    }

    // ItemManager에서 아이템 로드
    private void LoadItemsFromItemManager()
    {
        if (ItemManager.Instance != null)
        {
            foreach (ItemData item in ItemManager.Instance.PlayerItems)
            {
                AddItemDirectly(item); // 순환 참조 방지를 위해 직접 메서드 사용
            }
        }
    }

    // 복장 세트 정보 초기화
    private void InitializeCostumeSets()
    {
        if (costumeManager != null)
        {
            // CostumeManager에서 복장 세트 정보 가져오기
            availableCostumeSets = costumeManager.GetAllCostumeSets();
        }
    }

    // 아이템 추가 (아이템 획득 시 호출)
    public bool AddItem(ItemData item, bool fromItemManager = false)
    {
        if (item == null) return false;

        // 전체 아이템 목록에 추가
        if (!allItems.Contains(item))
        {
            allItems.Add(item);
            AddItemToCollection(item);

            // 해당 아이템이 ItemManager에도 추가되도록 함
            // fromItemManager가 true인 경우 이미 ItemManager에서 호출된 것이므로 순환 참조 방지
            if (ItemManager.Instance != null && !fromItemManager)
            {
                ItemManager.Instance.AddItem(item);
            }

            // 코스튬 매니저에 알림
            if (costumeManager != null && item.itemType == ItemType.CostumeParts)
            {
                costumeManager.AddPart(item);
            }

            // 이벤트 발생
            OnItemAdded?.Invoke(item);

            Debug.Log($"인벤토리에 {item.ItemName} 아이템이 추가되었습니다.");
            return true;
        }

        return false;
    }

    // 직접 아이템 추가 (ItemManager.SyncInventory에서 호출)
    public bool AddItemDirectly(ItemData item)
    {
        if (item == null) return false;

        // 전체 아이템 목록에 추가
        if (!allItems.Contains(item))
        {
            allItems.Add(item);
            AddItemToCollection(item);

            // 코스튬 매니저에 알림
            if (costumeManager != null && item.itemType == ItemType.CostumeParts)
            {
                costumeManager.AddPart(item);
            }

            // 이벤트 발생
            OnItemAdded?.Invoke(item);

            Debug.Log($"인벤토리에 {item.ItemName} 아이템이 직접 추가되었습니다.");
            return true;
        }

        return false;
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
    public bool UseItem(ItemData item)
    {
        if (item == null || !allItems.Contains(item)) return false;

        bool success = false;

        // 사용 아이템 로직
        if (item.itemType == ItemType.UsableItem)
        {
            if (ItemManager.Instance != null)
            {
                ItemManager.Instance.UseItem(item);
                success = true;

                // 소모성 아이템이면 제거
                if (success && item.isConsumable)
                {
                    RemoveItem(item);
                }

                // 이벤트 발생
                OnItemUsed?.Invoke(item);
            }
        }

        return success;
    }

    // 아이템 제거
    public bool RemoveItem(ItemData item)
    {
        if (item == null || !allItems.Contains(item)) return false;

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

        // 이벤트 발생
        OnItemRemoved?.Invoke(item);

        return true;
    }

    // 무기 속성 장착
    public bool EquipWeaponAttribute(ItemData weaponAttribute)
    {
        if (weaponAttribute == null || !weaponAttributes.Contains(weaponAttribute)) return false;

        // 이전 속성과 다를 때만 이벤트 발생
        if (equippedWeaponAttribute != weaponAttribute)
        {
            equippedWeaponAttribute = weaponAttribute;
            
            // 무기 속성 장착 이벤트 발생
            OnItemEquipped?.Invoke(weaponAttribute);
            
            Debug.Log($"무기 속성 장착: {weaponAttribute.ItemName}");
        }
        
        return true;
    }

    // 코스튬 파츠 장착
    public bool EquipCostumePart(ItemData costumePart, int slotIndex)
    {
        if (costumePart == null || !costumeParts.Contains(costumePart)) return false;
        if (slotIndex < 0 || slotIndex >= equippedCostumeParts.Length) return false;

        // 파츠 타입에 맞는 슬롯 인덱스로 변환
        int partTypeIndex = GetSlotIndexFromPartType(costumePart.partsType);
        if (partTypeIndex >= 0)
        {
            equippedCostumeParts[partTypeIndex] = costumePart;

            // 이벤트 발생
            OnItemEquipped?.Invoke(costumePart);

            Debug.Log($"슬롯 {partTypeIndex}에 {costumePart.ItemName} 코스튬을 장착했습니다.");

            // 복장 세트 완성 여부 확인
            CheckCostumeSetCompletion();

            return true;
        }

        return false;
    }

    // 파츠 타입을 슬롯 인덱스로 변환
    private int GetSlotIndexFromPartType(PartsType partType)
    {
        switch (partType)
        {
            case PartsType.Head: return 0;
            case PartsType.Body: return 1;
            case PartsType.Arms: return 2;
            case PartsType.Legs: return 3;
            default: return -1;
        }
    }

    // 복장 세트 완성 여부 확인
    private void CheckCostumeSetCompletion()
    {
        foreach (CostumeSetData setData in availableCostumeSets)
        {
            if (IsSetComplete(setData) && !setData.isUnlocked)
            {
                // 세트 해금 처리
                setData.isUnlocked = true;

                // CostumeManager에 알림
                if (costumeManager != null)
                {
                    costumeManager.ActivateCostume(setData.costumeId);
                }

                Debug.Log($"{setData.costumeName} 세트가 완성되었습니다!");
                activeCostumeSet = setData;
                return;
            }
        }
    }

    // 세트 완성 여부 확인 헬퍼 메서드
    private bool IsSetComplete(CostumeSetData setData)
    {
        // 필요한 모든 파츠가 장착되어 있는지 확인
        foreach (ItemData requiredPart in setData.requiredParts)
        {
            bool found = false;
            foreach (ItemData equippedPart in equippedCostumeParts)
            {
                if (equippedPart != null && equippedPart.id == requiredPart.id)
                {
                    found = true;
                    break;
                }
            }

            if (!found) return false;
        }

        return true;
    }

    // 활성화된 복장 세트 변경
    public bool SetActiveCostumeSet(CostumeSetData costumeSet)
    {
        if (costumeSet == null || !costumeSet.isUnlocked) return false;

        activeCostumeSet = costumeSet;

        // CostumeManager에도 알림
        if (costumeManager != null)
        {
            costumeManager.ActivateCostume(costumeSet.costumeId);
        }

        return true;
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

    // 특정 파츠 타입의 장착된 아이템 가져오기
    public ItemData GetEquippedPartByType(PartsType partType)
    {
        int index = GetSlotIndexFromPartType(partType);
        if (index >= 0 && index < equippedCostumeParts.Length)
        {
            return equippedCostumeParts[index];
        }
        return null;
    }

    // 현재 활성화된 복장 세트 반환
    public CostumeSetData GetActiveCostumeSet()
    {
        return activeCostumeSet;
    }

    // 사용 가능한 모든 복장 세트 반환
    public List<CostumeSetData> GetAvailableCostumeSets()
    {
        return availableCostumeSets;
    }

    // 복장 세트 해금 여부 확인
    public bool IsCostumeSetUnlocked(string costumeId)
    {
        CostumeSetData set = availableCostumeSets.Find(s => s.costumeId == costumeId);
        return set != null && set.isUnlocked;
    }

    // 현재 장착된 무기 속성 getter 추가
    public ItemData GetCurrentWeaponAttribute()
    {
        return equippedWeaponAttribute;
    }

    // 특정 인덱스의 무기 장착
    public void EquipWeapon(int index)
    {
        if (index < 0 || index >= playerWeapons.Count)
        {
            Debug.LogError($"InventoryManager: 잘못된 무기 인덱스 - {index}");
            return;
        }
        
        currentWeaponIndex = index;
        WeaponManager.Instance.EquipWeapon(playerWeapons[index]);
        Debug.Log($"무기 장착: {playerWeapons[index].weaponName}");
    }
    
    // 다음 무기 가져오기
    public WeaponData GetNextWeapon()
    {
        if (playerWeapons.Count <= 1) return null;
        
        int nextIndex = (currentWeaponIndex + 1) % playerWeapons.Count;
        currentWeaponIndex = nextIndex;
        
        return playerWeapons[nextIndex];
    }
    
    // 이전 무기 가져오기
    public WeaponData GetPreviousWeapon()
    {
        if (playerWeapons.Count <= 1) return null;
        
        int prevIndex = (currentWeaponIndex - 1 + playerWeapons.Count) % playerWeapons.Count;
        currentWeaponIndex = prevIndex;
        
        return playerWeapons[prevIndex];
    }
    
    // 새 무기 추가 (아이템 획득 등)
    public bool AddWeapon(WeaponData newWeapon)
    {
        // 이미 같은 무기를 가지고 있는지 확인
        if (playerWeapons.Contains(newWeapon))
        {
            Debug.Log($"이미 {newWeapon.weaponName} 무기를 소지하고 있습니다.");
            return false;
        }
        
        // 인벤토리가 가득 찼는지 확인
        if (playerWeapons.Count >= maxWeapons)
        {
            Debug.Log("무기 인벤토리가 가득 찼습니다!");
            return false;
        }
        
        // 새 무기 추가
        playerWeapons.Add(newWeapon);
        Debug.Log($"새 무기 획득: {newWeapon.weaponName}");
        return true;
    }
    
    // 무기 제거
    public bool RemoveWeapon(WeaponData weaponToRemove)
    {
        if (!playerWeapons.Contains(weaponToRemove))
        {
            Debug.Log($"{weaponToRemove.weaponName} 무기를 소지하고 있지 않습니다.");
            return false;
        }
        
        int removeIndex = playerWeapons.IndexOf(weaponToRemove);
        playerWeapons.Remove(weaponToRemove);
        
        // 제거된 무기가 현재 장착 중이었다면 다른 무기로 교체
        if (removeIndex == currentWeaponIndex)
        {
            if (playerWeapons.Count > 0)
            {
                currentWeaponIndex = 0;
                WeaponManager.Instance.EquipWeapon(playerWeapons[0]);
            }
        }
        // 현재 인덱스보다 앞에 있는 무기가 제거되었다면 인덱스 조정
        else if (removeIndex < currentWeaponIndex)
        {
            currentWeaponIndex--;
        }
        
        Debug.Log($"무기 제거: {weaponToRemove.weaponName}");
        return true;
    }
    
    // 현재 장착 무기 인덱스 가져오기
    public int GetCurrentWeaponIndex() => currentWeaponIndex;
    
    // 특정 인덱스의 무기 가져오기
    public WeaponData GetWeaponAt(int index)
    {
        if (index < 0 || index >= playerWeapons.Count)
        {
            return null;
        }
        return playerWeapons[index];
    }
    
    // 현재 무기 가져오기
    public WeaponData GetCurrentWeapon()
    {
        if (playerWeapons.Count == 0)
        {
            return null;
        }
        return playerWeapons[currentWeaponIndex];
    }
    
    // 모든 소지 무기 목록 가져오기
    public List<WeaponData> GetAllWeapons() => new List<WeaponData>(playerWeapons);
}