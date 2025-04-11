using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHP : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHP = 10;
    [SerializeField] private float currentHP;

    private readonly float MIN_HP = 0f;
    private readonly float MAX_HP = 100f;

    private PlayerStateManager playerStateManager;
    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;

    private void Awake()
    {
        // maxHP를 0과 100 사이로 클램프
        maxHP = Mathf.Clamp(maxHP, MIN_HP, MAX_HP);
        currentHP = maxHP;
        playerStateManager = GetComponent<PlayerStateManager>();
    }


    public void TakeDamage(float damage)
    {
        currentHP = Mathf.Clamp(currentHP - damage, MIN_HP, maxHP);
        Debug.Log($"플레이어가 {damage} 데미지를 입었습니다. 현재 HP: {currentHP}");

        // 플레이어가 데미지를 입으면 Hit 상태로 변경
        if (playerStateManager != null)
        {
            PlayerUI.Instance.SetHealthBar(maxHP, currentHP);
            playerStateManager.ChangeState(PlayerStateType.Hit);
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, MIN_HP, maxHP);
        Debug.Log($"플레이어가 {amount}만큼 회복했습니다. 현재 HP: {currentHP}");
    }

    public void IncreaseMaxHP(float amount)
    {
        if (amount <= 0) return; // 0 이하의 값은 무시

        float previousMaxHP = maxHP; // 이전 최대 HP 저장
        maxHP += amount; 
        float actualIncrease = maxHP - previousMaxHP; // 최대 HP 증가량

        // 현재 HP도 최대 HP를 초과하지 않도록 조정
        float previousCurrentHP = currentHP;
        currentHP = Mathf.Clamp(currentHP + actualIncrease, MIN_HP, maxHP);

        Debug.Log($"최대 HP가 {maxHP - previousMaxHP}만큼 증가했습니다. 새로운 최대 HP: {maxHP}");
    }

    private void Die()
    {
        Debug.Log("플레이어가 사망했습니다.");
    }
}
