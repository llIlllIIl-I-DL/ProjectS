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
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private GameObject activeIndicator;

    [Header("데이터")]
    [SerializeField] private CostumeSetData costumeSetData;

    // 매니저 참조
    private InventoryManager inventoryManager;
    private CostumeManager costumeManager;

    private void Awake()
    {
        // 버튼 이벤트 연결
        if (unlockButton != null)
            unlockButton.onClick.AddListener(OnUnlockButtonClicked);
    }

    private void Start()
    {
        // 매니저 참조 가져오기
        inventoryManager = InventoryManager.Instance;
        costumeManager = CostumeManager.Instance;

        UpdateUI();
    }

    // 복장 세트 데이터 설정
    public void SetCostumeSetData(CostumeSetData setData, bool isActive = false)
    {
        costumeSetData = setData;

        // 활성화 상태 설정
        if (activeIndicator != null)
            activeIndicator.SetActive(isActive);

        UpdateUI();
    }

    // UI 업데이트
    private void UpdateUI()
    {
        if (costumeSetData == null || inventoryManager == null) return;

        // 복장 이름 설정
        if (costumeNameText != null)
            costumeNameText.text = costumeSetData.costumeName;

        // 슬롯 업데이트
        UpdateSlots();

        // 잠금 아이콘 표시 설정
        if (lockIcon != null)
            lockIcon.SetActive(!costumeSetData.isUnlocked);

        // 버튼 상태 업데이트
        UpdateButtonState();
    }

    // 슬롯 업데이트
    private void UpdateSlots()
    {
        // costumeSetData가 null인지 확인
        if (costumeSetData == null)
        {
            Debug.LogWarning("UpdateSlots: costumeSetData가 null입니다.");
            return;
        }

        // 필요한 파츠 및 수집 상태 가져오기
        Dictionary<int, bool> partsCollectionStatus = new Dictionary<int, bool>();
        if (costumeManager != null && !string.IsNullOrEmpty(costumeSetData.costumeId))
        {
            // costumeId가 존재하는지 확인
            CostumeSetData costume = costumeManager.GetCostumeSet(costumeSetData.costumeId);
            if (costume != null)
            {
                partsCollectionStatus = costumeManager.GetPartsCollectionStatus(costumeSetData.costumeId);
            }
            else
            {
                Debug.LogWarning($"UpdateSlots: costumeId '{costumeSetData.costumeId}'를 가진 복장 세트를 찾을 수 없습니다.");
            }
        }

        // 각 슬롯 업데이트
        for (int i = 0; i < costumeSlots.Length && i < costumeSetData.requiredParts.Count; i++)
        {
            ItemData partItem = costumeSetData.requiredParts[i];

            if (partItem != null)
            {
                // 플레이어가 해당 파츠를 가지고 있는지 확인
                bool hasItem = partsCollectionStatus.ContainsKey(partItem.id) && partsCollectionStatus[partItem.id];

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
    }

    // 버튼 상태 업데이트
    private void UpdateButtonState()
    {
        bool isUnlocked = costumeSetData.isUnlocked;
        bool isActive = false;

        if (costumeManager != null)
        {
            isActive = costumeManager.IsActiveCostume(costumeSetData.costumeId);
        }

        // 버튼 텍스트 설정
        if (unlockButtonText != null)
        {
            if (isActive)
                unlockButtonText.text = "활성화 중";
            else if (isUnlocked)
                unlockButtonText.text = "활성화";
            else
                unlockButtonText.text = "해금 필요";
        }

        // 버튼 활성화 여부
        if (unlockButton != null)
        {
            unlockButton.interactable = isUnlocked && !isActive;
        }
    }

    // 해금/활성화 버튼 클릭 처리
    public void OnUnlockButtonClicked()
    {
        if (costumeSetData == null || costumeManager == null) return;

        if (costumeSetData.isUnlocked)
        {
            // 복장 활성화
            bool success = costumeManager.ActivateCostume(costumeSetData.costumeId);

            if (success)
            {
                Debug.Log($"{costumeSetData.costumeName} 복장을 활성화했습니다!");
                UpdateUI();
            }
        }
    }

    // 활성화 상태 업데이트
    public void UpdateActiveState(bool isActive)
    {
        if (activeIndicator != null)
            activeIndicator.SetActive(isActive);

        UpdateButtonState();
    }
}

// CostumeSetData 확장 메서드 (기존 코드와 호환성을 위해 추가)
public static class CostumeSetDataExtensions
{
    // 예전 코드와의 호환성을 위한 메서드
    public static ItemData GetPartAtSlot(this CostumeSetData costumeSet, int slotIndex)
    {
        if (costumeSet == null || slotIndex < 0 || slotIndex >= costumeSet.requiredParts.Count)
            return null;

        return costumeSet.requiredParts[slotIndex];
    }

    // 세트 완성 여부 확인 (이전 방식)
    public static bool IsComplete(this CostumeSetData costumeSet, List<ItemData> playerItems)
    {
        if (costumeSet == null || playerItems == null)
            return false;

        // 필요한 모든 파츠가 플레이어 인벤토리에 있는지 확인
        foreach (ItemData requiredPart in costumeSet.requiredParts)
        {
            bool found = false;
            foreach (ItemData playerItem in playerItems)
            {
                if (playerItem.id == requiredPart.id)
                {
                    found = true;
                    break;
                }
            }

            if (!found) return false;
        }

        return true;
    }

    // 이전 코드와 호환성을 위한 속성
    public static string SetName(this CostumeSetData costumeSet)
    {
        return costumeSet?.costumeName ?? "Unknown";
    }

    // 세트 보상 (필요하다면 구현)
    public static ItemData SetReward(this CostumeSetData costumeSet)
    {
        // 실제 구현에서는 costumeSet에 reward 필드를 추가하거나
        // 보상 아이템 데이터를 다른 방식으로 반환
        return null;
    }
}