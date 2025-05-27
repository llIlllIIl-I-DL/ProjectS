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
    Inactive,
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

    [Header("보스 초기 위치")]
    private Vector3 originalPosition;

    [Header("설정")]
    public float chaseRange = 5f;
    public bool isFastChasingAfterProjectile = false;

    //[Header("중력 관련")]
    //public float gravity = -20f;               // 중력 가속도
    //public float verticalSpeed = 0f;           // 현재 수직 속도
    //public float groundCheckDistance = 0.2f;   // 지면 감지 거리
    //public LayerMask groundLayer;              // 지면 레이어

    //private bool isGrounded;

    // 킥 공격 관련
    private float lastKickTime = -Mathf.Infinity;
    public float KickCooldown => GameConstants.Boss.KICK_COOLDOWN;
    public float LastKickTime => lastKickTime;
    public bool CanKick => Time.time - lastKickTime >= KickCooldown;

    private Animator animator;
    private bool isDead = false;
    private bool isActive = false;

    // 이벤트
    public System.Action OnBossBattle;
    public System.Action OnBossDefeated;

    private void Awake()
    {
        bossHealth = GetComponent<BossHealth>();
        animator = GetComponentInChildren<Animator>();
        // 사망 이벤트
        if (bossHealth != null)
        {
            bossHealth.OnBossDied += HandleBossDeath;
        }
    }

    private void Start()
    {
        playerTransform = GameObject.FindWithTag(GameConstants.Tags.PLAYER)?.transform;
        originalPosition = transform.position;

        // 상태 초기화
        InitializeStates();

        // 기본 상태로 시작
        ChangeState(BossState.Inactive);
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
        states.Add(BossState.Inactive, new BossInactiveState(this)); // 비활성화 상태 추가
    }

    public void ChangeState(BossState state)
    {
        // 이미 죽은 상태면 Die 상태 외의 상태 전이 차단
        if (isDead && state != BossState.Die) return;

        // 비활성화 상태이고, 활성화 상태로 전환하는 것이 아니라면 상태 변경 무시
        if (!isActive && state != BossState.Inactive && state != BossState.Die) return;

        // 존재하지 않는 상태나 이미 현재 상태인 경우 무시
        if (!states.ContainsKey(state)) return;
        if (currentState == states[state]) return;

        Debug.Log($"[BossStateMachine] 상태 전이: {(currentState != null ? currentState.GetType().Name : "none")} → {state}");

        // 상태 전환 처리
        currentState?.Exit();
        currentState = states[state];
        currentState?.Enter();
    }

    // 플레이어 감지 메소드 (BossRoomTrigger에서 호출)
    public void DetectPlayer(Transform player)
    {
        if (player == null || isDead) return;

        Debug.Log("[BossStateMachine] 플레이어 감지!");

        // 플레이어 참조 설정
        playerTransform = player;

        // 보스 활성화
        ActivateBoss();
    }

    // 보스 활성화 메소드
    public void ActivateBoss()
    {
        if (isActive || isDead) return;

        isActive = true;
        Debug.Log("[BossStateMachine] 보스 활성화!");

        // 이벤트 발생
        OnBossBattle?.Invoke();

        // 초기 상태 설정
        ChangeState(BossState.Idle);
    }

    // 보스 비활성화 메소드
    public void DeactivateBoss()
    {
        if (!isActive || isDead) return;

        isActive = false;
        Debug.Log("[BossStateMachine] 보스 비활성화!");

        // 비활성화 상태로 전환
        ChangeState(BossState.Inactive);
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

    public void ResetBoss()
    {
        Debug.Log("[BossStateMachine] 보스 리셋!");

        // 1. 비활성화 상태로 전환
        isActive = false;
        isDead = false;
        playerTransform = null;

        // 2. 체력 회복
        if (bossHealth != null)
        {
            bossHealth.ResetHealth(); // 아래에 정의 필요
        }

        // 3. 상태 초기화
        ChangeState(BossState.Inactive);

        // 4. 위치 초기화 (선택적)
        transform.position = originalPosition; // 초기 위치 저장 필요

        // 5. 애니메이터 초기화
        if (animator != null)
        {
            animator.Rebind(); // 애니메이션 초기화
            animator.Update(0f);
        }

        // 6. 쿨다운 초기화
        lastKickTime = -Mathf.Infinity;
    }
}