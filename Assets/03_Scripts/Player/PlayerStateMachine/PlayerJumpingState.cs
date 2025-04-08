using UnityEngine;

public class PlayerJumpingState : PlayerStateBase
{
    private float jumpStartTime;

    public PlayerJumpingState(PlayerController playerController) : base(playerController) { }

    public override void Enter()
    {
        jumpStartTime = Time.time;
        
        // 안전장치: 중력 확인 및 보정
        if (player.Rb.gravityScale <= 0)
        {
            Debug.LogWarning("Gravity was 0 or negative when entering JumpingState. Fixing to 1");
            player.Rb.gravityScale = 1f;
        }
        
        // 대시에서 전환된 경우 추가 안전 처리
        bool isAfterDash = Time.time - player.LastDashTime < 0.3f;
        if (isAfterDash)
        {
            // 이미 처리된 경우에도 한 번 더 속도 제한 (이중 안전장치)
            player.Rb.velocity = new Vector2(
                Mathf.Clamp(player.Rb.velocity.x, -player.MoveSpeed, player.MoveSpeed), 
                player.Rb.velocity.y
            );
            
            // 대시 직후 진입 시 중력 재확인
            if (player.Rb.gravityScale <= 0)
            {
                Debug.LogWarning("Gravity still 0 after dash! Emergency fix applied");
                player.Rb.gravityScale = 1f;
            }
        }
        
        // 점프 상태로 전환될 때 한 번만 점프 실행
        // 이미 점프 중이면 중복 실행하지 않음
        if (!player.IsJumping && player.Rb.velocity.y < 0.1f)
        {
            player.Jump();
            Debug.Log("Executing jump from JumpingState.Enter()");
        }
        else
        {
            Debug.Log("Skip jump execution in JumpingState, velocity.y: " + player.Rb.velocity.y);
        }

        player.IsJumping = true; // 상태 명확하게 설정
        player.UpdateAnimations(null);
    }

    public override void Exit()
    {
        player.IsJumping = false;
    }

    public override void HandleInput()
    {
        // 대시 처리
        if (player.DashPressed && player.CanDash)
        {
            player.DashPressed = false;
            player.ChangeState(PlayerStateType.Dashing);
            return;
        }

        // 점프 버튼 뗌 처리 (점프 높이 조절)
        if (player.JumpReleased && player.Rb.velocity.y > 0 && player.IsJumping)
        {
            player.Rb.velocity = new Vector2(player.Rb.velocity.x, player.Rb.velocity.y * player.JumpCutMultiplier);
            player.IsJumping = false;
            player.JumpReleased = false;
        }

        // 벽에 닿았는지 확인
        if (player.IsTouchingWall() && player.IsMovingInFacingDirection())
        {
            player.ChangeState(PlayerStateType.WallSliding);
            return;
        }
    }

    public override void Update()
    {
        // 상승 속도가 0 이하가 되면 낙하 상태로 전환
        if (player.Rb.velocity.y <= 0)
        {
            player.ChangeState(PlayerStateType.Falling);
            return;
        }

        // 벽에 닿았는지 검사
        if (player.IsTouchingWall() && player.IsMovingInFacingDirection())
        {
            player.ChangeState(PlayerStateType.WallSliding);
            return;
        }

        // 애니메이션 업데이트
        player.UpdateAnimations(null);
    }

    public override void FixedUpdate()
    {
        // 점프 중에도 수평 이동 가능
        player.Move();
    }
}