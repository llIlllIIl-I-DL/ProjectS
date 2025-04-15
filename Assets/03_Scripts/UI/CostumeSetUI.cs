using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CostumeSetUI : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private TextMeshProUGUI costumeNameText;
    [SerializeField] private Button unlockButton;
    [SerializeField] private TextMeshProUGUI unlockButtonText;
    [SerializeField] private Image[] costumeSlots = new Image[4];
    
    [Header("데이터")]
    [SerializeField] private CostumeSetData costumeSetData;
    
    // 플레이어 인벤토리 참조 (실제 구현에 맞게 수정 필요)
    private List<ItemData> playerItems = new List<ItemData>();
    
    private void Start()
    {
        UpdateUI();
    }
    
    // 플레이어 아이템 목록 업데이트 (인벤토리 시스템에 맞게 구현)
    public void SetPlayerItems(List<ItemData> items)
    {
        playerItems = items;
        UpdateUI();
    }
    
    // 복장 세트 데이터 설정
    public void SetCostumeSetData(CostumeSetData setData)
    {
        costumeSetData = setData;
        UpdateUI();
    }
    
    // UI 업데이트
    private void UpdateUI()
    {
        if (costumeSetData == null) return;
        
        // 복장 이름 설정
        costumeNameText.text = costumeSetData.SetName;
        
        // 슬롯 업데이트
        for (int i = 0; i < costumeSlots.Length; i++)
        {
            ItemData partItem = costumeSetData.GetPartAtSlot(i);
            
            if (partItem != null)
            {
                // 플레이어가 해당 파츠를 가지고 있는지 확인
                bool hasItem = false;
                foreach (ItemData playerItem in playerItems)
                {
                    if (playerItem.id == partItem.id)
                    {
                        hasItem = true;
                        break;
                    }
                }
                
                // 아이템 아이콘 설정 및 투명도 조절
                costumeSlots[i].sprite = partItem.Icon;
                Color slotColor = costumeSlots[i].color;
                slotColor.a = hasItem ? 1f : 0.5f;
                costumeSlots[i].color = slotColor;
            }
            else
            {
                // 빈 슬롯
                costumeSlots[i].sprite = null;
                Color slotColor = costumeSlots[i].color;
                slotColor.a = 0.3f;
                costumeSlots[i].color = slotColor;
            }
        }
        
        // 버튼 상태 업데이트
        bool isComplete = costumeSetData.IsComplete(playerItems);
        unlockButtonText.text = isComplete ? "해금" : "미 해금";
        unlockButton.interactable = isComplete;
    }
    
    // 해금 버튼 클릭 처리
    public void OnUnlockButtonClicked()
    {
        if (costumeSetData == null) return;
        
        if (costumeSetData.IsComplete(playerItems))
        {
            // 보상 지급 로직 추가
            Debug.Log($"{costumeSetData.SetName} 세트 해금! 보상: {costumeSetData.SetReward.ItemName}");
            
            // 여기에 보상 지급 로직 구현
            // 예: InventoryManager.AddItem(costumeSetData.SetReward);
            
            // UI 업데이트
            UpdateUI();
        }
    }
} 