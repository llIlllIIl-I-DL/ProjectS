using UnityEngine;

public abstract class DebuffEffect : MonoBehaviour
{
    protected BaseEnemy targetEnemy;
    protected float duration;
    protected float intensity;
    protected float tickDamage;
    protected float tickTimer;
    protected DebuffType debuffType;
    protected GameObject visualEffect;

    private DebuffData debuffData;

    public DebuffType DebuffType => debuffType;

    protected virtual void Awake()
    {
        targetEnemy = GetComponent<BaseEnemy>();
    }

    public virtual void Initialize(DebuffType type, float duration, float intensity, float tickDamage, DebuffData data)
    {
        this.debuffType = type;
        this.duration = duration;
        this.intensity = intensity;
        this.tickDamage = tickDamage;
        this.debuffData = data;
        this.tickTimer = 0f;

        ApplyInitialEffect();
    }

    public virtual void RefreshDebuff(float newDuration, float newIntensity, float newTickDamage)
    {
        // 더 긴 지속시간이나 더 강한 효과로 갱신
        duration = Mathf.Max(duration, newDuration);
        intensity = Mathf.Max(intensity, newIntensity);
        tickDamage = Mathf.Max(tickDamage, newTickDamage);

        // 타이머 리셋
        tickTimer = 0f;
    }

    protected virtual void Update()
    {
        if (targetEnemy == null)
        {
            Destroy(this);
            return;
        }

        // 타이머 업데이트
        tickTimer += Time.deltaTime;

        // 틱 데미지 적용
        if (tickTimer >= 1.0f)
        {
            ApplyTickEffect();
            tickTimer -= 1.0f;
        }

        // 지속시간 감소
        duration -= Time.deltaTime;
        if (duration <= 0)
        {
            RemoveDebuff();
        }
    }

    // 초기 효과 적용 (각 디버프 타입별 구현)
    protected abstract void ApplyInitialEffect();

    // 틱당 효과 적용 (각 디버프 타입별 구현)
    protected abstract void ApplyTickEffect();

    // 디버프 제거 효과 (각 디버프 타입별 구현)
    protected abstract void RemoveEffect();

    // 디버프 제거 (공통 로직)
    public virtual void RemoveDebuff()
    {
        // 원래 상태로 복구
        RemoveEffect();

        // 시각 효과 제거
        if (visualEffect != null)
        {
            Destroy(visualEffect);
        }

        // 컴포넌트 제거
        Destroy(this);
    }

    protected virtual void OnDestroy()
    {
        // 디버프 효과가 제거될 때 정리 작업
        if (targetEnemy != null)
        {
            RemoveEffect();
        }
    }
}
