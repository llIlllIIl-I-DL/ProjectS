using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClimbingState : PlayerStateBase
{
    private float climbSpeed = 4f; // 기본 사다리 오르는 속도
    private bool canHorizontallyExit = false; // 좌우 이동으로 사다리에서 내릴 수 있는지
    private Vector2 originalGravity; // 원래 중력값 저장용
    private float minClimbSpeedThreshold = 0.1f; // 애니메이션 재생을 위한 최소 속도
    private bool isMoving = false; // 실제 움직임 여부
    private PlayerAnimator playerAnimator;
    private float animationUpdateInterval = 0.1f; // 애니메이션 업데이트 간격 (초당 10회)
    private float lastAnimationUpdateTime = 0f;
    private bool wasMovingLastFrame = false; // 마지막 프레임에서의 움직임 상태
    
    public PlayerClimbingState(PlayerStateManager stateManager) : base(stateManager)
    {
        // 플레이어 설정에서 값을 가져옵니다
        var settings = stateManager.GetSettings();
        if (settings != null && settings.GetType().GetField("climbSpeed") != null)
        {
            climbSpeed = settings.climbSpeed;
        }
        
        playerAnimator = stateManager.GetComponent<PlayerAnimator>();
        
        Debug.Log("PlayerClimbingState 생성자 실행");
    }

    public override void Enter()
    {
        Debug.Log("사다리 오르기 상태 시작");
        
        // 중력 저장 및 비활성화
        var rigidbody = player.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            originalGravity = new Vector2(rigidbody.gravityScale, Physics2D.gravity.y);
            rigidbody.gravityScale = 0f; // 중력 비활성화
            rigidbody.velocity = Vector2.zero; // 속도 초기화
        }
        
        // 사다리 오르기 상태 플래그 설정
        player.SetClimbing(true);
        
        // 초기에는 움직이지 않는 상태로 시작
        isMoving = false;
        wasMovingLastFrame = false;
        if (playerAnimator != null)
        {
            playerAnimator.SetActuallyClimbing(false);
        }
        
        // 애니메이션 업데이트 시간 초기화
        lastAnimationUpdateTime = Time.time;
    }

    public override void Exit()
    {
        Debug.Log("사다리 오르기 상태 종료");
        
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
        wasMovingLastFrame = false;
        if (playerAnimator != null)
        {
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
            // 점프로 사다리에서 내리기
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
        
        // 움직임 여부 확인
        isMoving = Mathf.Abs(inputHandler.MoveDirection.y) > minClimbSpeedThreshold;
        
        // 움직임 상태가 변경된 경우에만 애니메이션 상태 업데이트
        if (wasMovingLastFrame != isMoving)
        {
            UpdateClimbingAnimation(isMoving);
            wasMovingLastFrame = isMoving;
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

        // 일정 간격으로 애니메이션 상태 확인 및 업데이트
        if (Time.time - lastAnimationUpdateTime >= animationUpdateInterval)
        {
            bool currentlyMoving = Mathf.Abs(inputHandler.MoveDirection.y) > minClimbSpeedThreshold;
            
            // 움직임 상태 변경 시에만 애니메이션 상태 업데이트
            if (currentlyMoving != wasMovingLastFrame)
            {
                UpdateClimbingAnimation(currentlyMoving);
                wasMovingLastFrame = currentlyMoving;
            }
            
            lastAnimationUpdateTime = Time.time;
        }
    }

    // 사다리 애니메이션 상태 업데이트 메서드
    private void UpdateClimbingAnimation(bool isCurrentlyMoving)
    {
        isMoving = isCurrentlyMoving;
        
        if (playerAnimator != null)
        {
            playerAnimator.SetActuallyClimbing(isMoving);
            Debug.Log($"사다리 애니메이션 상태 업데이트: {isMoving}");
        }
    }

    public override void FixedUpdate()
    {
        var inputHandler = player.GetInputHandler();
        var movement = player.GetMovement();
        
        // 사다리 오르내리기 - 수직 이동 처리
        Vector2 climbVelocity = new Vector2(0f, inputHandler.MoveDirection.y * climbSpeed);
        
        // 좌우 이동 - 제한적으로 허용 (필요에 따라 조정)
        float horizontalMovement = inputHandler.MoveDirection.x * (climbSpeed * 0.5f);
        climbVelocity.x = horizontalMovement;
        
        // 사다리에서의 이동 적용
        movement.ClimbMove(climbVelocity);
    }
} 