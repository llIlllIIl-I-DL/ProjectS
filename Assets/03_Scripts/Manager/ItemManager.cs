using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    private static ItemManager instance;
    public static ItemManager Instance => instance;

    [SerializeField] private List<ItemData> playerItems = new List<ItemData>();
    [SerializeField] private Transform itemParent; // 아이템 부모 오브젝트

    private PlayerHP playerHP;
    private InventoryManager inventoryManager;
    private CostumeManager costumeManager;

    // 아이템 인벤토리 접근을 위한 속성
    public List<ItemData> PlayerItems => playerItems;
    public int ItemCount => playerItems.Count;

    // 이벤트 델리게이트
    public delegate void ItemEventHandler(ItemData item);
    public event ItemEventHandler OnItemAdded;
    public event ItemEventHandler OnItemRemoved;
    public event ItemEventHandler OnItemUsed;

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
        playerHP = FindObjectOfType<PlayerHP>();
        if (playerHP == null)
        {
            Debug.LogWarning("플레이어 HP 컴포넌트를 찾을 수 없습니다!");
        }

        // 다른 매니저 참조 가져오기
        inventoryManager = InventoryManager.Instance;
        costumeManager = CostumeManager.Instance;

        // 이미 소유한 아이템을 인벤토리에 동기화
        SyncInventory();
    }

    // 인벤토리 매니저와 동기화
    private void SyncInventory()
    {
        if (inventoryManager != null)
        {
            // 원본 목록의 복사본을 만들어 반복
            List<ItemData> itemsToSync = new List<ItemData>(playerItems);
            
            foreach (ItemData item in itemsToSync)
            {
                // 중요: 순환 참조를 방지하기 위해 AddItem이 아닌 AddItemToCollection 메서드 직접 호출
                inventoryManager.AddItemDirectly(item);
            }
        }
    }

    // 아이템 추가
    public bool AddItem(ItemData itemData, bool fromInventory = false)
    {
        if (itemData == null) return false;

        if (itemData.itemType != ItemType.UtilityPoint) //UtilityPoint는 계속해서 획득 할 수 있어야 하니까...
        {
            // 중복 아이템 체크
            if (playerItems.Contains(itemData))
            {
                return false; // 이미 있는 아이템이면 추가하지 않음
            }
        }

        // 아이템 목록에 추가
        playerItems.Add(itemData);

        // 인벤토리 매니저에 동기화
        // fromInventory가 true인 경우 InventoryManager에서 호출된 것이므로 순환 참조 방지
        if (inventoryManager != null && !fromInventory)
        {
            inventoryManager.AddItem(itemData, true);
        }

        // 파츠 아이템인 경우 복장 매니저에 알림
        if (itemData.itemType == ItemType.CostumeParts && costumeManager != null)
        {
            costumeManager.AddPart(itemData);
        }

        // 이벤트 발생
        OnItemAdded?.Invoke(itemData);

        Debug.Log($"{itemData.ItemName} 아이템을 획득했습니다!");
        return true;
    }

    // 아이템 사용
    public bool UseItem(ItemData itemData)
    {
        if (itemData == null || !playerItems.Contains(itemData)) return false;

        bool success = false;

        // 아이템 타입에 따른 처리
        switch (itemData.itemType)
        {
            case ItemType.UsableItem:
                // 사용 아이템 효과 적용
                success = ApplyUsableItemEffect(itemData);
                break;

            case ItemType.WeaponAttribute:
                // 무기 속성 적용 로직
                success = EquipWeaponAttribute(itemData);
                break;

            case ItemType.CostumeParts:
                // 복장 파츠 적용 로직
                success = EquipCostumePart(itemData);
                break;

            default:
                Debug.LogWarning($"처리되지 않은 아이템 유형: {itemData.itemType}");
                return false;
        }

        if (success)
        {
            // 이벤트 발생
            OnItemUsed?.Invoke(itemData);

            // 소모성 아이템이면 사용 후 제거
            if (itemData.isConsumable)
            {
                RemoveItem(itemData);
            }
        }

        return success;
    }

    // 사용 아이템 효과 적용
    private bool ApplyUsableItemEffect(ItemData itemData)
    {
        if (itemData.itemUsageType != ItemUsageType.InstantUse) return false;

        switch (itemData.itemAttributeType)
        {
            case ItemAttributeType.HealItem:
                return UseHealItem(itemData);

            case ItemAttributeType.MaxHPUpItem:
                return UseMaxHPUpItem(itemData);

            default:
                Debug.LogWarning($"처리되지 않은 아이템 속성: {itemData.itemAttributeType}");
                return false;
        }
    }

    // 무기 속성 장착
    private bool EquipWeaponAttribute(ItemData itemData)
    {
        if (inventoryManager != null)
        {
            return inventoryManager.EquipWeaponAttribute(itemData);
        }
        return false;
    }

    // 복장 파츠 장착
    private bool EquipCostumePart(ItemData itemData)
    {
        if (inventoryManager != null)
        {
            // 파츠 타입에 맞는 슬롯 인덱스 가져오기
            int slotIndex = GetSlotIndexFromPartType(itemData.partsType);
            if (slotIndex >= 0)
            {
                return inventoryManager.EquipCostumePart(itemData, slotIndex);
            }
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

    // 아이템 제거
    public bool RemoveItem(ItemData itemData)
    {
        if (itemData == null || !playerItems.Contains(itemData)) return false;

        // 아이템 목록에서 제거
        playerItems.Remove(itemData);

        // 인벤토리 매니저에 동기화
        if (inventoryManager != null)
        {
            inventoryManager.RemoveItem(itemData);
        }

        // 이벤트 발생
        OnItemRemoved?.Invoke(itemData);

        return true;
    }

    // 힐링 아이템 사용
    private bool UseHealItem(ItemData itemData)
    {
        if (playerHP == null) return false;

        playerHP.Heal(itemData.effectValue);
        Debug.Log($"{itemData.ItemName}을(를) 사용해 {itemData.effectValue}만큼 체력을 회복했습니다.");
        return true;
    }

    // 최대 HP 증가 아이템 사용
    private bool UseMaxHPUpItem(ItemData itemData)
    {
        if (playerHP == null) return false;

        playerHP.IncreaseMaxHP(itemData.effectValue);
        Debug.Log($"{itemData.ItemName}을(를) 사용해 최대 체력이 {itemData.effectValue}만큼 증가했습니다.");
        return true;
    }

    // 특정 유형의 아이템 가져오기
    public ItemData GetItemByAttributeType(ItemAttributeType itemAttributeType)
    {
        return playerItems.Find(item => item.itemAttributeType == itemAttributeType);
    }

    // 특정 아이템 타입의 아이템 목록 가져오기
    public List<ItemData> GetItemsByType(ItemType itemType)
    {
        return playerItems.FindAll(item => item.itemType == itemType);
    }

    // 특정 이름의 아이템 가져오기
    public ItemData GetItemByName(string itemName)
    {
        return playerItems.Find(item => item.ItemName == itemName);
    }

    // ID로 아이템 가져오기
    public ItemData GetItemById(int id)
    {
        return playerItems.Find(item => item.id == id);
    }

    // 아이템 생성 메서드 (씬에 아이템 오브젝트 실제 생성)
    public GameObject SpawnItem(ItemData itemData, Vector3 position)
    {
        if (itemData == null)
        {
            Debug.LogError("생성할 아이템 데이터가 null입니다!");
            return null;
        }

        // 아이템 프리팹 로드
        GameObject itemPrefab = Resources.Load<GameObject>("Prefabs/Item");
        if (itemPrefab == null)
        {
            Debug.LogError("아이템 프리팹을 찾을 수 없습니다!");
            return null;
        }

        GameObject itemObject = Instantiate(itemPrefab, position, Quaternion.identity);
        if (itemParent != null)
        {
            itemObject.transform.SetParent(itemParent);
        }

        // 아이템 오브젝트에 데이터 설정
        Item itemComponent = itemObject.GetComponent<Item>();
        if (itemComponent != null)
        {
            itemComponent.SetItemData(itemData);
        }
        else
        {
            Debug.LogError("생성된 아이템 오브젝트에 Item 컴포넌트가 없습니다!");
        }

        return itemObject;
    }

    // 아이템 목록 초기화 (게임 리셋 등에 사용)
    public void ClearItems()
    {
        playerItems.Clear();
        OnItemRemoved?.Invoke(null); // null은 모든 아이템이 제거되었음을 의미
    }
}