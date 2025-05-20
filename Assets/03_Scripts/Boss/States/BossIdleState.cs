using UnityEngine;

public class BossIdleState : IEnemyState
{
    private readonly BossStateMachine stateMachine;
    private readonly Transform bossTransform;
    private readonly Transform playerTransform;
    private readonly Animator animator;

    public BossIdleState(BossStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        bossTransform = stateMachine.transform;
        playerTransform = stateMachine.playerTransform;
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter()
    {
        Debug.Log("Boss Idle 상태 진입");
        animator.SetBool(GameConstants.AnimParams.IS_IDLE, true);
    }

    public void Exit()
    {
        animator.SetBool(GameConstants.AnimParams.IS_IDLE, false);
    }

    public void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(playerTransform.position, bossTransform.position);
        
        // 거리에 따른 상태 전환
        if (distance >= GameConstants.Boss.DETECTION_RANGE)
        {
            stateMachine.ChangeState(BossState.Move);
        }
        else if (distance >= GameConstants.Boss.ATTACK_RANGE)
        {
            stateMachine.ChangeState(BossState.ProjectileAttack);
        }
        else
        {
            stateMachine.ChangeState(BossState.SlashAttack);
        }
    }

    public void FixedUpdate() { }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(GameConstants.Tags.PLAYER)) return;

        if (stateMachine.CanKick)
        {
            stateMachine.ChangeState(BossState.KickAttack);
        }
        else
        {
            float remainingTime = stateMachine.KickCooldown - (Time.time - stateMachine.LastKickTime);
            Debug.Log($"[Idle] Kick 쿨다운 중... 남은 시간: {remainingTime:F1}초");
        }
    }
}