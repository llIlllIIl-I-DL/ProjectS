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

    public Transform playerTransform;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public GameObject slashEffectPrefab;
    public GameObject kickEffectPrefab;
    public float chaseRange = 5f;

    [SerializeField] public int maxHP = 100;
    [SerializeField] private int currentHP;

    public float KickCooldown = 20f;
    private float lastKickTime = -Mathf.Infinity;

    public float LastKickTime => lastKickTime;

    public bool CanKick => Time.time - lastKickTime >= KickCooldown;

    public void Start()
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

    public void Awake()
    {
        currentHP = maxHP;
    }

    public void ChangeState(BossState state)
    {
        if (!states.ContainsKey(state))
        {
            Debug.LogError($"State {state} not found in StateMachine.");
            return;
        }

        if (currentState == states[state]) return;

        currentState?.Exit();
        currentState = states[state];
        currentState?.Enter();
    }


    public void Update() => currentState?.Update();
    public void FixedUpdate() => currentState?.FixedUpdate();

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 10f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 5f);
    }

    public void MarkKickUsed()
    {
        lastKickTime = Time.time;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        currentState?.OnTriggerEnter2D(other);
    }
}