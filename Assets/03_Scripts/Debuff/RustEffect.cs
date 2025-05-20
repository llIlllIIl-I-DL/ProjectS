using UnityEngine;

public class RustEffect : DebuffEffect
{
    private float originalSpeed;
    private float originalDefence;

    protected override void ApplyInitialEffect()
    {
        if (targetDebuffable == null) return;

        // 원래 속도 저장
        originalSpeed = targetDebuffable.MoveSpeed;
        originalDefence = targetDebuffable.Defence;

        // 속도 감소 적용 (intensity는 0.0-1.0 사이의 값, 감소율을 나타냄)
        targetDebuffable.MoveSpeed = originalSpeed * (1f - intensity);
        // 방어력 감소 적용
        targetDebuffable.Defence = originalDefence * (1f - intensity);

        // 시각적 효과 적용 (색상 변경 등)
        ApplyVisualEffect(true);
        
        // 필요하다면 visualEffect 관련 추가 설정
        if (visualEffect != null)
        {
            // 예: 파티클 색상 변경 등
            ParticleSystem ps = visualEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = new Color(0.7f, 1.0f, 0.7f);
            }
        }
    }

    protected override void ApplyTickEffect()
    {
        if (targetDebuffable == null) return;
        // 틱당 데미지 적용
        targetDebuffable.TakeDamage(tickDamage);
    }

    protected override void RemoveEffect()
    {
        if (targetDebuffable != null)
        {
            targetDebuffable.MoveSpeed = originalSpeed;
            targetDebuffable.Defence = originalDefence;
            ApplyVisualEffect(false);
        }
    }

    private void ApplyVisualEffect(bool apply)
    {
        SpriteRenderer renderer = targetDebuffable.gameObject.GetComponentInChildren<SpriteRenderer>();
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
