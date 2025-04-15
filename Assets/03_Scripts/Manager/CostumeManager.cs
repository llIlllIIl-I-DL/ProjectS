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

        if (inventoryManager != null)
        {
            // 인벤토리 변경 이벤트 구독
            inventoryManager.OnItemAdded += OnItemAdded;
        }

        // 초기 복장 해금 상태 검사
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
            return false;
        }

        // 이미 수집한 파츠인지 확인
        if (collectedPartIds.Contains(part.id))
        {
            return false;
        }

        // 파츠 추가
        collectedPartIds.Add(part.id);

        // 이벤트 발생
        OnPartCollected?.Invoke(part);

        // 복장 해금 상태 검사
        CheckCostumeUnlocks(part.costumeSetId);

        return true;
    }

    // 특정 복장 세트의 해금 상태 검사
    private void CheckCostumeUnlocks(string costumeSetId)
    {
        CostumeSetData costumeSet = allCostumeSets.Find(c => c.costumeId == costumeSetId);
        if (costumeSet == null || costumeSet.isUnlocked)
        {
            return;
        }

        // 모든 필요 파츠를 수집했는지 확인
        bool allPartsCollected = true;
        foreach (ItemData part in costumeSet.requiredParts)
        {
            if (!collectedPartIds.Contains(part.id))
            {
                allPartsCollected = false;
                break;
            }
        }

        // 모든 파츠를 수집했으면 복장 해금
        if (allPartsCollected)
        {
            costumeSet.isUnlocked = true;
            OnCostumeUnlocked?.Invoke(costumeSet);
        }
    }

    // 모든 복장 세트의 해금 상태 검사
    private void CheckAllCostumeUnlocks()
    {
        foreach (CostumeSetData costumeSet in allCostumeSets)
        {
            if (!costumeSet.isUnlocked)
            {
                bool allPartsCollected = true;
                foreach (ItemData part in costumeSet.requiredParts)
                {
                    if (!collectedPartIds.Contains(part.id))
                    {
                        allPartsCollected = false;
                        break;
                    }
                }

                if (allPartsCollected)
                {
                    costumeSet.isUnlocked = true;
                    OnCostumeUnlocked?.Invoke(costumeSet);
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

        foreach (ItemData part in costumeSet.requiredParts)
        {
            if (part != null)
            {
                result[part.id] = collectedPartIds.Contains(part.id);
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