using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossHealth : MonoBehaviour, IDebuffable
{
    [SerializeField] public float maxHP;
    [SerializeField] private float currentHP;
    [SerializeField] private float defence;
    [SerializeField] private float moveSpeed = 3;

    public float Defence
    {
        get => defence;
        set => defence = value;
    }
    public float CurrentHP
    {
        get => currentHP;
        set => currentHP = value;
    }
    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }


    public event System.Action OnBossDied;

    private Animator animator;
    public float damage;

    private void Awake()
    {
        currentHP = maxHP;
        animator = GetComponent<Animator>(); // Animator 연결

        // 보스가 죽었을 때 상태머신에 알림
        OnBossDied += HandleStateMachineNotification;

    }

    private void HandleStateMachineNotification()
    {
        BossStateMachine stateMachine = GetComponent<BossStateMachine>();
        if (stateMachine != null)
        {
            stateMachine.SetDead(); // 보스가 죽음 처리
        }
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(GameConstants.Tags.PLAYERATTACK))
        {
            TakeDamage(damage);
        }

        return;
    }

    public void TakeDamage(float damage)
    {
        if (currentHP <= 0) return;

        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        Debug.Log($"[BossHealth] 데미지 받음! 남은 체력: {currentHP}");

        // 애니메이션 재생
        animator?.SetTrigger(GameConstants.AnimParams.HIT);

        if (currentHP <= 0)
        {
            OnBossDied?.Invoke();
        }
    }

    public float GetCurrentHP() => currentHP;
}
