using System.Collections;
using UnityEngine;

public class BossSlashAttackState : IEnemyState
{
    private readonly BossStateMachine stateMachine;
    private readonly Transform bossTransform;
    private readonly Transform playerTransform;
    private readonly Animator animator;

    private bool isAttackFinished = false;

    public BossSlashAttackState(BossStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        bossTransform = stateMachine.transform;
        playerTransform = stateMachine.playerTransform;
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter()
    {
        Debug.Log("Boss Slash 상태 진입");
        animator.SetBool(GameConstants.AnimParams.IS_SLASHING, true);
        isAttackFinished = false;
    }
    
    public void Exit()
    {
        Debug.Log("Boss Slash 상태 종료");
        animator.SetBool(GameConstants.AnimParams.IS_SLASHING, false);
    }

    public void Update()
    {
        if (playerTransform == null || bossTransform == null) return;

        float distance = Vector3.Distance(playerTransform.position, bossTransform.position);

        if (distance < GameConstants.Boss.ATTACK_RANGE && !isAttackFinished)
        {
            Debug.Log("근거리 공격");
            isAttackFinished = true;
            stateMachine.StartCoroutine(SlashAttackCoroutine());
        }
        else if (distance > GameConstants.Boss.ATTACK_RANGE && distance < GameConstants.Boss.DETECTION_RANGE)
        {
            stateMachine.ChangeState(BossState.ProjectileAttack);
        }
        else if (distance >= GameConstants.Boss.DETECTION_RANGE)
        {
            stateMachine.ChangeState(BossState.Idle);
        }
    }

    public void FixedUpdate() { }
    
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(GameConstants.Tags.PLAYER))
        {
            if (stateMachine.CanKick)
            {
                Debug.Log("Kick 공격 진입!");
                stateMachine.ChangeState(BossState.KickAttack);
            }
            else
            {
                float remainingTime = stateMachine.KickCooldown - (Time.time - stateMachine.LastKickTime);
                Debug.Log($"Kick 쿨다운 중... 남은 시간: {remainingTime:F1}초");
            }
        }
    }

    private IEnumerator SlashAttackCoroutine()
    {
        yield return new WaitForSeconds(GameConstants.Boss.ATTACK_DELAY);

        if (playerTransform != null && 
            Vector2.Distance(bossTransform.position, playerTransform.position) <= GameConstants.Boss.ATTACK_RANGE)
        {
            var damageable = playerTransform.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(GameConstants.Boss.SLASH_DAMAGE);
                Debug.Log($"Slash 데미지: {GameConstants.Boss.SLASH_DAMAGE}");
            }
        }

        yield return new WaitForSeconds(GameConstants.Boss.RETURN_TO_IDLE_DELAY);
        
        stateMachine.ChangeState(BossState.Idle);
    }
}