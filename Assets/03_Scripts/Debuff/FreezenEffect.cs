using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreezeEffect : DebuffEffect
{
    private float originalSpeed;
    private Material originalMaterial;
    private Material freezeMaterial;

    protected override void ApplyInitialEffect()
    {
        // 원래 속도 저장
        originalSpeed = targetEnemy.GetMoveSpeed();

        // 속도 감소 (빙결은 더 큰 감소율)
        targetEnemy.SetMoveSpeed(originalSpeed * (1f - (intensity * 1.5f)));

        // 추가로 공격 속도 감소 적용 가능
        

        // 시각적 효과 적용
        ApplyVisualEffect(true);
    }

    protected override void ApplyTickEffect()
    {
        // 빙결 데미지
        targetEnemy.TakeDamage(tickDamage);

        // 추가 효과: 낮은 확률로 일시적 스턴
        if (Random.value < 0.05f * intensity)
        {
            // 스턴 효과 적용
        }
    }

    protected override void RemoveEffect()
    {
        // 원래 상태로 복구
        if (targetEnemy != null)
        {
            targetEnemy.SetMoveSpeed(originalSpeed);
            //targetEnemy.ResetAttackSpeedModifier();
            ApplyVisualEffect(false);
        }
    }

    private void ApplyVisualEffect(bool apply)
    {
        Renderer renderer = targetEnemy.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            if (apply)
            {
                // 빙결 효과 적용 (청색 색상)
                if (originalMaterial == null)
                {
                    originalMaterial = renderer.material;
                }

                if (freezeMaterial == null)
                {
                    freezeMaterial = new Material(originalMaterial);
                    freezeMaterial.color = new Color(0.7f, 0.9f, 1.0f);
                    // 얼음 효과를 위한 셰이더 파라미터 설정
                    if (freezeMaterial.HasProperty("_Glossiness"))
                    {
                        freezeMaterial.SetFloat("_Glossiness", 1.0f);
                    }
                }

                renderer.material = freezeMaterial;
            }
            else
            {
                // 원래 재질로 복구
                if (originalMaterial != null)
                {
                    renderer.material = originalMaterial;
                }
            }
        }
    }
}

