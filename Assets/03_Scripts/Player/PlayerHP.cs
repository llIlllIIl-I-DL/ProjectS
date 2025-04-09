using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHP : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHP = 10;
    [SerializeField] private float currentHP;
    
    private PlayerStateManager playerStateManager;

    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;

    private void Awake()
    {
        currentHP = maxHP;
        playerStateManager = GetComponent<PlayerStateManager>();
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        Debug.Log($"Player took {damage} damage. Current HP: {currentHP}");
        
        // 플레이어가 데미지를 입으면 Hit 상태로 변경
        if (playerStateManager != null)
        {
            playerStateManager.ChangeState(PlayerStateType.Hit);
        }
        
        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player died.");
    }
}
