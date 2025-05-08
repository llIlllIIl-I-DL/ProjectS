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
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float chaseRange = 5f;

    public float KickCooldown = 20f;
    private float lastKickTime = -Mathf.Infinity;

    public float LastKickTime => lastKickTime;
    public bool CanKick => Time.time - lastKickTime >= KickCooldown;

    private void Awake()
    {
        bossHealth = GetComponent<BossHealth>();
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
        if (!states.ContainsKey(state)) return;
        if (currentState == states[state]) return;

        currentState?.Exit();
        currentState = states[state];
        currentState?.Enter();
    }

    private void HandleBossDeath()
    {
        if (!(currentState is BossDieState))
        {
            ChangeState(BossState.Die);
        }
    }

    private void Update()
    {
        currentState?.Update();
    }

    private void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        currentState?.OnTriggerEnter2D(other);

        if (other.CompareTag("PlayerAttack"))
        {
            int damage = 20; // 또는 other.GetComponent<Attack>().damage;
            bossHealth.TakeDamage(damage);
        }
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
}
