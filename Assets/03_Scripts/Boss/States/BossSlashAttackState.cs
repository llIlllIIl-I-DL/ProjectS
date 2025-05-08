using System.Collections;
using UnityEngine;

public class BossSlashAttackState : IEnemyState
{
    private BossStateMachine BossStateMachine;
    private Transform bossTransform;
    private Transform playerTransform;
    private Animator animator;

    [SerializeField] private float detectionRange = 10f; //플레이어를 감지하는 거리
    [SerializeField] private float attackRange = 5f; //근접 공격 거리

    private int slashDamage = 20;
    private float attackDelay = 0.5f;
    private float returnToIdleDelay = 1.0f;

    private bool isAttackFinished = false;

    public BossSlashAttackState(BossStateMachine stateMachine)
    {
        BossStateMachine = stateMachine;
        bossTransform = stateMachine.transform;
        playerTransform = stateMachine.playerTransform;
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter()// 상태에 진입했을 때
    {
        Debug.Log("Boss Slash 상태 진입###");
        animator.SetBool("IsSlashing", true);
    }
    public void Exit()// 상태에서 나갈 때
    {
        Debug.Log("Boss Slash 상태 종료###");
        animator.SetBool("IsSlashing", false);
    }

    public void Update()// 매 프레임 업데이트
    {
        if (playerTransform == null || bossTransform == null) return;

        float distance = Vector3.Distance(playerTransform.position, bossTransform.position); // 계속해서 거리 확인 하기.

        Debug.Log($"{distance},{attackRange}");

        if( distance< attackRange)
        {
            Debug.Log("근거리 공격");
            SlashAttackCoroutine();
        }
        else if( distance > attackRange && distance < detectionRange)
        {
            Debug.Log("원거리 공격###");
            BossStateMachine.ChangeState(BossState.ProjectileAttack);
        }
        else
        {
            Debug.Log("Idle 상태로###");
            BossStateMachine.ChangeState(BossState.Idle);
        }


    }

    public void FixedUpdate()// 물리 업데이트
    {

    }
    public void OnTriggerEnter2D(Collider2D other)// 트리거 충돌 감지
    {
        if (other.CompareTag("Player"))
        {
            if (BossStateMachine.CanKick)
            {
                Debug.Log("Kick 공격 진입!###");
                BossStateMachine.ChangeState(BossState.KickAttack);
            }
            else
            {
                float remain = BossStateMachine.KickCooldown - (Time.time - BossStateMachine.LastKickTime);
                Debug.Log($"Kick 쿨다운 중... 남은 시간: {remain:F1}초###");
            }
        }
    }

    private IEnumerator SlashAttackCoroutine() //근접 공격 반복 코루틴
    {
        yield return new WaitForSeconds(attackDelay);

        if (playerTransform != null && Vector2.Distance(bossTransform.position, playerTransform.position) <= attackRange)
        {
            var damageable = playerTransform.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(slashDamage);
                Debug.Log("Slash 데미지: " + slashDamage + "@@@@");
            }
        }

        yield return new WaitForSeconds(returnToIdleDelay);

        isAttackFinished = true;
    }

}