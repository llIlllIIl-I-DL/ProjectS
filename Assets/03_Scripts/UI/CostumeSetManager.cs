using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// CostumeSetManager 클래스 - 기존 코드와의 호환성을 위한 연결 클래스
// 기존 코드에서 CostumeSetManager를 참조하고 있으므로, 이를 CostumeManager로 연결하는 역할
public class CostumeSetManager : MonoBehaviour
{
    private static CostumeSetManager instance;
    public static CostumeSetManager Instance => instance;

    // CostumeManager 참조
    private CostumeManager costumeManager;

    // 세트 데이터 캐싱 (기존 코드와 호환성 유지)
    [SerializeField] private List<CostumeSetData> costumeSets = new List<CostumeSetData>();

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
        // CostumeManager 참조 가져오기
        costumeManager = CostumeManager.Instance;

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
        }
    }

    // 파츠 수집 이벤트 처리
    private void OnPartCollected(ItemData part)
    {
        // 필요한 추가 로직 (기존 코드와의 호환성을 위해)
        Debug.Log($"파츠 수집됨: {part.ItemName}");
    }

    // 복장 해금 이벤트 처리
    private void OnCostumeUnlocked(CostumeSetData costumeSet)
    {
        // 필요한 추가 로직 (기존 코드와의 호환성을 위해)
        Debug.Log($"복장 세트 해금됨: {costumeSet.costumeName}");
    }

    // 복장 활성화 이벤트 처리
    private void OnCostumeActivated(CostumeSetData costumeSet)
    {
        // 필요한 추가 로직 (기존 코드와의 호환성을 위해)
        Debug.Log($"복장 세트 활성화됨: {costumeSet.costumeName}");
    }

    // 기존 코드에서 호출하는 메서드들 (CostumeManager로 연결)

    // 아이템 획득 시 호출 (InventoryManager에서 호출)
    public void OnItemAcquired(ItemData item)
    {
        if (costumeManager != null && item.itemType == ItemType.CostumeParts)
        {
            costumeManager.AddPart(item);
        }
    }

    // 아이템 제거 시 호출 (InventoryManager에서 호출)
    public void OnItemRemoved(ItemData item)
    {
        // 필요한 로직 구현 (아이템 제거 시 복장 세트 상태 업데이트 등)
    }

    // 활성화된 복장 세트 설정
    public void SetActiveCostumeSet(CostumeSetData costumeSet)
    {
        if (costumeManager != null && costumeSet != null)
        {
            costumeManager.ActivateCostume(costumeSet.costumeId);
        }
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
}