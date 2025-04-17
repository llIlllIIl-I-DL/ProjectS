using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// CostumeSetManager 클래스 - 기존 코드와의 호환성을 위한 연결 클래스
// 기존 코드에서 CostumeSetManager를 참조하고 있으므로, 이를 CostumeManager로 연결하는 역할
public class CostumeSetManager : MonoBehaviour
{
    private static CostumeSetManager instance;
    public static CostumeSetManager Instance => instance;

    // CostumeManager 참조
    private CostumeManager costumeManager;
    private InventoryManager inventoryManager;
    private GameManager gameManager;

    // 세트 데이터 캐싱 (기존 코드와 호환성 유지)
    [SerializeField] private List<CostumeSetData> costumeSets = new List<CostumeSetData>();
    
    // 이벤트 델리게이트
    public event Action<CostumeSetData> OnCostumeSetUnlocked;
    public event Action<CostumeSetData> OnCostumeSetActivated;
    public event Action<ItemData> OnCostumePartCollected;
    public event Action OnCostumeUIUpdated;

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
        // 필요한 매니저 참조 가져오기
        costumeManager = CostumeManager.Instance;
        inventoryManager = InventoryManager.Instance;
        gameManager = GameManager.Instance;

        if (costumeManager != null)
        {
            // 이벤트 구독
            costumeManager.OnPartCollected += OnPartCollected;
            costumeManager.OnCostumeUnlocked += OnCostumeUnlocked;
            costumeManager.OnCostumeActivated += OnCostumeActivated;

            // 초기 데이터 동기화
            SyncCostumeSets();
        }
        else
        {
            Debug.LogWarning("CostumeManager가 없습니다. CostumeSetManager 기능이 제한됩니다.");
        }
    }

    private void OnDestroy()
    {
        if (costumeManager != null)
        {
            // 이벤트 구독 해제
            costumeManager.OnPartCollected -= OnPartCollected;
            costumeManager.OnCostumeUnlocked -= OnCostumeUnlocked;
            costumeManager.OnCostumeActivated -= OnCostumeActivated;
        }
    }

    // 복장 세트 데이터 동기화
    private void SyncCostumeSets()
    {
        if (costumeManager != null)
        {
            costumeSets = costumeManager.GetAllCostumeSets();
            // UI 업데이트 알림
            OnCostumeUIUpdated?.Invoke();
        }
    }

    // 파츠 수집 이벤트 처리
    private void OnPartCollected(ItemData part)
    {
        // 필요한 추가 로직 (기존 코드와의 호환성을 위해)
        Debug.Log($"파츠 수집됨: {part.ItemName}");
        
        // 자체 이벤트 발생
        OnCostumePartCollected?.Invoke(part);
        
        // UI 업데이트 알림
        OnCostumeUIUpdated?.Invoke();
        
        // 모든 세트 상태 확인
        CheckAllSetsUnlockStatus();
    }

    // 복장 해금 이벤트 처리
    private void OnCostumeUnlocked(CostumeSetData costumeSet)
    {
        Debug.Log($"복장 세트 해금됨: {costumeSet.costumeName}");
        
        // 자체 이벤트 발생
        OnCostumeSetUnlocked?.Invoke(costumeSet);
        
        // UI 업데이트 알림
        OnCostumeUIUpdated?.Invoke();
    }

    // 복장 활성화 이벤트 처리
    private void OnCostumeActivated(CostumeSetData costumeSet)
    {
        Debug.Log($"복장 세트 활성화됨: {costumeSet.costumeName}");
        
        // 이전에 활성화된 복장 효과 제거
        DeactivateAllCostumeEffects();
        
        // 새 복장에 맞는 효과 활성화
        ActivateCostumeEffect(costumeSet);
        
        // 자체 이벤트 발생
        OnCostumeSetActivated?.Invoke(costumeSet);
        
        // UI 업데이트 알림
        OnCostumeUIUpdated?.Invoke();
        
        // 게임 데이터 저장
        if (gameManager != null)
        {
            gameManager.SaveCostumeState();
        }
    }
    
    // 모든 복장 효과 비활성화
    private void DeactivateAllCostumeEffects()
    {
        // 플레이어 객체에서 윙슈트 효과 찾아 비활성화
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            WingSuitEffect wingSuitEffect = playerMovement.GetComponent<WingSuitEffect>();
            if (wingSuitEffect != null)
            {
                wingSuitEffect.DeactivateEffect();
                Debug.Log("기존 윙슈트 효과를 비활성화했습니다.");
            }
            
            // 다른 효과들도 필요시 여기에 추가
        }
    }
    
    // 복장에 맞는 효과 활성화
    private void ActivateCostumeEffect(CostumeSetData costumeSet)
    {
        if (costumeSet == null) return;
        
        // 윙슈트 복장인 경우
        if (costumeSet.costumeId == "wing")
        {
            PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
            if (playerMovement != null)
            {
                WingSuitEffect wingSuitEffect = playerMovement.GetComponent<WingSuitEffect>();
                if (wingSuitEffect == null)
                {
                    wingSuitEffect = playerMovement.gameObject.AddComponent<WingSuitEffect>();
                }
                
                if (wingSuitEffect != null)
                {
                    wingSuitEffect.costumeData = costumeSet;
                    wingSuitEffect.enabled = true;
                    wingSuitEffect.ActivateEffect();
                    Debug.Log("윙슈트 효과가 활성화되었습니다.");
                }
            }
        }
        
        // 다른 특수 복장 효과도 필요시 여기에 추가
    }

    // 모든 세트의 해금 상태 확인
    private void CheckAllSetsUnlockStatus()
    {
        if (costumeManager == null) return;
        
        foreach (CostumeSetData costumeSet in costumeSets)
        {
            CheckSetUnlockStatus(costumeSet);
        }
    }
    
    // 특정 세트의 해금 상태 확인
    public bool CheckSetUnlockStatus(CostumeSetData costumeSet)
    {
        if (costumeManager == null || costumeSet == null) return false;
        
        // 이미 해금된 상태면 처리 불필요
        if (costumeSet.isUnlocked) return true;
        
        bool allPartsCollected = true;
        
        // 모든 필요 파츠가 수집되었는지 확인
        foreach (ItemData part in costumeSet.requiredParts)
        {
            if (part == null) continue;
            
            if (!costumeManager.HasPart(part.id))
            {
                allPartsCollected = false;
                break;
            }
        }
        
        // 모든 파츠가 수집되었으면 세트 해금
        if (allPartsCollected && !costumeSet.isUnlocked)
        {
            UnlockCostumeSet(costumeSet);
            return true;
        }
        
        return false;
    }
    
    // 복장 세트 해금 메서드
    public void UnlockCostumeSet(CostumeSetData costumeSet)
    {
        if (costumeSet == null) return;
        
        // 이미 해금된 상태면 처리 불필요
        if (costumeSet.isUnlocked) return;
        
        // 세트 해금 처리
        costumeSet.isUnlocked = true;
        
        Debug.Log($"복장 세트 '{costumeSet.costumeName}' 해금 완료!");
        
        // 이벤트 발생
        OnCostumeSetUnlocked?.Invoke(costumeSet);
        
        // UI 업데이트
        OnCostumeUIUpdated?.Invoke();
    }

    // 아이템 획득 시 호출 (InventoryManager에서 호출)
    public void OnItemAcquired(ItemData item)
    {
        if (costumeManager != null && item.itemType == ItemType.CostumeParts)
        {
            costumeManager.AddPart(item);
            
            // 모든 세트의 해금 상태 확인
            CheckAllSetsUnlockStatus();
        }
    }

    // 아이템 제거 시 호출 (InventoryManager에서 호출)
    public void OnItemRemoved(ItemData item)
    {
        if (costumeManager != null && item.itemType == ItemType.CostumeParts)
        {
            // 삭제된 파츠가 현재 활성화된 복장의 일부인지 확인
            CostumeSetData activeCostume = costumeManager.GetActiveCostume();
            if (activeCostume != null && activeCostume.requiredParts.Contains(item))
            {
                // 활성화된 복장의 파츠가 제거되면 해당 복장 비활성화
                DeactivateAllCostumeEffects();
            }
            
            // 세트 상태 업데이트
            SyncCostumeSets();
        }
    }

    // 활성화된 복장 세트 설정
    public void SetActiveCostumeSet(CostumeSetData costumeSet)
    {
        if (costumeManager != null && costumeSet != null)
        {
            // 모든 파츠가 있는지 확인
            Dictionary<int, bool> partsStatus = GetCostumePartsStatus(costumeSet);
            bool hasAllParts = true;
            
            foreach (var partStatus in partsStatus)
            {
                if (!partStatus.Value)
                {
                    hasAllParts = false;
                    break;
                }
            }
            
            if (!hasAllParts)
            {
                Debug.LogWarning($"'{costumeSet.costumeName}' 복장 활성화에 필요한 모든 파츠가 없습니다.");
                return;
            }
            
            if (!costumeSet.isUnlocked)
            {
                Debug.LogWarning($"'{costumeSet.costumeName}' 복장이 아직 해금되지 않았습니다.");
                return;
            }
            
            // 복장 활성화
            bool success = costumeManager.ActivateCostume(costumeSet.costumeId);
            
            if (success)
            {
                Debug.Log($"'{costumeSet.costumeName}' 복장이 성공적으로 활성화되었습니다.");
            }
            else
            {
                Debug.LogError($"'{costumeSet.costumeName}' 복장 활성화 실패!");
            }
        }
    }
    
    // 복장의 파츠 상태 확인
    public Dictionary<int, bool> GetCostumePartsStatus(CostumeSetData costumeSet)
    {
        Dictionary<int, bool> result = new Dictionary<int, bool>();
        
        if (costumeManager != null && costumeSet != null)
        {
            foreach (ItemData part in costumeSet.requiredParts)
            {
                if (part != null)
                {
                    result[part.id] = costumeManager.HasPart(part.id);
                }
            }
        }
        
        return result;
    }

    // 복장 세트 해금 여부 확인
    public bool IsCostumeSetUnlocked(string costumeId)
    {
        if (costumeManager != null)
        {
            CostumeSetData set = costumeManager.GetCostumeSet(costumeId);
            return set != null && set.isUnlocked;
        }
        return false;
    }
    
    // 복장에 필요한 모든 파츠가 있는지 확인
    public bool HasAllCostumeParts(string costumeId)
    {
        if (costumeManager == null) return false;
        
        CostumeSetData costumeSet = costumeManager.GetCostumeSet(costumeId);
        if (costumeSet == null) return false;
        
        Dictionary<int, bool> partsStatus = GetCostumePartsStatus(costumeSet);
        
        foreach (var partStatus in partsStatus)
        {
            if (!partStatus.Value) return false;
        }
        
        return true;
    }

    // 모든 복장 세트 가져오기
    public List<CostumeSetData> GetAllCostumeSets()
    {
        return costumeSets;
    }

    // 특정 복장 세트 찾기
    public CostumeSetData GetCostumeSetById(string costumeId)
    {
        if (costumeManager != null)
        {
            return costumeManager.GetCostumeSet(costumeId);
        }
        return costumeSets.Find(set => set.costumeId == costumeId);
    }

    // 현재 활성화된 복장 세트 가져오기
    public CostumeSetData GetActiveCostumeSet()
    {
        if (costumeManager != null)
        {
            return costumeManager.GetActiveCostume();
        }
        return null;
    }
    
    // 모든 UI 업데이트 요청
    public void RequestUIUpdate()
    {
        OnCostumeUIUpdated?.Invoke();
    }
}