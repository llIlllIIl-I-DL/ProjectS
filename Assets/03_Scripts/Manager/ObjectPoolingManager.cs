using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal; // URP 카메라를 위한 네임스페이스

// 개별 오브젝트 풀 클래스
public class ObjectPool
{
    private GameObject prefab;
    private Queue<GameObject> pool = new Queue<GameObject>();
    private Transform poolParent;

    public Transform PoolParent => poolParent;  // poolParent에 접근하기 위한 프로퍼티 추가

    public ObjectPool(GameObject prefab, int initialSize, Transform parent)
    {
        this.prefab = prefab;
        this.poolParent = parent;
        
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewObject();
        }
    }

    private GameObject CreateNewObject()
    {
        GameObject obj = GameObject.Instantiate(prefab, poolParent);
        obj.SetActive(false);
        pool.Enqueue(obj);
        return obj;
    }

    public GameObject Get(Transform parent = null)
    {
        GameObject obj = pool.Count > 0 ? pool.Dequeue() : CreateNewObject();
        
        // 총알인 경우 부모를 null로 설정
        Bullet bullet = obj.GetComponent<Bullet>();
        if (bullet != null)
        {
            obj.transform.SetParent(null);
        }
        else if (parent != null)
        {
            obj.transform.SetParent(parent);
            obj.transform.localPosition = Vector3.zero;
        }
        
        if (bullet != null)
        {
            bullet.ResetBullet();
        }
        
        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null) return;
        
        // 파티클 시스템 리셋 (파티클인 경우)
        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Stop();
            ps.Clear();
        }

        // 총알 컴포넌트 초기화
        Bullet bullet = obj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.ResetBullet();
        }
        
        obj.SetActive(false);
        // 해당 풀 타입의 부모 오브젝트의 자식으로 반환
        obj.transform.SetParent(poolParent);
        pool.Enqueue(obj);
    }
}

// 오브젝트 풀링 매니저 (싱글톤)
public class ObjectPoolingManager : MonoBehaviour
{
    private static ObjectPoolingManager _instance;
    public static ObjectPoolingManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ObjectPoolingManager");
                _instance = go.AddComponent<ObjectPoolingManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // 프리팹 타입 열거형
    public enum PoolType
    {
        AcidBullet,
        DebuffEffect,  // 모든 디버프 이펙트를 하나로 통합
        // 총알 타입
        NormalBullet,
        RustBullet,
        IronBullet,
        PoisonBullet,
        WaterBullet,
        FlameBullet,
        IceBullet,
        EnemyBullet,
        BossBullet,
        // 필요한 타입 추가
    }

    [System.Serializable]
    public class PoolInfo
    {
        public PoolType type;
        public GameObject prefab;
        public int initialSize = 10;
    }

    // 디버프 타입별 프리팹 매핑
    [System.Serializable]
    public class DebuffPrefabInfo
    {
        public DebuffType debuffType;
        public GameObject prefab;
    }

    [SerializeField] private List<PoolInfo> poolInfos = new List<PoolInfo>();
    [SerializeField] private List<DebuffPrefabInfo> debuffPrefabs = new List<DebuffPrefabInfo>();
    
    // 풀 저장소
    private Dictionary<PoolType, ObjectPool> pools = new Dictionary<PoolType, ObjectPool>();
    private Dictionary<DebuffType, GameObject> debuffPrefabMap = new Dictionary<DebuffType, GameObject>();

    // ElementType과 PoolType 매핑
    private Dictionary<ElementType, PoolType> bulletTypeToPoolType = new Dictionary<ElementType, PoolType>();

    [SerializeField] private Collider2D bulletBoundary; // 인스펙터에서 직접 할당 가능

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 디버프 프리팹 매핑
        InitializeDebuffPrefabMapping();
        
        // 풀 초기화
        InitializePools();

        // 초기화 메서드에 추가
        InitializeBulletMapping();

        // 인스펙터에서 할당하지 않았다면 Find로 시도
        if (bulletBoundary == null)
        {
            GameObject boundaryObj = GameObject.FindWithTag("BulletBoundary");
            if (boundaryObj != null)
                bulletBoundary = boundaryObj.GetComponent<Collider2D>();
        }
        if (bulletBoundary == null)
            Debug.LogWarning("플레이어의 경계 콜라이더(bulletBoundary)를 찾을 수 없습니다! 총알 반환이 정상 동작하지 않을 수 있습니다.");
    }

    private void Update()
    {
        CheckBulletsInBoundary();
    }

    private void CheckBulletsInBoundary()
    {
        if (bulletBoundary == null) return;

        Bullet[] bullets = GameObject.FindObjectsOfType<Bullet>(false);
        foreach (var bullet in bullets)
        {
            if (!bullet.gameObject.activeInHierarchy) continue;

            Vector3 bulletPosition = bullet.transform.position;
            if (!bulletBoundary.bounds.Contains(bulletPosition))
            {
                Debug.Log($"총알 위치 {bulletPosition} / 경계 {bulletBoundary.bounds.center}, {bulletBoundary.bounds.size}");
                ObjectPoolingManager.Instance.ReturnBullet(bullet.gameObject, bullet.BulletType);
            }
        }
    }

    private void InitializeDebuffPrefabMapping()
    {
        foreach (DebuffPrefabInfo info in debuffPrefabs)
        {
            debuffPrefabMap[info.debuffType] = info.prefab;
        }
    }

    private void InitializePools()
    {
        // 각 풀의 부모 오브젝트 생성
        Transform poolsParent = new GameObject("Pools").transform;
        poolsParent.SetParent(transform);

        foreach (PoolInfo info in poolInfos)
        {
            if (info.prefab != null)
            {
                // 풀 타입별 부모 오브젝트 생성
                Transform poolParent = new GameObject(info.type.ToString()).transform;
                poolParent.SetParent(poolsParent);
                
                // 풀 생성
                pools[info.type] = new ObjectPool(info.prefab, info.initialSize, poolParent);
            }
        }
    }

    // 초기화 메서드에 추가
    private void InitializeBulletMapping()
    {
        // 총알 타입과 풀 타입 매핑
        bulletTypeToPoolType[ElementType.Normal] = PoolType.NormalBullet;
        bulletTypeToPoolType[ElementType.Rust] = PoolType.RustBullet;
        bulletTypeToPoolType[ElementType.Iron] = PoolType.IronBullet;
        bulletTypeToPoolType[ElementType.Poison] = PoolType.PoisonBullet;
        bulletTypeToPoolType[ElementType.Water] = PoolType.WaterBullet;
        bulletTypeToPoolType[ElementType.Flame] = PoolType.FlameBullet;
        bulletTypeToPoolType[ElementType.Ice] = PoolType.IceBullet;
    }

    // 총알 가져오기
    public GameObject GetBullet(ElementType bulletType, Transform parent = null)
    {
        if (bulletTypeToPoolType.TryGetValue(bulletType, out PoolType poolType))
        {
            return GetObject(poolType, parent);
        }
        return null;
    }

    // 디버프 이펙트 가져오기
    public GameObject GetDebuffEffect(DebuffType debuffType, Transform parent = null)
    {
        if (debuffPrefabMap.TryGetValue(debuffType, out GameObject prefab))
        {
            return GetObject(PoolType.DebuffEffect, parent);
        }
        return null;
    }

    // 오브젝트 가져오기 (일반)
    public GameObject GetObject(PoolType type, Transform parent = null)
    {
        if (pools.TryGetValue(type, out ObjectPool pool))
        {
            return pool.Get(parent);
        }
        
        Debug.LogWarning($"Pool for type {type} not found!");
        return null;
    }

    // 총알 반환하기
    public void ReturnBullet(GameObject bullet, ElementType bulletType)
    {
        // EnemyBullet 컴포넌트가 있으면 무조건 EnemyBullet 풀로 반환
        if (bullet.GetComponent<EnemyBullet>() != null)
        {
            ReturnObject(bullet, PoolType.EnemyBullet);
            return;
        }

        if (bulletTypeToPoolType.TryGetValue(bulletType, out PoolType poolType))
        {
            ReturnObject(bullet, poolType);
        }
    }

    // 디버프 이펙트 반환하기
    public void ReturnDebuffEffect(GameObject obj, DebuffType debuffType)
    {
        if (debuffPrefabMap.TryGetValue(debuffType, out GameObject prefab))
        {
            ReturnObject(obj, PoolType.DebuffEffect);
        }
    }

    // 오브젝트 반환하기
    public void ReturnObject(GameObject obj, PoolType type)
    {
        if (pools.TryGetValue(type, out ObjectPool pool))
        {
            pool.Return(obj);
        }
    }
}