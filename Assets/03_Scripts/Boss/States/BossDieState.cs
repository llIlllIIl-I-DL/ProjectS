using UnityEngine;

public class BossDieState : IEnemyState
{
    private readonly BossStateMachine stateMachine;
    private readonly Transform boss;
    private readonly Animator animator;
    private readonly Rigidbody2D rb;

    public BossDieState(BossStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        boss = stateMachine.transform;
        animator = stateMachine.GetComponent<Animator>();
        rb = stateMachine.GetComponent<Rigidbody2D>();
    }

    public void Enter()
    {
        Debug.Log("[BossDieState] Enter");

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }

        animator?.SetTrigger("setDie");
        animator.enabled = false;

        // Optional: 게임 오브젝트 비활성화
        // stateMachine.gameObject.SetActive(false);
    }

    public void Exit()
    {
        Debug.Log("[BossDieState] Exit");
    }

    public void Update() { }
    public void FixedUpdate() { }
    public void OnTriggerEnter2D(Collider2D other) { }
}
