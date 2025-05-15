using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHitMovementState : PlayerMovementStateBase
{
    private float hitStartTime;
    private float hitDuration = 0.5f;         // 피격 상태 지속 시간
    private float knockbackForce = 1f;        // 넉백 힘
    private float invincibilityDuration = 1.5f; // 무적 시간
    private Vector2 knockbackDirection;        // 넉백 방향

    public PlayerHitMovementState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        // 상태 초기화
        stateMachine.SetJumping(false);
        stateMachine.SetWallSliding(false);
        
        hitStartTime = Time.time;
        
        // 피격 애니메이션 재생 (애니메이션 파라미터가 있다고 가정)
        var playerAnimator = stateMachine.gameObject.GetComponent<PlayerAnimator>();
        if (playerAnimator != null)
        {
            // IsHit 애니메이션 파라미터 설정
            playerAnimator.SetTrigger("Hit");
        }
        
        // 넉백 방향 설정 (플레이어가 바라보는 반대 방향으로)
        var movement = stateMachine.GetMovement();
        knockbackDirection = new Vector2(-movement.FacingDirection, 0.5f).normalized;
        
        // 넉백 적용
        ApplyKnockback();
        
        // 무적 시간 설정 (코루틴 실행은 MonoBehaviour에서만 가능하므로 PlayerStateManager에 요청)
        stateMachine.StartCoroutine(InvincibilityRoutine());
        
        // 디버그 로그
        Debug.Log("피격 상태 진입: 넉백 적용");
    }

    public override void Update()
    {
        // 피격 상태 지속 시간 체크
        if (Time.time >= hitStartTime + hitDuration)
        {
            ReturnToNormalState();
        }
    }

    public override void FixedUpdate()
    {
        // 피격 중에는 사용자 입력을 무시하고 넉백만 적용
    }

    public override void HandleInput()
    {
        // 피격 중에는 입력 처리하지 않음
    }

    public override void Exit()
    {
        // 상태 종료 시 정리
        var playerAnimator = stateMachine.gameObject.GetComponent<PlayerAnimator>();
        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger("Hit");
        }
    }

    private void ApplyKnockback()
    {
        // Rigidbody2D에 즉시 힘 적용
        var rb = stateMachine.gameObject.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // 현재 속도를 0으로 리셋 후 넉백 적용
            rb.velocity = Vector2.zero;
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        }
    }

    private void ReturnToNormalState()
    {
        // 상태에 따라 적절한 상태로 전환
        var collisionDetector = stateMachine.GetCollisionDetector();
        
        if (!collisionDetector.IsGrounded)
        {
            stateMachine.ChangeState(MovementStateType.Falling);
        }
        else
        {
            stateMachine.ChangeState(MovementStateType.Idle);
        }
    }

    private IEnumerator InvincibilityRoutine()
    {
        // 무적 상태 설정
        // 실제 구현에서는 플레이어 캐릭터에 무적 플래그를 설정하고
        // 스프라이트를 깜빡이게 하는 등의 시각적 피드백을 제공
        
        // 무적 시간 대기
        yield return new WaitForSeconds(invincibilityDuration);
        
        // 무적 상태 해제
        Debug.Log("무적 시간 종료");
    }
}
