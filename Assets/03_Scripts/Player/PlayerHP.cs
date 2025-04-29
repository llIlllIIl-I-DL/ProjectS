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
    private bool isDead = false;

    private Player player;
    
    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;

    private void Awake()
    {
        // maxHP를 0과 100 사이로 클램프
        maxHP = Mathf.Clamp(maxHP, MIN_HP, MAX_HP);
        currentHP = maxHP;
        playerStateManager = GetComponent<PlayerStateManager>();
        player = GetComponent<Player>();
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

        float changedMaxHP = maxHP * (amount / maxHP);
        float actualIncrease = previousMaxHP - changedMaxHP; // 최대 HP 증가량

        maxHP = previousMaxHP + changedMaxHP;

        // 현재 HP도 최대 HP를 초과하지 않도록 조정

        float previousCurrentHP = currentHP;
        currentHP = Mathf.Clamp(currentHP + actualIncrease, MIN_HP, maxHP);

        Debug.Log($"최대 HP가 {maxHP - previousMaxHP}만큼 증가했습니다. 새로운 최대 HP: {maxHP}");

        player.UpdateCurrentPlayerHP(maxHP);
    }

    // 체력 초기화 (부활 시 사용)
    public void ResetHealth()
    {
        currentHP = maxHP;
        isDead = false; // 사망 상태 초기화
        Debug.Log($"플레이어 체력 초기화: {currentHP}/{maxHP}");
        
        // 체력바 UI 업데이트
        PlayerUI.Instance?.SetHealthBar(maxHP, currentHP);
    }

    private void Die()
    {
        // 이미 사망 상태면 중복 처리 방지
        if (isDead) return;
        
        isDead = true;
        Debug.Log("플레이어가 사망했습니다.");
        
        // 사망 상태로 전환
        if (playerStateManager != null)
        {
            playerStateManager.ChangeState(PlayerStateType.Death);
        }
        
        // GameManager에 사망 알림
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDied(gameObject);
        }
    }

    // 최대 체력을 반환하는 메서드
    public float GetMaxHealth()
    {
        return maxHP;
    }
}
