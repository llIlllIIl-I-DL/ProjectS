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
    private GameManager gameManager;

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
        gameManager = GameManager.Instance;

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
        
        // 현재 복장이 활성화된 상태인지 확인하고 효과 처리
        if (costumeManager != null && costumeManager.IsActiveCostume(costumeSetData.costumeId))
        {
            UpdateCostumeEffect(true);
        }
    }

    // 슬롯 업데이트
    // 데이터는 외부에서 설정하고 Update 만 하면 자동으로 되는 구조 좋음
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

                // 슬롯에 대한 설정을 여기서 할 필요는 없다.
                // 슬롯 내부에서 알아서 하자
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
        if (costumeSetData == null)
        {
            Debug.LogWarning("UpdateButtonState: costumeSetData가 null입니다.");
            return;
        }

        bool isUnlocked = costumeSetData.isUnlocked;
        bool isActive = false;
        bool hasAllParts = false;

        // 디버그 정보
        Debug.Log($"복장 '{costumeSetData.costumeName}' 상태 - 해금됨: {isUnlocked}");

        if (costumeManager != null)
        {
            isActive = costumeManager.IsActiveCostume(costumeSetData.costumeId);
            
            // 인벤토리에 모든 파츠가 있는지 확인
            Dictionary<int, bool> partsStatus = costumeManager.GetPartsCollectionStatus(costumeSetData.costumeId);
            
            // 초기값을 true로 설정하고, 부족한 파츠가 있을 때만 false로 설정
            hasAllParts = true;
            
            Debug.Log($"파츠 상태 확인 - 총 {costumeSetData.requiredParts.Count}개 파츠 필요");
            
            // 모든 파츠를 가지고 있는지 확인
            foreach (ItemData part in costumeSetData.requiredParts)
            {
                if (part != null)
                {
                    bool hasPart = partsStatus.ContainsKey(part.id) && partsStatus[part.id];
                    Debug.Log($"파츠 '{part.ItemName}' 보유 여부: {hasPart}, ID: {part.id}");
                    
                    if (!hasPart)
                    {
                        hasAllParts = false;
                        break; // 하나라도 없으면 반복문 종료
                    }
                }
            }
            
            // 해금 상태 강제 재확인
            if (hasAllParts && !isUnlocked)
            {
                Debug.Log("모든 파츠를 가지고 있지만 해금되지 않은 상태입니다. CostumeManager에 해금 요청");
                costumeManager.CheckCostumeUnlocks(costumeSetData.costumeId);
                // 상태 다시 확인
                isUnlocked = costumeSetData.isUnlocked;
            }
            
            Debug.Log($"최종 확인 - 모든 파츠 보유 여부: {hasAllParts}, 해금 상태: {isUnlocked}");
        }
        else
        {
            Debug.LogWarning("costumeManager가 null입니다.");
        }

        // 버튼 텍스트 설정
        if (unlockButtonText != null)
        {
            if (isActive)
                unlockButtonText.text = "활성화 중";
            else if (isUnlocked && hasAllParts)
                unlockButtonText.text = "활성화";
            else if (isUnlocked && !hasAllParts)
                unlockButtonText.text = "파츠 부족";
            else
                unlockButtonText.text = "해금 필요";
            
            Debug.Log($"버튼 텍스트: {unlockButtonText.text}");
        }

        // 버튼 활성화 여부
        if (unlockButton != null)
        {
            // 세트가 해금되어 있고, 활성화 상태가 아니며, 모든 파츠가 인벤토리에 있으면 버튼 활성화
            unlockButton.interactable = isUnlocked && !isActive && hasAllParts;
            
            Debug.Log($"버튼 활성화 상태: {unlockButton.interactable}");
            Debug.Log($"조건 체크 - 해금됨: {isUnlocked}, 활성화 안됨: {!isActive}, 모든파츠있음: {hasAllParts}");
        }
    }

    // 복장 효과 업데이트
    private void UpdateCostumeEffect(bool isActive)
    {
        Debug.Log($"UpdateCostumeEffect 호출됨 - isActive: {isActive}, costumeId: {costumeSetData?.costumeId}");
        
        // 윙슈트 복장인 경우 효과 활성화
        if (costumeSetData != null && costumeSetData.costumeId == "wing" && isActive)
        {
            Debug.Log($"윙슈트 복장 '{costumeSetData.costumeName}'이 활성화되어 있습니다. 효과 활성화.");
            
            // 플레이어 객체 찾기 - 다양한 방법으로 시도
            PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>(true); // 비활성화된 오브젝트도 포함
            
            Debug.Log($"플레이어 찾기 결과: {(playerMovement != null ? "성공" : "실패")}");
            
            if (playerMovement == null)
            {
                // 태그로 찾기 시도
                // 플레이어를 찾기위한 방은은 따로 만들어 두자 - 플레이어 매니저 등
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    Debug.Log("태그로 플레이어 찾기 성공");
                    playerMovement = playerObject.GetComponent<PlayerMovement>();
                }
            }
            
            if (playerMovement != null)
            {
                // 윙슈트 효과 컴포넌트 가져오기 또는 추가
                // 기능 외적으로 접근하는데 코드 리소스가 큰 편
                WingSuitEffect wingSuitEffect = playerMovement.GetComponent<WingSuitEffect>();
                if (wingSuitEffect == null)
                {
                    wingSuitEffect = playerMovement.gameObject.AddComponent<WingSuitEffect>();
                    Debug.Log("플레이어에 윙슈트 효과 컴포넌트를 추가했습니다.");
                }
                
                // 효과 활성화
                if (wingSuitEffect != null && !wingSuitEffect.enabled)
                {
                    wingSuitEffect.enabled = true;
                    wingSuitEffect.ActivateEffect();
                    Debug.Log("윙슈트 효과가 활성화되었습니다.");
                }
            }
            else
            {
                Debug.LogError("플레이어 오브젝트를 찾을 수 없어 윙슈트 효과를 적용할 수 없습니다. 플레이어에 'Player' 태그가 있는지, PlayerMovement 컴포넌트가 있는지 확인하세요.");
            }
        }
    }

    // 해금/활성화 버튼 클릭 처리
    public void OnUnlockButtonClicked()
    {
        // 체크가 필요한 것부터 하나씩 return 으로 예외처리 - Good
        // 아니면 가로가 너무 깊어진다.
        if (costumeSetData == null)
        {
            Debug.LogWarning("OnUnlockButtonClicked: costumeSetData가 null입니다.");
            return;
        }

        if (costumeManager == null)
        {
            Debug.LogWarning("OnUnlockButtonClicked: costumeManager가 null입니다.");
            return;
        }

        Debug.Log($"복장 '{costumeSetData.costumeName}' 버튼 클릭됨");
        Debug.Log($"복장 해금 상태: {costumeSetData.isUnlocked}");
        
        // 해금된 복장만 활성화 가능
        if (!costumeSetData.isUnlocked)
        {
            Debug.LogWarning($"복장 '{costumeSetData.costumeName}'가 아직 해금되지 않았습니다.");
            return;
        }

        // 게임 상태 로그만 출력 (상태 체크 없음)
        if (gameManager != null)
        {
            Debug.Log($"현재 게임 상태: {gameManager.CurrentState}, 게임 상태와 관계없이 복장 활성화를 진행합니다.");
        }

        // 모든 필요 파츠가 있는지 확인
        bool hasAllRequiredParts = true;
        Dictionary<int, bool> partsStatus = costumeManager.GetPartsCollectionStatus(costumeSetData.costumeId);
        
        Debug.Log($"파츠 상태 확인 중 - 총 {costumeSetData.requiredParts.Count}개 파츠");
        
        foreach (ItemData part in costumeSetData.requiredParts)
        {
            if (part == null) continue;
            
            bool hasPart = partsStatus.ContainsKey(part.id) && partsStatus[part.id];
            Debug.Log($"파츠 '{part.ItemName}' (ID: {part.id}) 보유 여부: {hasPart}");
            
            if (!hasPart)
            {
                hasAllRequiredParts = false;
                Debug.LogWarning($"파츠 '{part.ItemName}'가 인벤토리에 없습니다.");
                break;
            }
        }
        
        if (!hasAllRequiredParts)
        {
            Debug.LogWarning("복장 활성화에 필요한 모든 파츠가 인벤토리에 없습니다.");
            return;
        }
        
        // 현재 활성화된 복장이 있으면 해당 효과 비활성화
        if (costumeManager.GetActiveCostume() != null)
        {
            DeactivateCurrentCostumeEffect();
        }
        
        // 복장 활성화
        Debug.Log($"'{costumeSetData.costumeName}' 복장 활성화 시도...");
        bool success = costumeManager.ActivateCostume(costumeSetData.costumeId);

        if (success)
        {
            Debug.Log($"{costumeSetData.costumeName} 복장을 활성화했습니다!");
            
            // 윙슈트 효과 활성화 - costumeId 값 확인 로그 추가
            Debug.Log($"복장 ID 확인: {costumeSetData.costumeId}, 윙슈트 여부: {costumeSetData.costumeId == "wing"}");
            if (costumeSetData.costumeId == "wing")
            {
                UpdateCostumeEffect(true);
            }
            
            // 게임 매니저를 통해 데이터 저장
            if (gameManager != null)
            {
                gameManager.SaveCostumeState();
                Debug.Log("복장 상태가 저장되었습니다.");
            }
            
            UpdateUI();
        }
        else
        {
            Debug.LogWarning($"{costumeSetData.costumeName} 복장 활성화에 실패했습니다.");
        }
    }
    
    // 현재 활성화된 복장 효과 비활성화
    private void DeactivateCurrentCostumeEffect()
    {
        CostumeSetData activeCostume = costumeManager.GetActiveCostume();
        if (activeCostume != null && activeCostume.costumeId == "wing")
        {
            // 윙슈트 효과 찾아서 비활성화
            PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
            if (playerMovement != null)
            {
                WingSuitEffect wingSuitEffect = playerMovement.GetComponent<WingSuitEffect>();
                if (wingSuitEffect != null)
                {
                    wingSuitEffect.DeactivateEffect();
                    Debug.Log("이전 윙슈트 효과를 비활성화했습니다.");
                }
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