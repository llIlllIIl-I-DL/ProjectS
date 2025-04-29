using UnityEngine;

public class BossMoveState : IEnemyState
{
    private BossStateMachine BossStateMachine;
    private Transform player;
    private Transform boss;
    private Rigidbody2D rb;
    private Animator animator;

    private float moveSpeed = 3f;
    private float detectionRange = 10f;
    private float attackRange = 5f;

    public BossMoveState(BossStateMachine stateMachine)
    {
        BossStateMachine = stateMachine;
        boss = stateMachine.transform;
        player = stateMachine.playerTransform;
        rb = stateMachine.GetComponent<Rigidbody2D>();
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter()// 상태에 진입했을 때
    {
        Debug.Log("Boss Move 상태 진입###");
        animator.SetBool("IsMoving", true);
    }
    public void Exit()// 상태에서 나갈 때
    {
        Debug.Log("Boss Move 상태 종료###");
        animator.SetBool("IsMoving", false);
    }
    public void Update()// 매 프레임 업데이트
    {
        if (player == null || boss == null) return;

        float distance = Vector2.Distance(boss.position, player.position);

        if(distance >= detectionRange)
        {   //감지 거리보다 플레이어가 멀리 있을 경우 Move 상태 유지
            return;
        }
        else if (distance >= attackRange){
            //감지 거리보다는 가깝고 근접 공격 거리보다는 멀 경우
            Debug.Log("원거리 공격###");
            BossStateMachine.ChangeState(BossState.ProjectileAttack);
        }
        else //근접 공격 거리 안에 들어와있을 경우
        {
            Debug.Log("근거리 공격###");
            BossStateMachine.ChangeState(BossState.SlashAttack);
        }


    }
    public void FixedUpdate()// 물리 업데이트
    {
        if (player == null || boss == null || rb == null) return;

        float distance = Vector2.Distance(player.position, boss.position);

        if (distance <= attackRange)
        {
            rb.velocity = Vector2.zero;
            animator?.SetFloat("MoveSpeed", 0f);
            return;
        }

        Vector2 direction = (player.position - boss.position).normalized;
        rb.velocity = direction * moveSpeed;

        // 방향 전환
        if (direction.x != 0)
        {
            boss.localScale = new Vector3(Mathf.Sign(direction.x), 1f, 1f);
        }

        animator?.SetFloat("MoveSpeed", rb.velocity.magnitude);
    }
    public void OnTriggerEnter2D(Collider2D other)// 트리거 충돌 감지
    {
        if (other.CompareTag("Player"))
        {
            if (BossStateMachine.CanKick)
            {
                Debug.Log("Kick 공격 진입###");
                BossStateMachine.ChangeState(BossState.KickAttack);
            }
            else
            {
                float remain = BossStateMachine.KickCooldown - (Time.time - BossStateMachine.LastKickTime);
                Debug.Log($"Kick 쿨다운 중... 남은 시간: {remain:F1}초");
            }
        }
    }
}