using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 테스트용 아이템 생성기 클래스
public class ItemDemo : MonoBehaviour
{
    [Header("아이템 설정")]
    [SerializeField] private ItemData[] availableItems;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private KeyCode spawnItemKey = KeyCode.I;
    [SerializeField] private KeyCode useItemKey = KeyCode.U;

    private ItemManager itemManager;
    
    private void Start()
    {
        // ItemManager 찾기
        itemManager = FindObjectOfType<ItemManager>();
        if (itemManager == null)
        {
            Debug.LogError("ItemManager를 찾을 수 없습니다. 씬에 ItemManager를 추가해주세요.");
        }
        
        // 아이템 데이터 로드
        if (availableItems == null || availableItems.Length == 0)
        {
            LoadDefaultItems();
        }
    }
    
    private void Update()
    {
        // 아이템 스폰 키 입력 감지
        if (Input.GetKeyDown(spawnItemKey) && itemManager != null)
        {
            SpawnRandomItem();
        }
        
        // 아이템 사용 키 입력 감지
        if (Input.GetKeyDown(useItemKey) && itemManager != null)
        {

        }
    }
    
    // 랜덤 위치에 랜덤 아이템 스폰
    private void SpawnRandomItem()
    {
        if (availableItems.Length == 0 || spawnPoints.Length == 0)
        {
            Debug.LogWarning("아이템 또는 스폰 위치가 설정되지 않았습니다.");
            return;
        }
        
        // 랜덤 아이템 선택
        ItemData randomItem = availableItems[Random.Range(0, availableItems.Length)];
        
        // 랜덤 위치 선택
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        // 아이템 생성
        itemManager.SpawnItem(randomItem, spawnPoint.position);
        Debug.Log($"{randomItem.ItemName} 아이템이 생성되었습니다.");
    }
    
    
    // Resources 폴더에서 기본 아이템 로드
    private void LoadDefaultItems()
    {
        ItemData[] loadedItems = Resources.LoadAll<ItemData>("Items");
        if (loadedItems != null && loadedItems.Length > 0)
        {
            availableItems = loadedItems;
            Debug.Log($"{availableItems.Length}개의 아이템이 로드되었습니다.");
        }
        else
        {
            Debug.LogWarning("Resources/Items 폴더에서 아이템을 찾을 수 없습니다.");
        }
    }
} 