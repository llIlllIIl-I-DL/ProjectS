using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossHealth : MonoBehaviour, IDamageable
{
    [SerializeField] public float maxHP;
    [SerializeField]  private float currentHP;

    public event System.Action OnBossDied;

    private Animator animator;
    public float damage;

    private void Awake()
    {
        currentHP = maxHP;
        animator = GetComponent<Animator>(); // Animator 연결
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        TakeDamage(damage);
    }

    public void TakeDamage(float damage)
    {
        if (currentHP <= 0) return;

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        Debug.Log($"[BossHealth] 데미지 받음! 남은 체력: {currentHP} !!!!");

        // 애니메이션 재생
        animator?.SetTrigger("setHit");

        if (currentHP == 0)
        {
            OnBossDied?.Invoke();
        }
    }

    public float GetCurrentHP() => currentHP;
}
