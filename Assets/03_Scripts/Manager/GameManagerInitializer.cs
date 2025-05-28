using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 매니저 초기화 스크립트
/// 씬에 GameManager가 없으면 자동으로 생성합니다.
/// </summary>
public class GameManagerInitializer : MonoBehaviour
{
    [SerializeField] private GameObject gameManagerPrefab;

    private void Awake()
    {
        // 이미 GameManager가 존재하는지 확인
        if (GameManager.Instance == null && gameManagerPrefab != null)
        {
            // GameManager 프리팹 생성
            Instantiate(gameManagerPrefab);
            Debug.Log("GameManager를 생성했습니다.");
        }
    }

    private void Start()
    {
        // GameManager가 제대로 초기화되었는지 확인
        if (GameManager.Instance != null)
        {
            Debug.Log("GameManager 초기화 완료");
        }
        else
        {
            Debug.LogWarning("GameManager 초기화 실패");
        }
    }
} 