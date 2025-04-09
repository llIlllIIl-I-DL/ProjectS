using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

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
        
        // 애니메이터 참조 설정
        playerAnimator = stateManager.GetComponent<PlayerAnimator>();
        if (playerAnimator == null)
        {
            Debug.LogError("PlayerAnimator 컴포넌트를 찾을 수 없습니다!");
        }
        
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
            Debug.Log($"중력 비활성화: 원래 값={originalGravity.x}");
        }
        else
        {
            Debug.LogError("Rigidbody2D를 찾을 수 없습니다!");
        }
        
        // 애니메이터 참조 확인
        if (playerAnimator == null)
        {
            playerAnimator = player.GetComponent<PlayerAnimator>();
            if (playerAnimator == null)
            {
                Debug.LogError("PlayerAnimator 컴포넌트를 찾을 수 없습니다! 애니메이션이 작동하지 않을 수 있습니다.");
            }
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
        else
        {
            Debug.LogError("Enter에서 playerAnimator가 null입니다!");
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
            Debug.Log($"중력 복원: 값={originalGravity.x}");
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
        else
        {
            Debug.LogError("Exit에서 playerAnimator가 null입니다!");
        }
    }

    public override void HandleInput()
    {
        var inputHandler = player.GetInputHandler();
        var collisionDetector = player.GetCollisionDetector();
        
        // 점프 입력 시 사다리에서 점프하여 내리기
        if (inputHandler.JumpPressed)
        {
            Debug.Log("사다리에서 점프로 내리기");
            // 점프로 사다리에서 내리기
            player.ExitClimbingState(true);
            return;
        }
        
        // 좌우 이동 + 사다리 영역을 벗어났을 때 사다리에서 내리기
        if (canHorizontallyExit && !Mathf.Approximately(inputHandler.MoveDirection.x, 0f) && 
            !collisionDetector.IsOnLadder)
        {
            Debug.Log("사다리 영역을 벗어나 내리기");
            player.ExitClimbingState(false);
            return;
        }
        
        // 움직임 여부 확인 (y축 값이 임계값보다 큰지)
        bool previouslyMoving = isMoving;
        isMoving = Mathf.Abs(inputHandler.MoveDirection.y) > minClimbSpeedThreshold;
        
        // 디버깅 로그 추가
        if (Debug.isDebugBuild && Time.frameCount % 30 == 0)
        {
            Debug.Log($"사다리 입력 처리: Y입력={inputHandler.MoveDirection.y}, 임계값={minClimbSpeedThreshold}, 움직임={isMoving}");
        }
        
        // 움직임 상태에 변화가 있을 때만 로그 출력
        if (previouslyMoving != isMoving)
        {
            Debug.Log($"사다리 움직임 상태 변경: {isMoving}, Y 입력: {inputHandler.MoveDirection.y}, 임계값: {minClimbSpeedThreshold}");
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
            Debug.Log("사다리에서 벗어남 - 사다리 상태 종료");
            player.ExitClimbingState(false);
            return;
        }
        
        // 땅에 닿았는지 확인 (사다리 아래에 도달)
        if (collisionDetector.IsGrounded && inputHandler.MoveDirection.y <= 0)
        {
            Debug.Log("사다리 아래 지면에 도달 - 사다리 상태 종료");
            player.ExitClimbingState(false);
            return;
        }
        
        // 사다리 위에 도달했는지 확인 (사다리 위로 올라갔을 때)
        if (collisionDetector.IsAtTopOfLadder && inputHandler.MoveDirection.y > 0)
        {
            Debug.Log("사다리 상단에 도달 - 사다리 상태 종료");
            player.ExitClimbingState(false);
            return;
        }

        // 일정 간격으로 애니메이션 상태 확인 및 업데이트
        if (Time.time - lastAnimationUpdateTime >= animationUpdateInterval)
        {
            // 현재 움직임 상태 확인
            bool currentlyMoving = Mathf.Abs(inputHandler.MoveDirection.y) > minClimbSpeedThreshold;
            
            // 디버그: 매 간격마다 입력값 출력
            if (Debug.isDebugBuild && Time.frameCount % 60 == 0)
            {
                Debug.Log($"사다리 상태 업데이트: 움직임={currentlyMoving}, Y 입력={inputHandler.MoveDirection.y}, 임계값={minClimbSpeedThreshold}");
            }
            
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
            Debug.Log($"사다리 애니메이션 상태 업데이트 호출: {isMoving}");
            playerAnimator.SetActuallyClimbing(isMoving);

            
        }
        else
        {
            Debug.LogError("UpdateClimbingAnimation에서 playerAnimator가 null입니다! 애니메이터 참조를 다시 가져오려고 시도합니다.");
            
            // 애니메이터 참조 다시 설정 시도
            playerAnimator = player.GetComponent<PlayerAnimator>();
            if (playerAnimator != null)
            {
                playerAnimator.SetActuallyClimbing(isMoving);
                Debug.Log("애니메이터 참조를 다시 가져왔습니다.");
            }
        }
    }
    
    // 애니메이션 상태 확인을 위한 코루틴
    private IEnumerator CheckAnimationStateAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        
        Animator animator = player.GetComponent<Animator>();
        if (animator != null)
        {
            // 현재 애니메이터의 IsActuallyClimbing 파라미터 상태 확인
            AnimatorControllerParameter[] parameters = animator.parameters;
            foreach (var param in parameters)
            {
                if (param.name == "IsActuallyClimbing")
                {
                    bool actualValue = animator.GetBool(param.name);
                    Debug.Log($"애니메이션 상태 확인: IsActuallyClimbing={actualValue}, 설정한 값={isMoving}");
                    break;
                }
            }
        }
    }

    public override void FixedUpdate()
    {
        var inputHandler = player.GetInputHandler();
        var movement = player.GetMovement();
        
        // 입력 확인
        Vector2 moveInput = inputHandler.MoveDirection;
        
        // 사다리 오르내리기 - 수직 이동 처리
        Vector2 climbVelocity = new Vector2(0f, moveInput.y * climbSpeed);
        
        // 좌우 이동 - 제한적으로 허용 (필요에 따라 조정)
        float horizontalMovement = moveInput.x * (climbSpeed * 0.5f);
        climbVelocity.x = horizontalMovement;
        
        // 이동 디버깅 (20프레임마다 로그)
        if (Debug.isDebugBuild && Time.frameCount % 20 == 0)
        {
            Debug.Log($"사다리 이동: velocity={climbVelocity}, input={moveInput}");
        }
        
        // 사다리에서의 이동 적용
        movement.ClimbMove(climbVelocity);
    }
} 