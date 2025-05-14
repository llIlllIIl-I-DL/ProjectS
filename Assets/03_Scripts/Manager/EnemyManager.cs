using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;

public class EnemyManager : Singleton<EnemyManager>
{
    // 적 프리팹 캐싱
    private Dictionary<string, GameObject> enemyPrefabs = new Dictionary<string, GameObject>();
    
    // 활성화된 적 리스트
    private List<BaseEnemy> activeEnemies = new List<BaseEnemy>();

    private Dictionary<string, Queue<BaseEnemy>> enemyPools = new Dictionary<string, Queue<BaseEnemy>>();
    [SerializeField] private int defaultPoolSize = 5; // 기본 풀 크기
    [SerializeField] private bool usePooling = true; // 풀링 사용 여부 (디버깅용)

    protected override void Awake()
    {
        base.Awake();
        
    }
    
    // 적 프리팹 미리 로드
    public void PreloadEnemyPrefabs(List<string> enemyAddresses)
    {
        foreach (string address in enemyAddresses)
        {
            LoadEnemyPrefab(address);
        }
    }
    
    // 적 프리팹 로드
    private void LoadEnemyPrefab(string address)
    {
        // 어드레서블 사용 기준??
        Addressables.LoadAssetAsync<GameObject>(address).Completed += (operation) =>
        {
            if (operation.Status == AsyncOperationStatus.Succeeded)
            {
                enemyPrefabs[address] = operation.Result;
                Debug.Log($"적 프리팹 로드 성공: {address}");
            }
            else
            {
                Debug.LogError($"적 프리팹 로드 실패: {address}");
            }
        };
    }

    // 풀 초기화 메서드
    private void InitializePool(string enemyAddress, int poolSize = 0)
    {
        if (poolSize <= 0) poolSize = defaultPoolSize;
        
        // 주소에 대한 풀이 없으면 생성
        if (!enemyPools.ContainsKey(enemyAddress))
            enemyPools[enemyAddress] = new Queue<BaseEnemy>();
            
        // 프리팹 로드 확인
        if (!enemyPrefabs.ContainsKey(enemyAddress))
        {
            LoadEnemyPrefab(enemyAddress);
            // 비동기 로드이므로 풀 초기화는 로드 완료 후 별도로 진행해야 함
            Addressables.LoadAssetAsync<GameObject>(enemyAddress).Completed += (op) => {
                if (op.Status == AsyncOperationStatus.Succeeded)
                    PopulatePool(enemyAddress, poolSize);
            };
        }
        else
        {
            // 이미 프리팹이 로드되어 있으면 바로 풀 채우기
            PopulatePool(enemyAddress, poolSize);
            Debug.Log($"풀 초기화 완료: {enemyAddress} x {poolSize}개");
        }
    }

    // 풀 채우기 (미리 인스턴스 생성)
    private void PopulatePool(string enemyAddress, int count)
    {
        GameObject prefab = enemyPrefabs[enemyAddress];
        
        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab);
            BaseEnemy enemy = obj.GetComponent<BaseEnemy>();
            
            if (enemy != null)
            {
                obj.SetActive(false);
                enemy.Initialize(enemyAddress, this); // BaseEnemy에 이 메서드 추가 필요
                enemyPools[enemyAddress].Enqueue(enemy);
            }
        }
        
        Debug.Log($"풀 생성 완료: {enemyAddress} x {count}개");
    }

    // 적 스폰
    public BaseEnemy SpawnEnemy(string enemyAddress, Vector3 position, Quaternion rotation)
    {
        // 풀링 사용 시
        if (usePooling)
        {
            // 풀이 없으면 초기화
            if (!enemyPools.ContainsKey(enemyAddress))
                InitializePool(enemyAddress);
                
            // 풀에서 사용 가능한 적 확인
            if (enemyPools.TryGetValue(enemyAddress, out Queue<BaseEnemy> pool) && pool.Count > 0)
            {
                BaseEnemy enemy = pool.Dequeue();
                
                // 적 활성화 및 위치 설정
                enemy.transform.position = position;
                enemy.transform.rotation = rotation;
                enemy.gameObject.SetActive(true);
                enemy.OnSpawned(); // 적 리셋/초기화 (BaseEnemy에 추가 필요)
                
                activeEnemies.Add(enemy);
                return enemy;
            }
        }
        
        // 기존 로드/생성 로직은 유지 (풀이 비어있거나 풀링을 사용하지 않을 때)
        if (enemyPrefabs.ContainsKey(enemyAddress))
        {
            // 캐싱된 프리팹 사용
            GameObject enemy = Instantiate(enemyPrefabs[enemyAddress], position, rotation);
            BaseEnemy baseEnemy = enemy.GetComponent<BaseEnemy>();
            if (baseEnemy != null)
                activeEnemies.Add(baseEnemy);
            return baseEnemy;
        }
        else
        {
            // 캐싱되지 않은 경우 바로 로드 후 생성
            Addressables.InstantiateAsync(enemyAddress, position, rotation).Completed += (operation) =>
            {
                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject enemy = operation.Result;
                    BaseEnemy baseEnemy = enemy.GetComponent<BaseEnemy>();
                    if (baseEnemy != null)
                        activeEnemies.Add(baseEnemy);
                }
                else
                {
                    Debug.LogError($"적 생성 실패: {enemyAddress}");
                }
            };
        }
        
        return null; // 비동기 생성 시에는 null 반환
    }

    // 풀로 적 반환
    public void ReturnToPool(BaseEnemy enemy, string poolKey)
    {
        if (!usePooling) 
        {
            Destroy(enemy.gameObject);
            return;
        }
        
        // 활성 목록에서 제거
        activeEnemies.Remove(enemy);
        
        // 오브젝트 비활성화
        enemy.gameObject.SetActive(false);
        
        // 풀이 없으면 생성
        if (!enemyPools.ContainsKey(poolKey))
            enemyPools[poolKey] = new Queue<BaseEnemy>();
            
        // 풀에 추가
        enemyPools[poolKey].Enqueue(enemy);
    }
    
    // 모든 적 제거
    public void ClearAllEnemies()
    {
        if (usePooling)
        {
            // 모든 활성 적을 풀로 반환
            foreach (var enemy in new List<BaseEnemy>(activeEnemies))
            {
                if (enemy != null)
                    enemy.ReturnToPool();
            }
            activeEnemies.Clear();
        }
        else
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null)
                    Destroy(enemy.gameObject);
            }
            activeEnemies.Clear();
        }
    }
    
    // 리소스 정리
    private void OnDestroy()
    {
        foreach (var prefab in enemyPrefabs.Values)
        {
            Addressables.Release(prefab);
        }
    }
}