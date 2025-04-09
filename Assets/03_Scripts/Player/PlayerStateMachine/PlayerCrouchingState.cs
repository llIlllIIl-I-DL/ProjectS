using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCrouchingState : PlayerStateBase
{
    private float crouchingMoveSpeed = 3.5f; // 기본값
    private float originalColliderHeight;
    private float crouchingColliderHeight;
    private float crouchingOffsetY = -0.34f; // 기본값
    private CapsuleCollider2D playerCollider;
    private BoxCollider2D boxCollider; // 플레이어가 BoxCollider2D를 사용하는 경우 대비

    public PlayerCrouchingState(PlayerStateManager stateManager) : base(stateManager)
    {
        // 플레이어 설정에서 값을 가져옵니다
        var settings = stateManager.GetSettings();
        if (settings != null)
        {
            Debug.Log("PlayerSettings 발견");
            crouchingMoveSpeed = settings.crouchSpeed;
            crouchingOffsetY = settings.crouchOffsetY;
        }
        else
        {
            Debug.LogWarning("PlayerSettings를 찾을 수 없습니다. 기본값을 사용합니다.");
        }
        
        Debug.Log("PlayerCrouchingState 생성자 실행");
    }

    private void InitializeCollider()
    {
        // CapsuleCollider2D 또는 BoxCollider2D 찾기
        playerCollider = player.GetComponent<CapsuleCollider2D>();
        
        if (playerCollider != null)
        {
            Debug.Log($"CapsuleCollider2D 찾음: {playerCollider.name}, 현재 크기: {playerCollider.size}");
            originalColliderHeight = playerCollider.size.y;
            crouchingColliderHeight = originalColliderHeight * 0.6f; // 기본값, 설정에서 가져올 수 있으면 변경
            
            // PlayerSettings의 crouchHeightRatio 값 사용 시도
            var settings = player.GetSettings();
            if (settings != null && settings.GetType().GetField("crouchHeightRatio") != null)
            {
                crouchingColliderHeight = originalColliderHeight * settings.crouchHeightRatio;
            }
            
            Debug.Log($"콜라이더 초기화 완료: 원래 높이={originalColliderHeight}, 앉을 때 높이={crouchingColliderHeight}, 오프셋={crouchingOffsetY}");
        }
        else
        {
            // BoxCollider2D 시도
            boxCollider = player.GetComponent<BoxCollider2D>();
            
            if (boxCollider != null)
            {
                Debug.Log($"BoxCollider2D 찾음: {boxCollider.name}, 현재 크기: {boxCollider.size}");
                originalColliderHeight = boxCollider.size.y;
                crouchingColliderHeight = originalColliderHeight * 0.6f; // 기본값
                
                // PlayerSettings의 crouchHeightRatio 값 사용 시도
                var settings = player.GetSettings();
                if (settings != null && settings.GetType().GetField("crouchHeightRatio") != null)
                {
                    crouchingColliderHeight = originalColliderHeight * settings.crouchHeightRatio;
                }
                
                Debug.Log($"콜라이더 초기화 완료: 원래 높이={originalColliderHeight}, 앉을 때 높이={crouchingColliderHeight}, 오프셋={crouchingOffsetY}");
            }
            else
            {
                Debug.LogError("플레이어에 CapsuleCollider2D 또는 BoxCollider2D가 없습니다!");
            }
        }
    }

    public override void Enter()
    {
        Debug.Log("앉기 상태 Enter() 메서드 시작");
        
        // 콜라이더 초기화 (Enter 시에 매번 초기화)
        InitializeCollider();
        
        // 상태 플래그 설정
        player.SetCrouching(true);
        
        // Collider 크기 조절
        AdjustColliderSize(true);
        
        Debug.Log("앉기 상태 시작: 콜라이더 크기 조절");
    }

    public override void Exit()
    {
        Debug.Log("앉기 상태 Exit() 메서드 시작");
        
        // 콜라이더가 초기화되지 않았으면 초기화
        if (playerCollider == null && boxCollider == null)
        {
            InitializeCollider();
        }
        
        // Collider 크기 복원
        AdjustColliderSize(false);
        
        // 상태 플래그 해제
        player.SetCrouching(false);
        
        Debug.Log("앉기 상태 종료: 콜라이더 크기 복원");
    }

    // 콜라이더 크기 조절 함수
    private void AdjustColliderSize(bool isCrouching)
    {
        if (playerCollider != null)
        {
            // 현재 콜라이더 상태 기록
            Debug.Log($"콜라이더 변경 전: Size={playerCollider.size}, Offset={playerCollider.offset}");
            
            if (isCrouching)
            {
                // 앉기 - 크기 줄임
                Vector2 newSize = playerCollider.size;
                newSize.y = crouchingColliderHeight;
                playerCollider.size = newSize;
                
                // 오프셋 조정
                Vector2 newOffset = playerCollider.offset;
                newOffset.y = crouchingOffsetY;
                playerCollider.offset = newOffset;
            }
            else
            {
                // 일어서기 - 원래 크기로 복원
                Vector2 originalSize = playerCollider.size;
                originalSize.y = originalColliderHeight;
                playerCollider.size = originalSize;
                
                // 오프셋 복원
                Vector2 originalOffset = playerCollider.offset;
                originalOffset.y = 0f;
                playerCollider.offset = originalOffset;
            }
            
            // 변경 후 상태 확인
            Debug.Log($"콜라이더 변경 후: Size={playerCollider.size}, Offset={playerCollider.offset}");
        }
        else if (boxCollider != null)
        {
            // BoxCollider2D 처리
            Debug.Log($"박스 콜라이더 변경 전: Size={boxCollider.size}, Offset={boxCollider.offset}");
            
            if (isCrouching)
            {
                // 앉기 - 크기 줄임
                Vector2 newSize = boxCollider.size;
                newSize.y = crouchingColliderHeight;
                boxCollider.size = newSize;
                
                // 오프셋 조정
                Vector2 newOffset = boxCollider.offset;
                newOffset.y = crouchingOffsetY;
                boxCollider.offset = newOffset;
            }
            else
            {
                // 일어서기 - 원래 크기로 복원
                Vector2 originalSize = boxCollider.size;
                originalSize.y = originalColliderHeight;
                boxCollider.size = originalSize;
                
                // 오프셋 복원
                Vector2 originalOffset = boxCollider.offset;
                originalOffset.y = 0f;
                boxCollider.offset = originalOffset;
            }
            
            // 변경 후 상태 확인
            Debug.Log($"박스 콜라이더 변경 후: Size={boxCollider.size}, Offset={boxCollider.offset}");
        }
        else
        {
            Debug.LogError("앉기 상태에서 콜라이더를 찾을 수 없습니다!");
        }
    }

    public override void HandleInput()
    {
        var inputHandler = player.GetInputHandler();
        
        // 앉은 상태에서 점프 입력 시
        if (inputHandler.JumpPressed)
        {
            // 앉기 상태를 종료하고, 일반적인 점프 로직으로 진행
            player.ExitCrouchState();
            return;
        }
        
        // 앉은 상태에서 아래 방향키를 떼면 일어섬
        if (!inputHandler.IsDownPressed)
        {
            player.ExitCrouchState();
        }
    }

    public override void Update()
    {
        // 디버그를 위해 매 프레임 콜라이더 상태 체크 (문제 해결 후 제거할 수 있음)
        if (playerCollider != null)
        {
            Debug.Log($"Update - 현재 콜라이더 크기: {playerCollider.size}, 오프셋: {playerCollider.offset}");
        }
        else if (boxCollider != null)
        {
            Debug.Log($"Update - 현재 박스 콜라이더 크기: {boxCollider.size}, 오프셋: {boxCollider.offset}");
        }
    }

    public override void FixedUpdate()
    {
        var inputHandler = player.GetInputHandler();
        var movement = player.GetMovement();
        
        // 앉은 상태에서 이동 (감소된 속도로)
        if (inputHandler.IsMoving())
        {
            // 좌우 이동만 가능하게 수정 (필요한 경우)
            Vector2 moveInput = inputHandler.MoveDirection;
            moveInput.y = 0; // y축 이동 제한
            
            // 앉기 상태에서의 이동 속도 적용
            movement.Move(moveInput * crouchingMoveSpeed);
        }
        else
        {
            // 정지
            //movement.Stop();
        }
    }
} 