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
    [SerializeField] private List<DebuffDataSO> defaultDebuffs = new List<DebuffDataSO>();

    // 디버프 타입별 데이터 캐시
    private Dictionary<DebuffType, DebuffDataSO> debuffDataCache = new Dictionary<DebuffType, DebuffDataSO>();

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
        foreach (DebuffDataSO data in defaultDebuffs)
        {
            debuffDataCache[data.type] = data;
        }
    }

    // 디버프 적용 메서드
    public void ApplyDebuff(IDebuffable target, DebuffType type, float duration = -1, float intensity = -1, float tickDamage = -1)
    {
        if (target == null) return;

        // 디버프 데이터 가져오기
        if (!debuffDataCache.TryGetValue(type, out DebuffDataSO data))
        {
            Debug.LogWarning($"Debuff type {type} not found in cache.");
            return;
        }

        // 사용자가 지정한 값이 있으면 오버라이드
        float finalDuration = duration > 0 ? duration : data.duration;
        float finalIntensity = intensity > 0 ? intensity : data.intensity;
        float finalTickDamage = tickDamage > 0 ? tickDamage : data.tickDamage;

        // 해당 적에게 디버프 컴포넌트 찾기
        DebuffEffect existingDebuff = GetDebuffComponent(target, type);

        if (existingDebuff != null)
        {
            // 이미 디버프가 있다면 갱신
            existingDebuff.RefreshDebuff(finalDuration, finalIntensity, finalTickDamage);
        }
        else
        {
            // 새로운 디버프 적용
            CreateDebuffEffect(target, type, data, finalDuration, finalIntensity, finalTickDamage);
        }
    }

    // 특정 타입의 디버프 컴포넌트 가져오기
    private DebuffEffect GetDebuffComponent(IDebuffable target, DebuffType type)
    {
        var mono = target as MonoBehaviour;
        if (mono == null) return null;
        DebuffEffect[] debuffs = mono.GetComponents<DebuffEffect>();
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
    private void CreateDebuffEffect(IDebuffable target, DebuffType type, DebuffDataSO data, float duration, float intensity, float tickDamage)
    {
        switch (type)
        {
            case DebuffType.Rust:
                RustEffect rustEffect = target.gameObject.AddComponent<RustEffect>();
                rustEffect.Initialize(type, duration, intensity, tickDamage, data);
                break;
/*
            case DebuffType.Freeze:
                FreezeEffect freezeEffect = target.gameObject.AddComponent<FreezeEffect>();
                freezeEffect.Initialize(type, duration, intensity, tickDamage, data);
                break;

            case DebuffType.Burn:
                BurnEffect burnEffect = target.gameObject.AddComponent<BurnEffect>();
                burnEffect.Initialize(type, duration, intensity, tickDamage, data);
                break;

            // 추가 디버프 타입 처리...

            default:
                GenericDebuffEffect genericEffect = target.gameObject.AddComponent<GenericDebuffEffect>();
                genericEffect.Initialize(type, duration, intensity, tickDamage, data);
                break;*/
        }

        // 시각 효과 생성
        CreateVisualEffect(target, data);
    }

    // 시각 효과 생성
    private void CreateVisualEffect(IDebuffable target, DebuffDataSO data)
    {
        if (data.visualEffectPrefab != null)
        {
            // 풀링 매니저 사용
            GameObject effect = ObjectPoolingManager.Instance.GetDebuffEffect(data.type, target.transform);
            
            // 이펙트 참조를 DebuffEffect에 전달
            DebuffEffect debuffEffect = GetDebuffComponent(target, data.type);
            if (debuffEffect != null)
            {
                debuffEffect.SetVisualEffect(effect);
            }
        }

        // 사운드 효과 재생
        if (data.effectSound != null)
        {
            AudioSource audio = null;
            var mono = target as MonoBehaviour;
            if (mono != null)
            {
                audio = mono.GetComponent<AudioSource>();
                if (audio == null)
                {
                    audio = mono.gameObject.AddComponent<AudioSource>();
                }
                audio.PlayOneShot(data.effectSound);
            }
        }
    }

    // 디버프 제거
    public void RemoveDebuff(IDamageable target, DebuffType type)
    {
        if (target == null) return;
        var mono = target as MonoBehaviour;
        if (mono == null) return;
        DebuffEffect debuff = GetDebuffComponent(target as IDebuffable, type);
        if (debuff != null)
        {
            debuff.RemoveDebuff();
        }
    }

    // 모든 디버프 제거
    public void RemoveAllDebuffs(IDamageable target)
    {
        if (target == null) return;
        var mono = target as MonoBehaviour;
        if (mono == null) return;
        DebuffEffect[] debuffs = mono.GetComponents<DebuffEffect>();
        foreach (DebuffEffect debuff in debuffs)
        {
            debuff.RemoveDebuff();
        }
    }
}