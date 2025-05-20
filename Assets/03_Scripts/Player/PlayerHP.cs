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

    public event System.Action<float, float> OnHPChanged;
    public event System.Action<float> OnDamaged;
    public event System.Action OnDied;

    public float CurrentHP
    {
        get => currentHP;
        set
        {
            currentHP = Mathf.Clamp(value, MIN_HP, maxHP);
            OnHPChanged?.Invoke(maxHP, currentHP);
        }
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
        if (currentHP <= 0) return;

        float prevHP = currentHP;
        CurrentHP = currentHP - amount;

        if (CurrentHP < prevHP)
            OnDamaged?.Invoke(amount);

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        CurrentHP = currentHP + amount;
    }

    public void IncreaseMaxHP(float amount)
    {
        if (amount <= 0) return;
        maxHP += amount;
        currentHP = Mathf.Clamp(currentHP, MIN_HP, maxHP);
        OnHPChanged?.Invoke(maxHP, currentHP);
    }

    public void DecreaseMaxHP(float amount)
    {
        if (amount <= 0) return;
        maxHP = Mathf.Max(MIN_HP, maxHP - amount);
        currentHP = Mathf.Clamp(currentHP, MIN_HP, maxHP);
        OnHPChanged?.Invoke(maxHP, currentHP);
    }

    public void ResetHealth()
    {
        currentHP = maxHP;
        OnHPChanged?.Invoke(maxHP, currentHP);
    }

    private void Die()
    {
        Debug.Log("플레이어가 사망했습니다.");
        OnDied?.Invoke();
    }
}
