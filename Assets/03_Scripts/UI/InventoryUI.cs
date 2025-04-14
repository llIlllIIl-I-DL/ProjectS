using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum InventoryTab
{
    WeaponAttribute,
    CostumeParts,
    UsableItems
}

public class InventoryUI : MonoBehaviour
{
    [Header("탭 설정")]
    [SerializeField] private Button weaponAttributeTabButton;
    [SerializeField] private Button costumePartsTabButton;
    [SerializeField] private Button usableItemsTabButton;
    
    [Header("아이템 슬롯")]
    [SerializeField] private Transform itemSlotContainer;
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private int maxItemsPerPage = 12;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button prevPageButton;
    [SerializeField] private TextMeshProUGUI pageText;
    
    [Header("아이템 상세 정보")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailItemIcon;
    [SerializeField] private TextMeshProUGUI detailItemName;
    [SerializeField] private TextMeshProUGUI detailItemDescription;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button useButton;
    
    private List<InvenSlotUI> itemSlots = new List<InvenSlotUI>();
    private InventoryTab currentTab = InventoryTab.WeaponAttribute;
    private int currentPage = 0;
    private ItemData selectedItem;
    
    // 참조
    private InventoryManager inventoryManager;
    
    private void Awake()
    {
        // 슬롯 초기화
        InitializeItemSlots();
        
        // 버튼 이벤트 연결
        if (weaponAttributeTabButton != null)
            weaponAttributeTabButton.onClick.AddListener(() => SwitchTab(InventoryTab.WeaponAttribute));
        
        if (costumePartsTabButton != null)
            costumePartsTabButton.onClick.AddListener(() => SwitchTab(InventoryTab.CostumeParts));
        
        if (usableItemsTabButton != null)
            usableItemsTabButton.onClick.AddListener(() => SwitchTab(InventoryTab.UsableItems));
        
        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);
        
        if (prevPageButton != null)
            prevPageButton.onClick.AddListener(PrevPage);
        
        if (equipButton != null)
            equipButton.onClick.AddListener(EquipSelectedItem);
        
        if (useButton != null)
            useButton.onClick.AddListener(UseSelectedItem);
    }
    
    private void Start()
    {
        inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("인벤토리 매니저를 찾을 수 없습니다!");
        }
        
        // 초기 탭 로드
        SwitchTab(currentTab);
        HideItemDetail();
    }
    
    // 아이템 슬롯 초기화
    private void InitializeItemSlots()
    {
        // 기존 슬롯 제거
        foreach (Transform child in itemSlotContainer)
        {
            Destroy(child.gameObject);
        }
        itemSlots.Clear();
        
        // 새 슬롯 생성
        for (int i = 0; i < maxItemsPerPage; i++)
        {
            GameObject slotObj = Instantiate(itemSlotPrefab, itemSlotContainer);
            InvenSlotUI slotUI = slotObj.GetComponent<InvenSlotUI>();
            
            if (slotUI != null)
            {
                int slotIndex = i; // 클로저를 위한 로컬 변수
                
                // 슬롯 클릭 이벤트 설정 (여기에 추가해야 함)
                Button slotButton = slotObj.GetComponent<Button>();
                if (slotButton != null)
                {
                    slotButton.onClick.AddListener(() => OnItemSlotClicked(slotIndex));
                }
                
                itemSlots.Add(slotUI);
            }
        }
    }
    
    // 현재 탭에 따라 아이템 표시 업데이트
    private void UpdateItemDisplay()
    {
        if (inventoryManager == null) return;
        
        List<ItemData> itemsToShow = new List<ItemData>();
        
        // 현재 탭에 따라 아이템 목록 가져오기
        switch (currentTab)
        {
            case InventoryTab.WeaponAttribute:
                itemsToShow = inventoryManager.GetWeaponAttributes();
                break;
            case InventoryTab.CostumeParts:
                itemsToShow = inventoryManager.GetCostumeParts();
                break;
            case InventoryTab.UsableItems:
                itemsToShow = inventoryManager.GetUsableItems();
                break;
        }
        
        // 페이지 정보 업데이트
        int totalPages = Mathf.CeilToInt((float)itemsToShow.Count / maxItemsPerPage);
        if (totalPages <= 0) totalPages = 1;
        
        if (currentPage >= totalPages)
            currentPage = totalPages - 1;
        
        if (pageText != null)
            pageText.text = $"{currentPage + 1} / {totalPages}";
        
        // 페이지 버튼 상태 업데이트
        if (prevPageButton != null)
            prevPageButton.interactable = (currentPage > 0);
        
        if (nextPageButton != null)
            nextPageButton.interactable = (currentPage < totalPages - 1);
        
        // 아이템 슬롯 업데이트
        int startIndex = currentPage * maxItemsPerPage;
        
        for (int i = 0; i < itemSlots.Count; i++)
        {
            int itemIndex = startIndex + i;
            
            if (itemIndex < itemsToShow.Count)
            {
                itemSlots[i].gameObject.SetActive(true);
                itemSlots[i].SetItem(itemsToShow[itemIndex]);
            }
            else
            {
                itemSlots[i].gameObject.SetActive(false);
                itemSlots[i].SetItem(null);
            }
        }
    }
    
    // 탭 전환
    private void SwitchTab(InventoryTab tab)
    {
        currentTab = tab;
        currentPage = 0;
        selectedItem = null;
        HideItemDetail();
        UpdateItemDisplay();
        
        // 탭 버튼 상태 업데이트 (여기서 선택 상태 표시 로직 구현)
    }
    
    // 다음 페이지
    private void NextPage()
    {
        currentPage++;
        UpdateItemDisplay();
    }
    
    // 이전 페이지
    private void PrevPage()
    {
        currentPage--;
        if (currentPage < 0) currentPage = 0;
        UpdateItemDisplay();
    }
    
    // 아이템 슬롯 클릭 처리
    private void OnItemSlotClicked(int slotIndex)
    {
        int itemIndex = currentPage * maxItemsPerPage + slotIndex;
        List<ItemData> currentItems = null;
        
        // 현재 탭의 아이템 목록 가져오기
        switch (currentTab)
        {
            case InventoryTab.WeaponAttribute:
                currentItems = inventoryManager.GetWeaponAttributes();
                break;
            case InventoryTab.CostumeParts:
                currentItems = inventoryManager.GetCostumeParts();
                break;
            case InventoryTab.UsableItems:
                currentItems = inventoryManager.GetUsableItems();
                break;
        }
        
        // 유효한 아이템 인덱스인지 확인
        if (currentItems != null && itemIndex >= 0 && itemIndex < currentItems.Count)
        {
            selectedItem = currentItems[itemIndex];
            ShowItemDetail(selectedItem);
        }
    }
    
    // 아이템 상세 정보 표시
    private void ShowItemDetail(ItemData item)
    {
        if (item == null || detailPanel == null) return;
        
        detailPanel.SetActive(true);
        
        if (detailItemIcon != null)
            detailItemIcon.sprite = item.Icon;
        
        if (detailItemName != null)
            detailItemName.text = item.ItemName;
        
        if (detailItemDescription != null)
            detailItemDescription.text = item.ItemDescription;
        
        // 장착/사용 버튼 설정
        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(item.itemType == ItemType.WeaponAttribute || 
                                           item.itemType == ItemType.CostumeParts);
        }
        
        if (useButton != null)
        {
            useButton.gameObject.SetActive(item.itemType == ItemType.UsableItem);
        }
    }
    
    // 아이템 상세 정보 숨기기
    private void HideItemDetail()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);
        
        selectedItem = null;
    }
    
    // 선택한 아이템 장착
    private void EquipSelectedItem()
    {
        if (selectedItem == null || inventoryManager == null) return;
        
        if (selectedItem.itemType == ItemType.WeaponAttribute)
        {
            inventoryManager.EquipWeaponAttribute(selectedItem);
        }
        else if (selectedItem.itemType == ItemType.CostumeParts)
        {
            // 코스튬 파츠의 경우 장착 위치를 결정해야 함 (현재는 0번 슬롯으로 가정)
            // 실제로는 파츠 유형에 따라 다른 슬롯에 장착해야 함
            inventoryManager.EquipCostumePart(selectedItem, 0);
        }
        
        UpdateItemDisplay();
    }
    
    // 선택한 아이템 사용
    private void UseSelectedItem()
    {
        if (selectedItem == null || inventoryManager == null) return;
        
        if (selectedItem.itemType == ItemType.UsableItem)
        {
            inventoryManager.UseItem(selectedItem);
            UpdateItemDisplay();
            
            // 소모성 아이템인 경우 사용 후 상세 정보 숨기기
            if (selectedItem.isConsumable)
            {
                HideItemDetail();
            }
        }
    }
} 