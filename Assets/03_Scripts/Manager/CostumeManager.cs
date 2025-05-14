using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// 복장 관리자 클래스
public class CostumeManager : MonoBehaviour
{
    public static CostumeManager Instance { get; private set; }

    // 수집한 파츠 아이템 목록
    private HashSet<int> collectedPartIds = new HashSet<int>();

    // 전체 복장 세트 목록 (에디터에서 할당)
    [SerializeField] private List<CostumeSetData> allCostumeSets = new List<CostumeSetData>();

    // 현재 활성화된 복장
    private CostumeSetData activeCostumeSet;
    private CostumeEffectBase activeEffect;

    // 이벤트 델리게이트
    public delegate void PartCollectionHandler(ItemData part);
    public event PartCollectionHandler OnPartCollected;

    public delegate void CostumeHandler(CostumeSetData costumeSet);
    public event CostumeHandler OnCostumeUnlocked;
    public event CostumeHandler OnCostumeActivated;

    // Action 스타일로 구성가능
    // public event Action<ItemData> OnPartCollected;
    // public event Action<CostumeSetData> OnCostumeUnlocked;
    // public event Action<CostumeSetData> OnCostumeActivated;
    
    private InventoryManager inventoryManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        inventoryManager = InventoryManager.Instance;
        Debug.Log("CostumeManager 시작됨");

        if (inventoryManager != null)
        {
            // 인벤토리 변경 이벤트 구독
            inventoryManager.OnItemAdded += OnItemAdded;
            Debug.Log("인벤토리 매니저에 이벤트 구독 완료");
            
            // 기존 인벤토리에서 파츠 불러오기
            LoadPartsFromInventory();
        }
        else
        {
            Debug.LogWarning("inventoryManager가 null입니다.");
        }

        // 초기 복장 해금 상태 검사
        Debug.Log($"초기 수집된 파츠 수: {collectedPartIds.Count}");
        CheckAllCostumeUnlocks();
    }

    private void OnDestroy()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnItemAdded -= OnItemAdded;
        }
    }

    // 아이템 추가 이벤트 처리
    private void OnItemAdded(ItemData item)
    {
        // 파츠 아이템인 경우 처리
        if (item.itemType == ItemType.CostumeParts)
        {
            AddPart(item);
        }
    }

    // 파츠 추가
    public bool AddPart(ItemData part)
    {
        if (part == null || part.itemType != ItemType.CostumeParts)
        {
            Debug.LogWarning($"AddPart: 유효하지 않은 파츠입니다. (null: {part == null})");
            return false;
        }

        Debug.Log($"파츠 추가 시도: {part.ItemName} (ID: {part.id})");
        
        // 이미 수집한 파츠인지 확인
        if (collectedPartIds.Contains(part.id))
        {
            Debug.Log($"이미 수집한 파츠입니다: {part.ItemName} (ID: {part.id})");
            return false;
        }

        // 파츠 추가
        collectedPartIds.Add(part.id);
        Debug.Log($"파츠를 성공적으로 추가했습니다: {part.ItemName} (ID: {part.id})");
        Debug.Log($"현재 수집한 총 파츠 수: {collectedPartIds.Count}");

        // 이벤트 발생
        OnPartCollected?.Invoke(part);

        // 복장 해금 상태 검사
        if (!string.IsNullOrEmpty(part.costumeSetId))
        {
            Debug.Log($"복장 세트 해금 검사: {part.costumeSetId}");
            CheckCostumeUnlocks(part.costumeSetId);
        }
        else
        {
            Debug.LogWarning($"파츠에 costumeSetId가 없습니다: {part.ItemName} (ID: {part.id})");
            // 모든 복장 세트를 검사하여 파츠가 어떤 세트에 속하는지 확인
            CheckAllCostumeUnlocks();
        }

        return true;
    }

    // 특정 복장 세트의 해금 상태 검사
    public void CheckCostumeUnlocks(string costumeSetId)
    {
        if (string.IsNullOrEmpty(costumeSetId))
        {
            Debug.LogWarning("CheckCostumeUnlocks: costumeSetId가 null 또는 빈 문자열입니다.");
            return;
        }

        CostumeSetData costumeSet = allCostumeSets.Find(c => c.costumeId == costumeSetId);
        if (costumeSet == null)
        {
            Debug.LogWarning($"CheckCostumeUnlocks: costumeId '{costumeSetId}'를 가진 복장 세트를 찾을 수 없습니다.");
            return;
        }

        if (costumeSet.isUnlocked)
        {
            Debug.Log($"복장 세트 '{costumeSet.costumeName}'는 이미 해금되어 있습니다.");
            return;
        }

        Debug.Log($"복장 세트 '{costumeSet.costumeName}' 해금 검사 시작...");

        // 모든 필요 파츠를 수집했는지 확인
        bool allPartsCollected = true;
        foreach (ItemData part in costumeSet.requiredParts)
        {
            if (part == null) continue;

            bool hasPart = collectedPartIds.Contains(part.id);
            Debug.Log($"파츠 '{part.ItemName}' (ID: {part.id}) 보유 여부: {hasPart}");
            
            if (!hasPart)
            {
                allPartsCollected = false;
                Debug.Log($"파츠 '{part.ItemName}'가 없어서 '{costumeSet.costumeName}' 복장 해금이 불가능합니다.");
                break;
            }
        }

        // 모든 파츠를 수집했으면 복장 해금
        if (allPartsCollected)
        {
            costumeSet.isUnlocked = true;
            Debug.Log($"모든 파츠를 수집하여 '{costumeSet.costumeName}' 복장이 해금되었습니다!");
            OnCostumeUnlocked?.Invoke(costumeSet);
        }
        else
        {
            Debug.Log($"'{costumeSet.costumeName}' 복장을 해금하기 위한 모든 파츠가 아직 수집되지 않았습니다.");
        }
    }

    // 인벤토리에서 파츠 불러오기
    private void LoadPartsFromInventory()
    {
        if (inventoryManager == null) return;
        
        List<ItemData> inventoryParts = inventoryManager.GetCostumeParts();
        Debug.Log($"인벤토리에서 {inventoryParts.Count}개의 파츠를 발견했습니다.");
        
        foreach (ItemData part in inventoryParts)
        {
            if (part != null && part.itemType == ItemType.CostumeParts)
            {
                if (!collectedPartIds.Contains(part.id))
                {
                    collectedPartIds.Add(part.id);
                    Debug.Log($"인벤토리에서 파츠 로드: {part.ItemName} (ID: {part.id})");
                }
            }
        }
        
        Debug.Log($"파츠 로드 후 컬렉션 크기: {collectedPartIds.Count}");
    }

    // 모든 복장 세트의 해금 상태 검사
    private void CheckAllCostumeUnlocks()
    {
        Debug.Log($"모든 복장 세트 해금 상태 검사 시작 (총 {allCostumeSets.Count}개 세트)");
        
        foreach (CostumeSetData costumeSet in allCostumeSets)
        {
            if (costumeSet == null)
            {
                Debug.LogWarning("null인 복장 세트가 발견되었습니다.");
                continue;
            }
            
            Debug.Log($"복장 세트 '{costumeSet.costumeName}' 검사 중 (해금됨: {costumeSet.isUnlocked})");
            
            if (!costumeSet.isUnlocked)
            {
                bool allPartsCollected = true;
                
                Debug.Log($"  필요한 파츠 수: {costumeSet.requiredParts.Count}");
                
                foreach (ItemData part in costumeSet.requiredParts)
                {
                    if (part == null)
                    {
                        Debug.LogWarning($"  null인 파츠가 '{costumeSet.costumeName}' 세트에 포함되어 있습니다.");
                        continue;
                    }
                    
                    bool hasPart = collectedPartIds.Contains(part.id);
                    Debug.Log($"  파츠 '{part.ItemName}' (ID: {part.id}) 보유 여부: {hasPart}");
                    
                    if (!hasPart)
                    {
                        allPartsCollected = false;
                        Debug.Log($"  파츠 '{part.ItemName}'가 없어서 '{costumeSet.costumeName}' 복장 해금이 불가능합니다.");
                        break;
                    }
                }

                if (allPartsCollected)
                {
                    costumeSet.isUnlocked = true;
                    Debug.Log($"모든 파츠를 수집하여 '{costumeSet.costumeName}' 복장이 해금되었습니다!");
                    OnCostumeUnlocked?.Invoke(costumeSet);
                }
                else
                {
                    Debug.Log($"'{costumeSet.costumeName}' 복장을 해금하기 위한 모든 파츠가 아직 수집되지 않았습니다.");
                }
            }
        }
    }

    // 복장 활성화
    public bool ActivateCostume(string costumeId)
    {
        CostumeSetData costumeSet = allCostumeSets.Find(c => c.costumeId == costumeId);
        if (costumeSet == null || !costumeSet.isUnlocked)
        {
            return false;
        }

        // 이전 복장 비활성화
        DeactivateCurrentCostume();

        // 새 복장 활성화
        activeCostumeSet = costumeSet;

        // 복장 효과 활성화
        ActivateCostumeEffect(costumeSet);

        // 이벤트 발생
        OnCostumeActivated?.Invoke(costumeSet);

        return true;
    }

    // 현재 복장 비활성화
    private void DeactivateCurrentCostume()
    {
        if (activeCostumeSet != null && activeEffect != null)
        {
            activeEffect.DeactivateEffect();
            Destroy(activeEffect);
            activeEffect = null;
        }

        activeCostumeSet = null;
    }

    // 복장 효과 활성화
    private void ActivateCostumeEffect(CostumeSetData costumeSet)
    {
        if (costumeSet == null) return;

        // 복장 ID에 따라 적절한 효과 컴포넌트 추가
        switch (costumeSet.costumeId)
        {
            case "wing":
                activeEffect = gameObject.AddComponent<WingSuitEffect>();
                break;

            // 다른 복장 효과 추가...

            default:
                return;
        }

        if (activeEffect != null)
        {
            activeEffect.costumeData = costumeSet;
            activeEffect.ActivateEffect();
        }
    }

    // 파츠 수집 여부 확인
    public bool HasPart(int partId)
    {
        return collectedPartIds.Contains(partId);
    }

    // 복장 세트의 파츠 수집 상태 가져오기
    public Dictionary<int, bool> GetPartsCollectionStatus(string costumeId)
    {
        // 잘못된 costumeId 처리
        if (string.IsNullOrEmpty(costumeId))
        {
            Debug.LogWarning("GetPartsCollectionStatus: costumeId가 null 또는 빈 문자열입니다.");
            return new Dictionary<int, bool>();
        }

        // allCostumeSets가 null이거나 비어있는지 확인
        if (allCostumeSets == null || allCostumeSets.Count == 0)
        {
            Debug.LogWarning("GetPartsCollectionStatus: allCostumeSets가 초기화되지 않았거나 비어 있습니다.");
            return new Dictionary<int, bool>();
        }

        CostumeSetData costumeSet = allCostumeSets.Find(c => c != null && c.costumeId == costumeId);
        if (costumeSet == null)
        {
            Debug.LogWarning($"GetPartsCollectionStatus: costumeId '{costumeId}'를 가진 복장 세트를 찾을 수 없습니다.");
            return new Dictionary<int, bool>();
        }

        Dictionary<int, bool> result = new Dictionary<int, bool>();

        Debug.Log($"GetPartsCollectionStatus: '{costumeSet.costumeName}' 세트의 파츠 상태 확인 시작 (현재 수집된 파츠: {collectedPartIds.Count}개)");
        
        foreach (ItemData part in costumeSet.requiredParts)
        {
            if (part != null)
            {
                bool isCollected = collectedPartIds.Contains(part.id);
                result[part.id] = isCollected;
                Debug.Log($"파츠 '{part.ItemName}' (ID: {part.id}) 수집 상태: {isCollected}");
            }
            else
            {
                Debug.LogWarning($"null인 파츠가 '{costumeSet.costumeName}' 세트에 포함되어 있습니다.");
            }
        }

        return result;
    }

    // 모든 복장 세트 가져오기
    public List<CostumeSetData> GetAllCostumeSets()
    {
        return new List<CostumeSetData>(allCostumeSets);
    }

    // 복장 세트 가져오기
    public CostumeSetData GetCostumeSet(string costumeId)
    {
        // 잘못된 costumeId 처리
        if (string.IsNullOrEmpty(costumeId))
        {
            Debug.LogWarning("GetCostumeSet: costumeId가 null 또는 빈 문자열입니다.");
            return null;
        }

        // allCostumeSets가 null이거나 비어있는지 확인
        if (allCostumeSets == null || allCostumeSets.Count == 0)
        {
            Debug.LogWarning("GetCostumeSet: allCostumeSets가 초기화되지 않았거나 비어 있습니다.");
            return null;
        }

        return allCostumeSets.Find(c => c != null && c.costumeId == costumeId);
    }

    // 특정 복장이 활성화되어 있는지 확인
    public bool IsActiveCostume(string costumeId)
    {
        return activeCostumeSet != null && activeCostumeSet.costumeId == costumeId;
    }

    // 현재 활성화된 복장 가져오기
    public CostumeSetData GetActiveCostume()
    {
        return activeCostumeSet;
    }
}