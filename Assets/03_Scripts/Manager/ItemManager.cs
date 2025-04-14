using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    private static ItemManager instance;
    public static ItemManager Instance => instance;

    [SerializeField] private List<ItemData> playerItems = new List<ItemData>();
    [SerializeField] private Transform itemParent;// 아이템 부모 오브젝트

    private PlayerHP playerHP;

    // 아이템 인벤토리 접근을 위한 속성
    public List<ItemData> PlayerItems => playerItems;
    public int ItemCount => playerItems.Count;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
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
            Debug.LogError("플레이어 HP 컴포넌트를 찾을 수 없습니다!");
        }
    }

    public void AddItem(ItemData itemData)
    {
        if (itemData == null) return;
        
        playerItems.Add(itemData);
        Debug.Log($"{itemData.ItemName} 아이템을 획득했습니다!");
    }

    public void UseItem(ItemData itemData)
    {
        if (itemData == null || !playerItems.Contains(itemData)) return;

        switch (itemData.itemAttributeType)
        {
            case ItemAttributeType.HealItem:
                UseHealItem(itemData);
                break;
                
            case ItemAttributeType.MaxHPUpItem:
                UseMaxHPUpItem(itemData);
                break;
                
            default:
                Debug.LogWarning($"처리되지 않은 아이템 유형: {itemData.itemAttributeType}");
                break;
        }

        // 소모성 아이템이면 사용 후 제거
        if (itemData.isConsumable)
        {
            playerItems.Remove(itemData);
        }
    }

    
    // 특정 유형의 아이템 가져오기
    public ItemData GetItemByType(ItemAttributeType itemAttrivuteType)
    {
        return playerItems.Find(item => item.itemAttributeType == itemAttrivuteType);
    }
    
    // 특정 이름의 아이템 가져오기
    public ItemData GetItemByName(string itemName)
    {
        return playerItems.Find(item => item.ItemName == itemName);
    }

    private void UseHealItem(ItemData itemData)
    {
        if (playerHP == null) return;
        
        playerHP.Heal(itemData.effectValue);
        Debug.Log($"{itemData.ItemName}을(를) 사용해 {itemData.effectValue}만큼 체력을 회복했습니다.");
    }

    private void UseMaxHPUpItem(ItemData itemData)
    {
        if (playerHP == null) return;
        
        playerHP.IncreaseMaxHP(itemData.effectValue);
        Debug.Log($"{itemData.ItemName}을(를) 사용해 최대 체력이 {itemData.effectValue}만큼 증가했습니다.");
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
} 