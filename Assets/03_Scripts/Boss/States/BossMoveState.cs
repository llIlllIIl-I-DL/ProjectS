using UnityEngine;

public class BossMoveState : IEnemyState
{
    private readonly BossStateMachine stateMachine;
    private readonly Transform playerTransform;
    private readonly Transform bossTransform;
    private readonly Rigidbody2D rb;
    private readonly Animator animator;

    private float moveSpeed = GameConstants.Boss.NORMAL_MOVE_SPEED;
    private bool isDead;

    public BossMoveState(BossStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        bossTransform = stateMachine.transform;
        playerTransform = stateMachine.playerTransform;
        rb = stateMachine.GetComponent<Rigidbody2D>();
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter()
    {
        if (stateMachine.isFastChasingAfterProjectile)
            Debug.Log("빠른 추적 모드 시작");

        Debug.Log("Boss Move 상태 진입");
        animator.SetBool(GameConstants.AnimParams.IS_MOVING, true);
    }

    public void Exit()
    {
        Debug.Log("Boss Move 상태 종료");
        animator.SetBool(GameConstants.AnimParams.IS_MOVING, false);
        
        // 상태 종료 시 빠른 추적 모드 비활성화
        stateMachine.isFastChasingAfterProjectile = false;
    }

    public void Update()
    {
        if (playerTransform == null || bossTransform == null) return;

        // 보스 방향 설정 (플레이어를 향해 바라봄)
        UpdateBossDirection();
        
        // 이동 속도 설정
        UpdateMoveSpeed();
    }

    private void UpdateBossDirection()
    {
        if (playerTransform != null)
        {
            float directionX = playerTransform.position.x - bossTransform.position.x;
            if (directionX != 0)
                bossTransform.localScale = new Vector3(Mathf.Sign(directionX), 1f, 1f);
        }
    }
    
    private void UpdateMoveSpeed()
    {
        moveSpeed = stateMachine.isFastChasingAfterProjectile 
            ? GameConstants.Boss.FAST_MOVE_SPEED
            : GameConstants.Boss.NORMAL_MOVE_SPEED;
    }

    public void FixedUpdate()
    {
        if (playerTransform == null || bossTransform == null || rb == null) return;

        float distance = Vector2.Distance(playerTransform.position, bossTransform.position);

        // 공격 범위 내에 들어오면 공격으로 전환
        if (distance <= GameConstants.Boss.ATTACK_RANGE)
        {
            rb.velocity = Vector2.zero;
            animator?.SetFloat(GameConstants.AnimParams.MOVE_SPEED, 0f);

            stateMachine.ChangeState(BossState.SlashAttack);
            return;
        }

        // 플레이어 방향으로 이동
        Vector2 direction = (playerTransform.position - bossTransform.position).normalized;
        rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y); // x축으로만 이동

        animator?.SetFloat(GameConstants.AnimParams.MOVE_SPEED, Mathf.Abs(rb.velocity.x));
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(GameConstants.Tags.PLAYER))
        {
            if (stateMachine.CanKick)
            {
                Debug.Log("Kick 공격 진입");
                stateMachine.ChangeState(BossState.KickAttack);
            }
            else
            {
                float remainingTime = stateMachine.KickCooldown - (Time.time - stateMachine.LastKickTime);
                Debug.Log($"Kick 쿨다운 중... 남은 시간: {remainingTime:F1}초");
            }
        }
    }
}