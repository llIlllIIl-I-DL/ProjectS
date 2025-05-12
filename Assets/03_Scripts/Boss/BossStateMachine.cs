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

    public bool isFastChasingAfterProjectile = false;

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

        // 상태 추가
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
        // 이미 죽은 상태면 상태 전이 차단
        if (isDead && state != BossState.Die) return;

        if (!states.ContainsKey(state)) return;
        if (currentState == states[state]) return;

        Debug.Log($"[BossStateMachine] 상태 전이: {currentState} → {state}!!!");

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
        if (isDead) return;
        currentState?.Update();
    }

    private void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 10f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 5f);
    }

    public void SetDead()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("[BossStateMachine] 보스가 사망했습니다.!!!!");

        currentState?.Exit(); // 현재 상태 종료
        ChangeState(BossState.Die); // 반드시 Die 상태로 전이

        // 사망 애니메이션 트리거
        animator?.SetTrigger("setDead"); // 또는 "setDie"
    }
    public void OnDeathAnimationEnd()
    {
        animator.enabled = false;
        // 기타 처리: 피격 무시, 오브젝트 제거 등
    }

    public void OnKickAnimationEnd()
    {
        if (currentState is BossKickAttackState)
        {
            ChangeState(BossState.Idle);
        }
    }
}
