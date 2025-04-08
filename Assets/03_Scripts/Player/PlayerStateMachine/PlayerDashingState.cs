using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDashingState : PlayerStateBase
{
    private float originalGravity; // 원래 중력 값 저장
    private Coroutine dashCoroutine; // 대시 코루틴 참조 저장

    public PlayerDashingState(PlayerController playerController) : base(playerController) { }

    public override void Enter()
    {
        // 원래 중력 값 저장
        originalGravity = player.Rb.gravityScale;
        Debug.Log($"DashingState.Enter: Saving original gravity: {originalGravity}");
        
        // 중력 비활성화 (코루틴에서도 하지만 확실히 하기 위해 여기서도 설정)
        player.Rb.gravityScale = 0;
        
        // 애니메이션 업데이트
        player.UpdateAnimations(null);
        
        // 대시 코루틴 시작 및 참조 저장
        dashCoroutine = player.StartCoroutine(DashSequence());
    }

    public override void Exit()
    {
        // 대시 종료 시 처리
        Debug.Log($"DashingState.Exit: Restoring gravity from 0 to {originalGravity}");
        
        // 대시 코루틴만 중지
        if (dashCoroutine != null)
        {
            player.StopCoroutine(dashCoroutine);
            dashCoroutine = null;
            
            // 쿨다운은 더 이상 여기서 처리하지 않음 (PlayerController에서 관리)
        }
        
        // 중력 복원
        player.Rb.gravityScale = originalGravity;
        
        // 안전장치: 중력이 이상한 값이면 1로 설정
        if (player.Rb.gravityScale <= 0 || player.Rb.gravityScale > 10)
        {
            Debug.LogWarning("DashingState.Exit: Gravity value was abnormal! Setting to 1");
            player.Rb.gravityScale = 1f;
        }
        
        // isDashing 플래그 확실히 해제
        player.IsDashing = false;
    }

    public override void HandleInput()
    {
        // 대시 중에는 입력 처리 없음 (대시 코루틴에서 처리)
    }

    public override void Update()
    {
        // 대시 중 상태 점검
        // 혹시라도 중력이 변경되었다면 다시 0으로 설정
        if (player.Rb.gravityScale != 0 && player.IsDashing)
        {
            Debug.LogWarning("DashingState.Update: Gravity changed during dash! Resetting to 0");
            player.Rb.gravityScale = 0;
        }
    }

    public override void FixedUpdate()
    {
        // 대시 중에는 추가적인 물리 처리 없음
    }
    
    // 대시 시퀀스 코루틴 - 상태 내부에서 관리
    private IEnumerator DashSequence()
    {
        // canDash는 PlayerController.StartDash()에서 관리
        // 여기서는 isDashing 플래그만 관리
        player.IsDashing = true;
        Debug.Log("DashSequence: 대시 시작");
        
        // 대시 방향 결정 (입력이 없으면 현재 바라보는 방향)
        float dashDirection = (player.GetMovementVector().x != 0) ? 
            Mathf.Sign(player.GetMovementVector().x) : player.FacingDirection;

        // 대시 속도 설정
        player.Rb.velocity = new Vector2(dashDirection * player.DashSpeed, 0);

        // 대시 지속 시간 동안 대기
        yield return new WaitForSeconds(player.DashDuration);
        
        Debug.Log("DashSequence: 대시 지속 시간 종료");

        // 대시 종료 후 점프 입력 처리
        bool shouldJumpAfterDash = player.JumpPressed && player.LastJumpTime > 0;
        
        // 대시 완료 처리
        player.IsDashing = false;
        
        // 대시 후 다음 상태로 자동 전환
        if (shouldJumpAfterDash && (player.IsGrounded() || player.LastGroundedTime > 0))
        {
            Debug.Log("DashSequence: 대시 후 점프로 전환");
            // 점프 입력 처리
            player.JumpPressed = false;
            player.LastJumpTime = 0;
            player.ChangeState(PlayerStateType.Jumping);
        }
        else if (player.IsGrounded())
        {
            Debug.Log("DashSequence: 대시 후 지상 상태로 전환");
            if (player.IsMoving())
            {
                if (player.IsSprinting)
                    player.ChangeState(PlayerStateType.Sprinting);
                else
                    player.ChangeState(PlayerStateType.Running);
            }
            else
                player.ChangeState(PlayerStateType.Idle);
        }
        else if (player.IsTouchingWall() && !player.IsGrounded())
        {
            Debug.Log("DashSequence: 대시 후 벽 슬라이딩으로 전환");
            player.ChangeState(PlayerStateType.WallSliding);
        }
        else
        {
            Debug.Log("DashSequence: 대시 후 낙하 상태로 전환");
            player.ChangeState(PlayerStateType.Falling);
        }
    }
    
    // 별도의 대시 쿨다운 코루틴 제거됨 - PlayerController.DashCooldownManager 사용
}
