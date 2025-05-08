using System.Collections.Generic;
using UnityEngine;

// 디버프 타입 열거형
public enum DebuffType
{
    None,
    Rust,       // 산성/부식
    Freeze,     // 빙결
    Burn,       // 화상
    Poison,     // 독
    // 추가 디버프 타입
}

// 디버프 데이터를 담는 구조체
[System.Serializable]
public struct DebuffData
{
    public DebuffType type;
    public float duration;
    public float intensity;  // 디버프 효과의 강도 (%, 0.0-1.0)
    public float tickDamage; // 초당 데미지
    public GameObject visualEffectPrefab;
    public Color tintColor;  // 적용될 색상
    public AudioClip effectSound;
}

// 디버프 매니저
public class DebuffManager : MonoBehaviour
{
    private static DebuffManager _instance;
    public static DebuffManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("DebuffManager");
                _instance = go.AddComponent<DebuffManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Default Debuff Settings")]
    [SerializeField] private List<DebuffData> defaultDebuffs = new List<DebuffData>();

    // 디버프 타입별 데이터 캐시
    private Dictionary<DebuffType, DebuffData> debuffDataCache = new Dictionary<DebuffType, DebuffData>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 기본 디버프 데이터 캐싱
        CacheDefaultDebuffs();
    }

    private void CacheDefaultDebuffs()
    {
        foreach (DebuffData data in defaultDebuffs)
        {
            debuffDataCache[data.type] = data;
        }
    }

    // 디버프 적용 메서드
    public void ApplyDebuff(BaseEnemy enemy, DebuffType type, float duration = -1, float intensity = -1, float tickDamage = -1)
    {
        if (enemy == null) return;

        // 디버프 데이터 가져오기
        if (!debuffDataCache.TryGetValue(type, out DebuffData data))
        {
            Debug.LogWarning($"Debuff type {type} not found in cache.");
            return;
        }

        // 사용자가 지정한 값이 있으면 오버라이드
        float finalDuration = duration > 0 ? duration : data.duration;
        float finalIntensity = intensity > 0 ? intensity : data.intensity;
        float finalTickDamage = tickDamage > 0 ? tickDamage : data.tickDamage;

        // 해당 적에게 디버프 컴포넌트 찾기
        DebuffEffect existingDebuff = GetDebuffComponent(enemy, type);

        if (existingDebuff != null)
        {
            // 이미 디버프가 있다면 갱신
            existingDebuff.RefreshDebuff(finalDuration, finalIntensity, finalTickDamage);
        }
        else
        {
            // 새로운 디버프 적용
            CreateDebuffEffect(enemy, type, data, finalDuration, finalIntensity, finalTickDamage);
        }
    }

    // 특정 타입의 디버프 컴포넌트 가져오기
    private DebuffEffect GetDebuffComponent(BaseEnemy enemy, DebuffType type)
    {
        DebuffEffect[] debuffs = enemy.GetComponents<DebuffEffect>();
        foreach (DebuffEffect debuff in debuffs)
        {
            if (debuff.DebuffType == type)
            {
                return debuff;
            }
        }
        return null;
    }

    // 새 디버프 효과 생성
    private void CreateDebuffEffect(BaseEnemy enemy, DebuffType type, DebuffData data, float duration, float intensity, float tickDamage)
    {
        switch (type)
        {
            case DebuffType.Rust:
                RustEffect rustEffect = enemy.gameObject.AddComponent<RustEffect>();
                rustEffect.Initialize(type, duration, intensity, tickDamage, data);
                break;
/*
            case DebuffType.Freeze:
                FreezeEffect freezeEffect = enemy.gameObject.AddComponent<FreezeEffect>();
                freezeEffect.Initialize(type, duration, intensity, tickDamage, data);
                break;

            case DebuffType.Burn:
                BurnEffect burnEffect = enemy.gameObject.AddComponent<BurnEffect>();
                burnEffect.Initialize(type, duration, intensity, tickDamage, data);
                break;

            // 추가 디버프 타입 처리...

            default:
                GenericDebuffEffect genericEffect = enemy.gameObject.AddComponent<GenericDebuffEffect>();
                genericEffect.Initialize(type, duration, intensity, tickDamage, data);
                break;*/
        }

        // 시각 효과 생성
        CreateVisualEffect(enemy, data);
    }

    // 시각 효과 생성
    private void CreateVisualEffect(BaseEnemy enemy, DebuffData data)
    {
        if (data.visualEffectPrefab != null)
        {
            GameObject effect = Instantiate(data.visualEffectPrefab, enemy.transform);
            effect.transform.localPosition = Vector3.zero;

            // 효과 자동 제거는 DebuffEffect에서 처리
        }

        // 사운드 효과 재생
        if (data.effectSound != null)
        {
            AudioSource audio = enemy.GetComponent<AudioSource>();
            if (audio == null)
            {
                audio = enemy.gameObject.AddComponent<AudioSource>();
            }

            audio.PlayOneShot(data.effectSound);
        }
    }

    // 디버프 제거
    public void RemoveDebuff(BaseEnemy enemy, DebuffType type)
    {
        if (enemy == null) return;

        DebuffEffect debuff = GetDebuffComponent(enemy, type);
        if (debuff != null)
        {
            debuff.RemoveDebuff();
        }
    }

    // 모든 디버프 제거
    public void RemoveAllDebuffs(BaseEnemy enemy)
    {
        if (enemy == null) return;

        DebuffEffect[] debuffs = enemy.GetComponents<DebuffEffect>();
        foreach (DebuffEffect debuff in debuffs)
        {
            debuff.RemoveDebuff();
        }
    }
}