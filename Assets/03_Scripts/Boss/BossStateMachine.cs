using System.Collections.Generic;
using UnityEngine;

public enum BossState
{
    Idle,
    Move,
    ProjectileAttack,
    SlashAttack,
    KickAttack,
    Die,
}

public class BossStateMachine : MonoBehaviour
{
    public IEnemyState currentState;
    private Dictionary<BossState, IEnemyState> states = new Dictionary<BossState, IEnemyState>();

    public BossHealth bossHealth; // 체력 컴포넌트 참조

    public Transform playerTransform;
    private Animator animator;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float chaseRange = 5f;

    public float KickCooldown = 20f;
    private float lastKickTime = -Mathf.Infinity;

    public float LastKickTime => lastKickTime;
    public bool CanKick => Time.time - lastKickTime >= KickCooldown;

    private bool isDead = false;

    private void Awake()
    {
        bossHealth = GetComponent<BossHealth>();
        animator = GetComponentInChildren<Animator>();
        bossHealth.OnBossDied += HandleBossDeath;
    }

    private void Start()
    {
        playerTransform = GameObject.FindWithTag("Player")?.transform;

        states.Add(BossState.Idle, new BossIdleState(this));
        states.Add(BossState.Move, new BossMoveState(this));
        states.Add(BossState.ProjectileAttack, new BossProjectileAttackState(this));
        states.Add(BossState.SlashAttack, new BossSlashAttackState(this));
        states.Add(BossState.KickAttack, new BossKickAttackState(this));
        states.Add(BossState.Die, new BossDieState(this));

        ChangeState(BossState.Idle);
    }

    public void ChangeState(BossState state)
    {
        if (isDead) return; // 사망 시 상태 전이 차단
        if (!states.ContainsKey(state)) return;
        if (currentState == states[state]) return;

        currentState?.Exit();
        currentState = states[state];
        currentState?.Enter();
    }

    private void HandleBossDeath()
    {
        if (isDead) return;

        isDead = true;
        Die();
    }

    public void Die()
    {
        animator.Play("Boss_Die");
        ChangeState(BossState.Die);
    }

    private void Update()
    {
        currentState?.Update();
    }

    private void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }

    public void MarkKickUsed()
    {
        lastKickTime = Time.time;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 10f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 5f);
    }
    public void OnDeathAnimationEnd()
    {
        animator.enabled = false;
        // 기타 처리: 피격 무시, 오브젝트 제거 등
    }

}
