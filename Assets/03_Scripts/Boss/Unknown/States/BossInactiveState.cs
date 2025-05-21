using UnityEngine;

// 보스가 비활성화 상태일 때의 상태 클래스
public class BossInactiveState : IEnemyState
{
    private readonly BossStateMachine stateMachine;
    private readonly Animator animator;

    public BossInactiveState(BossStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        animator = stateMachine.GetComponent<Animator>();
    }

    public void Enter()
    {
        Debug.Log("Boss Inactive 상태 진입");

        // 기본 애니메이션 설정 (Idle 또는 특별한 대기 애니메이션)
        if (animator != null)
        {
            animator.SetBool(GameConstants.AnimParams.IS_IDLE, true);
            // 다른 애니메이션 파라미터는 모두 비활성화
            animator.SetBool(GameConstants.AnimParams.IS_MOVING, false);
            animator.SetBool(GameConstants.AnimParams.IS_SLASHING, false);
            animator.SetBool(GameConstants.AnimParams.IS_KICKING, false);
        }

        // 움직임 정지 등 추가 설정
        Rigidbody2D rb = stateMachine.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }

    public void Exit()
    {
        Debug.Log("Boss Inactive 상태 종료");
        // 비활성화 상태에서 나갈 때 필요한 처리
    }

    public void Update()
    {

    }

    public void FixedUpdate()
    {

    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        // 비활성화 상태에서 플레이어와 충돌할 경우
        // 이 부분은 보스룸 트리거가 아닌 직접 접촉했을 때를 위한 백업 처리
        if (other.CompareTag(GameConstants.Tags.PLAYER))
        {
            Debug.Log("[BossInactiveState] 플레이어가 보스와 직접 접촉했습니다!");
            
            // 보스 활성화 및 플레이어 감지
            stateMachine.DetectPlayer(other.transform);
        }
    }
}