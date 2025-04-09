using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClimbingState : PlayerStateBase
{
    private float climbSpeed = 4f;
    private bool canHorizontallyExit = false;
    private Vector2 originalGravity;
    private float minClimbSpeedThreshold = 0.1f;
    private bool isMoving = false;
    private PlayerAnimator playerAnimator;

    public PlayerClimbingState(PlayerStateManager stateManager) : base(stateManager)
    {
        var settings = stateManager.GetSettings();
        if (settings != null && settings.GetType().GetField("climbSpeed") != null)
        {
            climbSpeed = settings.climbSpeed;
        }

        playerAnimator = stateManager.GetComponent<PlayerAnimator>();
    }

    public override void Enter()
    {
        // 중력 저장 및 비활성화
        var rigidbody = player.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            originalGravity = new Vector2(rigidbody.gravityScale, Physics2D.gravity.y);
            rigidbody.gravityScale = 0f;
            rigidbody.velocity = Vector2.zero;
        }

        // 애니메이터 참조 확인
        if (playerAnimator == null)
        {
            playerAnimator = player.GetComponent<PlayerAnimator>();
        }

        // 사다리 오르기 상태 플래그 설정
        player.SetClimbing(true);

        // 초기에는 움직이지 않는 상태로 시작 - 한 프레임에서 멈춰있음
        isMoving = false;
        if (playerAnimator != null)
        {
            playerAnimator.SetClimbing(true);
            playerAnimator.SetActuallyClimbing(false);
        }
    }

    public override void Exit()
    {
        // 중력 복원
        var rigidbody = player.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            rigidbody.gravityScale = originalGravity.x;
        }

        // 사다리 오르기 상태 플래그 해제
        player.SetClimbing(false);

        // 움직임 상태 초기화
        isMoving = false;
        if (playerAnimator != null)
        {
            playerAnimator.SetClimbing(false);
            playerAnimator.SetActuallyClimbing(false);
        }
    }

    public override void HandleInput()
    {
        var inputHandler = player.GetInputHandler();
        var collisionDetector = player.GetCollisionDetector();

        // 점프 입력 시 사다리에서 점프하여 내리기
        if (inputHandler.JumpPressed)
        {
            player.ExitClimbingState(true);
            return;
        }

        // 좌우 이동 + 사다리 영역을 벗어났을 때 사다리에서 내리기
        if (canHorizontallyExit && !Mathf.Approximately(inputHandler.MoveDirection.x, 0f) &&
            !collisionDetector.IsOnLadder)
        {
            player.ExitClimbingState(false);
            return;
        }

        // 움직임 여부 확인 (y축 값이 임계값보다 큰지)
        bool previouslyMoving = isMoving;
        isMoving = Mathf.Abs(inputHandler.MoveDirection.y) > minClimbSpeedThreshold;

        // 움직임 상태에 변화가 있을 때만 애니메이션 업데이트
        if (previouslyMoving != isMoving)
        {
            UpdateClimbingAnimation(isMoving);
        }
    }

    public override void Update()
    {
        var collisionDetector = player.GetCollisionDetector();
        var inputHandler = player.GetInputHandler();

        // 사다리에서 벗어났는지 확인
        if (!collisionDetector.IsOnLadder)
        {
            player.ExitClimbingState(false);
            return;
        }

        // 땅에 닿았는지 확인 (사다리 아래에 도달)
        if (collisionDetector.IsGrounded && inputHandler.MoveDirection.y <= 0)
        {
            player.ExitClimbingState(false);
            return;
        }

        // 사다리 위에 도달했는지 확인 (사다리 위로 올라갔을 때)
        if (collisionDetector.IsAtTopOfLadder && inputHandler.MoveDirection.y > 0)
        {
            player.ExitClimbingState(false);
            return;
        }

        // 현재 움직임 상태 확인
        bool currentlyMoving = Mathf.Abs(inputHandler.MoveDirection.y) > minClimbSpeedThreshold;

        // 움직임 상태 변경 시에만 애니메이션 상태 업데이트
        if (currentlyMoving != isMoving)
        {
            UpdateClimbingAnimation(currentlyMoving);
            isMoving = currentlyMoving;
        }
    }

    private void UpdateClimbingAnimation(bool isCurrentlyMoving)
    {
        isMoving = isCurrentlyMoving;

        if (playerAnimator != null)
        {
            // 실제로 사다리를 오르는 중인지 설정 (움직일 때만 애니메이션 재생)
            playerAnimator.SetActuallyClimbing(isMoving);
        }
        else
        {
            // 애니메이터 참조 다시 설정 시도
            playerAnimator = player.GetComponent<PlayerAnimator>();
            if (playerAnimator != null)
            {
                playerAnimator.SetActuallyClimbing(isMoving);
            }
        }
    }

    public override void FixedUpdate()
    {
        var inputHandler = player.GetInputHandler();
        var movement = player.GetMovement();

        // 입력 확인 및 이동 속도 계산
        Vector2 moveInput = inputHandler.MoveDirection;

        // 사다리 오르내리기 - 수직 이동 처리
        Vector2 climbVelocity = new Vector2(0f, moveInput.y * climbSpeed);

        // 좌우 이동 - 제한적으로 허용 (록맨 스타일은 좌우 이동이 거의 없음)
        float horizontalMovement = moveInput.x * (climbSpeed * 0.1f);
        climbVelocity.x = horizontalMovement;

        // 사다리에서의 이동 적용
        movement.ClimbMove(climbVelocity);
    }
}