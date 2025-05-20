using UnityEngine;

public class BossMoveState : IEnemyState
{
    private BossStateMachine BossStateMachine;
    private Transform player;
    private Transform boss;
    private Rigidbody2D rb;
    private Animator animator;
    private BossHealth bossHealth;

    //private float moveSpeed = 3f;
    private float detectionRange = 10f;
    private float attackRange = 5f;
    private bool isDead;

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
        if (BossStateMachine.isFastChasingAfterProjectile)
            Debug.Log("빠른 추적 모드 시작###");

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

        //float distance = Vector2.Distance(boss.position, player.position);

        //if (distance >= detectionRange)
        //{   // 감지 거리보다 플레이어가 멀리 있을 경우 Move 상태 유지
        //    return;
        //}
        //else if (distance >= attackRange)
        //{
        //    // 감지 거리보다는 가깝고 근접 공격 거리보다는 멀 경우
        //    Debug.Log("원거리 공격###");
        //    BossStateMachine.ChangeState(BossState.ProjectileAttack);
        //}
        //else // 근접 공격 거리 안에 들어와 있을 경우
        //{
        //    Debug.Log("근거리 공격###");
        //    BossStateMachine.ChangeState(BossState.SlashAttack);
        //}

        // 방향만 계산
        if (player != null)
        {
            float directionX = player.position.x - boss.position.x;
            if (directionX != 0)
                boss.localScale = new Vector3(Mathf.Sign(directionX), 1f, 1f);
        }

        // [수정] 빠른 추적 모드일 경우 이동 속도 증가
        if (BossStateMachine.isFastChasingAfterProjectile)
        {
            bossHealth.MoveSpeed = 3f; // 빠른 속도로 이동
        }
        else
        {
            bossHealth.MoveSpeed = 1f; // 기본 속도
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

            BossStateMachine.ChangeState(BossState.SlashAttack); // 상태 전이 추가
            return;
        }

        Vector2 direction = (player.position - boss.position).normalized;
        rb.velocity = new Vector2(direction.x * bossHealth.MoveSpeed, rb.velocity.y); // x축으로만 이동

        animator?.SetFloat("MoveSpeed", Mathf.Abs(rb.velocity.x));
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
    public void SetDead()
    {
        isDead = true;
        Debug.Log("[BossStateMachine] 보스 사망 처리됨 - 상태 업데이트 정지");
    }
}