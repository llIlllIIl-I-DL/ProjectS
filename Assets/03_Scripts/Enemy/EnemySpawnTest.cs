using UnityEngine;
using System.Collections.Generic;

public class EnemySpawnTest : MonoBehaviour
{
    [SerializeField] private string[] enemyAddresses;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 2f;
    
    private float spawnTimer = 0f;
    private EnemyManager enemyManager;
    
    private void Start()
    {
        enemyManager = EnemyManager.Instance;
        
        // 적 프리팹 미리 로드 (선택 사항)
        List<string> addresses = new List<string>(enemyAddresses);
        enemyManager.PreloadEnemyPrefabs(addresses);
    }
    
    private void Update()
    {
        // 스페이스 바를 누르면 적 생성
        if (Input.GetKeyDown(KeyCode.P))
        {
            SpawnRandomEnemy();
        }
        
        // 또는 일정 간격으로 자동 생성
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnRandomEnemy();
        }
    }
    
    private void SpawnRandomEnemy()
    {
        if (enemyAddresses.Length == 0 || spawnPoints.Length == 0) return;
        
        // 랜덤 적 유형과 스폰 위치 선택
        string randomAddress = enemyAddresses[Random.Range(0, enemyAddresses.Length)];
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        // 적 생성
        BaseEnemy enemy = enemyManager.SpawnEnemy(
            randomAddress, 
            spawnPoint.position,
            Quaternion.identity
        );
        
        if (enemy == null)
            Debug.Log("비동기 생성 중...");
        else
            Debug.Log($"{randomAddress} 생성 완료!");
    }
}