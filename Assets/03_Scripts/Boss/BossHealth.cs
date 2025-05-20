using System;
using System.Collections;
using UnityEngine;

public class BossHealth : MonoBehaviour, IDamageable
{
    [Header("체력 설정")]
    [SerializeField] private float maxHP = 100f;
    [SerializeField] private float currentHP;

    [Header("이동 속도")]
    [SerializeField] private float moveSpeed = 1f;
    public float MoveSpeed 
    { 
        get { return moveSpeed; } 
        set { moveSpeed = value; } 
    }

    [Header("무적 시간")]
    [SerializeField] private float invincibilityTime = 0.5f;
    private bool isInvincible = false;

    // 이벤트
    public event Action OnBossDied;
    public event Action<float, float> OnBossHPChanged;

    // 프로퍼티
    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public bool IsDead { get; private set; } = false;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    [SerializeField] private Material hitMaterial;

    private void Awake()
    {
        // 컴포넌트 참조 가져오기
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.material;
        }
        
        // 초기 체력 설정
        currentHP = maxHP;
    }

    public void TakeDamage(float damage)
    {
        // 이미 사망했거나 무적 상태면 데미지 무시
        if (IsDead || isInvincible)
            return;

        // 데미지 적용
        currentHP -= damage;
        
        // 체력 변경 이벤트 발생
        OnBossHPChanged?.Invoke(maxHP, currentHP);
        
        // 피격 효과
        StartCoroutine(HitEffect());
        
        // 체력이 0 이하면 사망 처리
        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void ResetHealth()
    {
        currentHP = maxHP;
        IsDead = false;
        isInvincible = false;
        
        // 체력 변경 이벤트 발생
        OnBossHPChanged?.Invoke(maxHP, currentHP);
    }

    private void Die()
    {
        if (IsDead)
            return;
            
        IsDead = true;
        
        // 애니메이션 설정
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        
        // 사망 이벤트 발생
        OnBossDied?.Invoke();
        
        Debug.Log("보스가 사망했습니다!");
    }

    private IEnumerator HitEffect()
    {
        isInvincible = true;
        
        // 히트 이펙트 (깜빡임 등) 적용
        if (spriteRenderer != null && hitMaterial != null)
        {
            spriteRenderer.material = hitMaterial;
            
            yield return new WaitForSeconds(0.1f);
            
            spriteRenderer.material = originalMaterial;
        }
        
        // 무적 시간만큼 대기
        yield return new WaitForSeconds(invincibilityTime);
        
        isInvincible = false;
    }
} 