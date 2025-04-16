using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 초기화 매니저
/// 씬이 시작될 때 필요한 매니저들이 생성되었는지 확인하고, 없으면 생성합니다.
/// </summary>
public class SceneInitializer : MonoBehaviour
{
    [Header("매니저 프리팹")]
    [SerializeField] private GameObject gameManagerPrefab;
    [SerializeField] private GameObject inventoryManagerPrefab;
    [SerializeField] private GameObject itemManagerPrefab;
    [SerializeField] private GameObject costumeManagerPrefab;
    [SerializeField] private GameObject audioManagerPrefab;

    // 초기화 완료 플래그
    private bool isInitialized = false;

    private void Awake()
    {
        if (isInitialized) return;

        Debug.Log("씬 초기화 시작...");
        
        // 매니저들을 순서대로 초기화
        InitializeManager<GameManager>(gameManagerPrefab);
        InitializeManager<InventoryManager>(inventoryManagerPrefab);
        InitializeManager<ItemManager>(itemManagerPrefab);
        InitializeManager<CostumeManager>(costumeManagerPrefab);
        InitializeManager<AudioManager>(audioManagerPrefab);

        isInitialized = true;
        Debug.Log("씬 초기화 완료");
    }

    // 매니저 초기화 메서드
    private void InitializeManager<T>(GameObject managerPrefab) where T : MonoBehaviour
    {
        // 이미 해당 타입의 매니저가 존재하는지 확인
        T existingManager = FindObjectOfType<T>();
        
        if (existingManager == null && managerPrefab != null)
        {
            // 매니저 프리팹 생성
            GameObject managerObj = Instantiate(managerPrefab);
            Debug.Log($"{typeof(T).Name}를 생성했습니다.");
        }
        else
        {
            Debug.Log($"{typeof(T).Name}가 이미 존재합니다.");
        }
    }
} 