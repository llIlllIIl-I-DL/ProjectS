using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CostumeSetManager : MonoBehaviour
{
    [Header("복장 세트 데이터")]
    [SerializeField] private List<CostumeSetData> costumeSets = new List<CostumeSetData>();
    
    [Header("UI 요소")]
    [SerializeField] private GameObject costumeSetPrefab; // CostumeSetUI 컴포넌트를 가진 프리팹
    [SerializeField] private Transform costumeSetContainer; // 세트 UI를 담을 부모 오브젝트
    
    [Header("참조")]
    [SerializeField] private InventoryManager inventoryManager; // 인벤토리 매니저 참조
    
    private List<CostumeSetUI> costumeSetUIs = new List<CostumeSetUI>();
    
    private void Start()
    {
        InitializeCostumeSets();
        UpdateAllSetUIs();
    }
    
    // 복장 세트 UI 초기화
    private void InitializeCostumeSets()
    {
        // 기존 UI 정리
        foreach (Transform child in costumeSetContainer)
        {
            Destroy(child.gameObject);
        }
        costumeSetUIs.Clear();
        
        // 세트별 UI 생성
        foreach (CostumeSetData setData in costumeSets)
        {
            GameObject setObj = Instantiate(costumeSetPrefab, costumeSetContainer);
            CostumeSetUI setUI = setObj.GetComponent<CostumeSetUI>();
            
            if (setUI != null)
            {
                setUI.SetCostumeSetData(setData);
                costumeSetUIs.Add(setUI);
            }
        }
    }
    
    // 모든 세트 UI 업데이트
    public void UpdateAllSetUIs()
    {
        if (inventoryManager == null)
        {
            Debug.LogWarning("인벤토리 매니저가 연결되지 않았습니다.");
            return;
        }
        
        List<ItemData> playerItems = inventoryManager.GetAllItems();
        
        foreach (CostumeSetUI setUI in costumeSetUIs)
        {
            setUI.SetPlayerItems(playerItems);
        }
    }
    
    // 아이템 획득 시 호출할 메서드
    public void OnItemAcquired(ItemData item)
    {
        UpdateAllSetUIs();
    }
    
    // 아이템 사용/제거 시 호출할 메서드 
    public void OnItemRemoved(ItemData item)
    {
        UpdateAllSetUIs();
    }
}

// 인벤토리 매니저 참조를 위한 간단한 인터페이스 (실제 구현에 맞게 수정 필요)
public class InventoryManager : MonoBehaviour
{
    // 플레이어가 가진 모든 아이템 반환
    public List<ItemData> GetAllItems()
    {
        // 실제 인벤토리 시스템에 맞게 구현
        List<ItemData> items = new List<ItemData>();
        
        // 여기에 아이템 로드 로직 추가
        
        return items;
    }
    
    // 아이템 추가
    public void AddItem(ItemData item)
    {
        // 아이템 추가 로직
    }
} 