using UnityEngine;

public class BossHealth : MonoBehaviour, IDamageable
{
    [SerializeField] public float maxHP;
    [SerializeField] private float currentHP;
    
    public event System.Action OnBossDied;
    
    private Animator animator;
    
    private void Awake()
    {
        currentHP = maxHP;
        animator = GetComponent<Animator>();
        
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