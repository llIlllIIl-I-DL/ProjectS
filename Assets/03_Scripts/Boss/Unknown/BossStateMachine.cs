using System.Collections.Generic;
using UnityEngine;

public enum BossState
{
    Idle,
    Move,
    ProjectileAttack,
    SlashAttack,
    KickAttack,
    Die
}

public class BossStateMachine : MonoBehaviour
{
    public IEnemyState currentState;
    private Dictionary<BossState, IEnemyState> states = new Dictionary<BossState, IEnemyState>();

    [Header("참조")]
    public BossHealth bossHealth;
    public Transform playerTransform;
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("설정")]
    public float chaseRange = 5f;
    public bool isFastChasingAfterProjectile = false;

    // 킥 공격 관련
    private float lastKickTime = -Mathf.Infinity;
    public float KickCooldown => GameConstants.Boss.KICK_COOLDOWN;
    public float LastKickTime => lastKickTime;
    public bool CanKick => Time.time - lastKickTime >= KickCooldown;

    private Animator animator;
    private bool isDead = false;

    private void Awake()
    {
        bossHealth = GetComponent<BossHealth>();
        animator = GetComponentInChildren<Animator>();
        bossHealth.OnBossDied += HandleBossDeath;
    }

    private void Start()
    {
        playerTransform = GameObject.FindWithTag(GameConstants.Tags.PLAYER)?.transform;

        // 상태 초기화
        InitializeStates();

        // 기본 상태로 시작
        ChangeState(BossState.Idle);
    }

    private void InitializeStates()
    {
        // 상태 추가
        states.Add(BossState.Idle, new BossIdleState(this));
        states.Add(BossState.Move, new BossMoveState(this));
        states.Add(BossState.ProjectileAttack, new BossProjectileAttackState(this));
        states.Add(BossState.SlashAttack, new BossSlashAttackState(this));
        states.Add(BossState.KickAttack, new BossKickAttackState(this));
        states.Add(BossState.Die, new BossDieState(this));
    }

    public void ChangeState(BossState state)
    {
        // 이미 죽은 상태면 Die 상태 외의 상태 전이 차단
        if (isDead && state != BossState.Die) return;

        // 존재하지 않는 상태나 이미 현재 상태인 경우 무시
        if (!states.ContainsKey(state)) return;
        if (currentState == states[state]) return;

        Debug.Log($"[BossStateMachine] 상태 전이: {currentState} → {state}");

        // 상태 전환 처리
        currentState?.Exit();
        currentState = states[state];
        currentState?.Enter();
    }

    private void HandleBossDeath()
    {
        if (isDead) return;

        isDead = true;
        SetDead();
    }

    public void SetDead()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("[BossStateMachine] 보스가 사망했습니다.");

        // 상태 전환
        currentState?.Exit();
        ChangeState(BossState.Die);

        // 사망 애니메이션 트리거
        animator?.SetTrigger(GameConstants.AnimParams.DEAD);
    }

    private void Update()
    {
        if (isDead) return;
        currentState?.Update();
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        currentState?.FixedUpdate();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, GameConstants.Boss.DETECTION_RANGE);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, GameConstants.Boss.ATTACK_RANGE);
    }

    // 애니메이션 이벤트에서 호출되는 메서드들
    public void OnDeathAnimationEnd()
    {
        animator.enabled = false;
    }

    public void OnKickAnimationEnd()
    {
        if (currentState is BossKickAttackState)
        {
            ChangeState(BossState.Idle);
        }
    }

    // 킥 공격 사용 시 쿨다운 갱신
    public void UpdateKickCooldown()
    {
        lastKickTime = Time.time;
    }
}