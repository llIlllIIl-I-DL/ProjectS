using UnityEngine;
namespace BossFSM
{

public class Boss : MonoBehaviour
{
    public Animator Animator { get; private set; }
    private BossStateMachine stateMachine;
    public int maxHealth = 100;
    private int currentHealth;

    private void Awake()
    {
        Animator = GetComponent<Animator>();
        stateMachine = GetComponent<BossStateMachine>();
        currentHealth = maxHealth;
    }

    private void Start()
    {
        // 초기 상태를 Idle로 설정
        stateMachine.ChangeState(new BossIdleState(stateMachine, this));
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        stateMachine.ChangeState(new BossDieState(stateMachine, this));
    }
} 
}