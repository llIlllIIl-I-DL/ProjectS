using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossHealth : MonoBehaviour
{
    [SerializeField] public int maxHP = 100;
    [SerializeField]  private int currentHP;

    public event System.Action OnBossDied;

    private Animator animator;

    private void Awake()
    {
        currentHP = maxHP;
        animator = GetComponent<Animator>(); // Animator 연결
    }

    public void TakeDamage(int damage)
    {
        if (currentHP <= 0) return;

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        Debug.Log($"[BossHealth] 데미지 받음! 남은 체력: {currentHP}");

        // 애니메이션 재생
        animator?.SetTrigger("setHit");

        if (currentHP == 0)
        {
            OnBossDied?.Invoke();
        }
    }

    public int GetCurrentHP() => currentHP;
}
