using UnityEngine;

public class RustEffect : DebuffEffect
{
    private float originalSpeed;

    protected override void ApplyInitialEffect()
    {
        // 원래 속도 저장
        originalSpeed = targetEnemy.GetMoveSpeed();

        // 속도 감소 적용 (intensity는 0.0-1.0 사이의 값, 감소율을 나타냄)
        targetEnemy.SetMoveSpeed(originalSpeed * (1f - intensity));

        // 시각적 효과 적용 (색상 변경 등)
        ApplyVisualEffect(true);
    }

    protected override void ApplyTickEffect()
    {
        // 틱당 데미지 적용
        targetEnemy.TakeDamage(tickDamage);

    }

    protected override void RemoveEffect()
    {
        // 원래 속도로 복구
        if (targetEnemy != null)
        {
            targetEnemy.SetMoveSpeed(originalSpeed);
            ApplyVisualEffect(false);
        }
    }

    private void ApplyVisualEffect(bool apply)
    {
        SpriteRenderer renderer = targetEnemy.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
        {
            if (apply)
            {
                // 시각적 효과 적용 (녹색 산성 색상)
                renderer.color = new Color(0.7f, 1.0f, 0.7f);
            }
            else
            {
                // 원래 색상으로 복구
                renderer.color = Color.white;
            }
        }
    }
}
