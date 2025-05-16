using UnityEngine;

public class PlayerHP : MonoBehaviour, IDebuffable
{
    [SerializeField] private float maxHP = 10;
    [SerializeField] private float currentHP;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float sprintMultiplier;
    [SerializeField] private float jumpForce;
    [SerializeField] private float defence = 0f;

    private readonly float MIN_HP = 0f;
    private readonly float MAX_HP = 100f;

    public float CurrentHP
    {
        get => currentHP;
        set => currentHP = Mathf.Clamp(value, MIN_HP, maxHP);
    }

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    public float SprintMultiplier
    {
        get => sprintMultiplier;
        set => sprintMultiplier = value;
    }

    public float JumpForce
    {
        get => jumpForce;
        set => jumpForce = value;
    }

    public float Defence
    {
        get => defence;
        set => defence = value;
    }

    public float MaxHP
    {
        get => maxHP;
        set => maxHP = value;
    }

    private void Awake()
    {
        // PlayerSettings에서 moveSpeed, sprintMultiplier, jumpForce 초기값 받아오기
        var player = GetComponent<Player>();
        if (player != null)
        {
            var settingsField = player.GetType().GetField("settings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (settingsField != null)
            {
                var settings = settingsField.GetValue(player) as PlayerSettings;
                if (settings != null)
                {
                    moveSpeed = settings.moveSpeed;
                    sprintMultiplier = settings.sprintMultiplier;
                    jumpForce = settings.jumpForce;
                }
            }
        }
    }

    public void TakeDamage(float amount)
    {
        currentHP = Mathf.Clamp(currentHP - amount, MIN_HP, maxHP);
        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, MIN_HP, maxHP);
    }

    public void IncreaseMaxHP(float amount)
    {
        if (amount <= 0) return;
        maxHP += amount;
        currentHP = Mathf.Clamp(currentHP, MIN_HP, maxHP);
    }

    public void DecreaseMaxHP(float amount)
    {
        if (amount <= 0) return;
        maxHP = Mathf.Max(MIN_HP, maxHP - amount);
        currentHP = Mathf.Clamp(currentHP, MIN_HP, maxHP);
    }

    public void ResetHealth()
    {
        currentHP = maxHP;
    }

    private void Die()
    {
        // 사망 처리(이벤트, 상태 전환 등은 PlayerStateManager 등에서 처리)
        Debug.Log("플레이어가 사망했습니다.");
    }
}
