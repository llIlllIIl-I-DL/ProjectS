using UnityEngine;

public class DebuffManager : MonoBehaviour
{
    private BaseEnemy targetEnemy;
    private float duration;
    private float speedReductionFactor;
    private float damagePerSecond;
    private float originalSpeed;
    private float originalAttackPower;
    private float currentSpeed;
    private float currentAttackPower;
    private float timer;

    private void Awake()
    {
        targetEnemy = GetComponent<BaseEnemy>();
    }

    public void Initialize(float debuffDuration, float speedReductionPercent, float dotDamage)
    {
        duration = debuffDuration;
        speedReductionFactor = speedReductionPercent / 100f;
        damagePerSecond = dotDamage;
        timer = 0f;

        // 원래 속도 저장 및 감소된 속도 적용
        originalSpeed = targetEnemy.SetMoveSpeed();
        currentSpeed = originalSpeed * (1f - speedReductionFactor);

        // 디버프 시각적 표시 (예: 색상 변경)
        ApplyVisualEffect(true);
    }

    public void RefreshDuration(float newDuration)
    {
        duration = newDuration;
        timer = 0f;
    }

    private void Update()
    {
        if (targetEnemy == null)
        {
            Destroy(this);
            return;
        }

        // 타이머 업데이트
        timer += Time.deltaTime;

        // 지속 데미지 적용
        if (timer >= 1.0f)
        {
            targetEnemy.TakeDamage(damagePerSecond);
            timer -= 1.0f; // 1초마다 데미지
        }

        // 디버프 지속시간 체크
        duration -= Time.deltaTime;
        if (duration <= 0)
        {
            RemoveDebuff();
        }
    }

    private void RemoveDebuff()
    {
        // 원래 속도로 복구
        if (targetEnemy != null)
        {
            targetEnemy.SetMoveSpeed(originalSpeed);
            ApplyVisualEffect(false);
        }

        Destroy(this);
    }

    private void ApplyVisualEffect(bool apply)
    {
        // 디버프 시각적 효과 (예: 색상 변경)
        SpriteRenderer renderer = targetEnemy.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
        {
            if (apply)
            {
                renderer.color = new Color(0.7f, 1.0f, 0.7f); // 녹색빛 산성 효과
            }
            else
            {
                renderer.color = Color.white; // 원래 색상으로 복구
            }
        }
    }

    private void OnDestroy()
    {
        // 컴포넌트가 제거될 때 원래 상태로 복구
        if (targetEnemy != null)
        {
            targetEnemy.SetMoveSpeed(originalSpeed);
            ApplyVisualEffect(false);
        }
    }
}

