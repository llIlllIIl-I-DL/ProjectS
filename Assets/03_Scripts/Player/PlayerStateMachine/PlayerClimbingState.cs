using System.Collections.Generic;
using UnityEngine;

public class PlayerClimbingState : PlayerStateBase
{
    // 클래스 변수 추가
    private float climbSpeed = 4f;
    private bool canHorizontallyExit = false;
    private Vector2 originalGravity;
    private float minClimbSpeedThreshold = 0.1f;
    private bool isMoving = false;
    private PlayerAnimator playerAnimator;

    // 사다리 관련 플랫폼 콜라이더 관리
    private List<Collider2D> connectedPlatformColliders = new List<Collider2D>();
    private bool platformCollidersDisabled = false;

    // 사다리 중앙 고정 관련 변수
    private Collider2D currentLadderCollider;
    private float ladderCenterX;
    private float playerHorizontalMovementRange = 0.5f; // 사다리에서 좌우로 움직일 수 있는 최대 범위

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
        Debug.Log("Entering Climbing State");

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

        // 사다리에 연결된 플랫폼 콜라이더 찾아서 비활성화
        FindAndDisablePlatformColliders();
    }

    public override void Exit()
    {
        Debug.Log("Exiting Climbing State");

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

        // 비활성화된 플랫폼 콜라이더 다시 활성화
        RestorePlatformColliders();
    }

    // 사다리에 연결된 플랫폼 콜라이더 찾기 및 비활성화
    private void FindAndDisablePlatformColliders()
    {
        // 1. 현재 플레이어가 닿아있는 사다리 찾기
        var collisionDetector = player.GetCollisionDetector();
        if (!collisionDetector.IsOnLadder)
        {
            Debug.LogWarning("Player is not on a ladder!");
            return;
        }

        // 2. 현재 닿아있는 사다리 트리거 찾기 (Ladder 레이어)
        int ladderLayer = LayerMask.NameToLayer("Ladder");
        if (ladderLayer == -1)
        {
            Debug.LogError("Ladder layer not found!");
            return;
        }

        // 3. 플레이어와 겹치는 모든 콜라이더 찾기
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
        {
            Debug.LogError("Player collider not found!");
            return;
        }

        // 사다리 콜라이더 찾기
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(1 << ladderLayer);
        filter.useTriggers = true;

        List<Collider2D> overlappingColliders = new List<Collider2D>();
        Physics2D.OverlapCollider(playerCollider, filter, overlappingColliders);

        if (overlappingColliders.Count == 0)
        {
            Debug.LogWarning("No ladder colliders found!");
            return;
        }

        // 플레이어를 사다리 중앙에 위치시키기
        Collider2D ladderCollider = overlappingColliders[0]; // 첫 번째 사다리 콜라이더 사용
        CenterPlayerOnLadder(ladderCollider);

        // 4. 각 사다리에 대해 연결된 플랫폼 콜라이더 찾기
        foreach (var ladderCol in overlappingColliders)
        {
            Transform ladderTransform = ladderCol.transform;

            // 4.1. 같은 게임 오브젝트에 있는 다른 콜라이더 확인 (플랫폼일 수 있음)
            Collider2D[] attachedColliders = ladderTransform.GetComponents<Collider2D>();
            foreach (var coll in attachedColliders)
            {
                // 트리거가 아닌 콜라이더는 플랫폼으로 간주
                if (!coll.isTrigger && coll.enabled)
                {
                    connectedPlatformColliders.Add(coll);
                }
            }

            // 4.2. 부모/자식 관계의 플랫폼 콜라이더 찾기
            // 부모에서 찾기
            if (ladderTransform.parent != null)
            {
                Collider2D[] parentColliders = ladderTransform.parent.GetComponentsInChildren<Collider2D>();
                foreach (var coll in parentColliders)
                {
                    // 트리거가 아니고, 이미 리스트에 없으며, 사다리 레이어가 아닌 경우
                    if (!coll.isTrigger && !connectedPlatformColliders.Contains(coll) &&
                        coll.gameObject.layer != ladderLayer && coll.enabled)
                    {
                        connectedPlatformColliders.Add(coll);
                    }
                }
            }

            // 4.3. 사다리 위치와 겹치거나 가까운 다른 플랫폼 콜라이더 찾기 (선택적)
            int platformLayer = LayerMask.NameToLayer("Platform");
            if (platformLayer != -1)
            {
                Collider2D[] nearbyPlatforms = Physics2D.OverlapBoxAll(
                    ladderCol.bounds.center,
                    ladderCol.bounds.size * 1.2f, // 약간 더 넓은 영역 검색
                    0f,
                    1 << platformLayer
                );

                foreach (var platform in nearbyPlatforms)
                {
                    if (!connectedPlatformColliders.Contains(platform) && platform.enabled)
                    {
                        connectedPlatformColliders.Add(platform);
                    }
                }
            }
        }

        // 5. 연결된 플랫폼 콜라이더 비활성화
        Debug.Log($"Found {connectedPlatformColliders.Count} platform colliders to disable");
        foreach (var platformCollider in connectedPlatformColliders)
        {
            if (platformCollider != null && platformCollider.enabled)
            {
                platformCollider.enabled = false;
                Debug.Log($"Disabled platform collider: {platformCollider.gameObject.name}");
            }
        }

        platformCollidersDisabled = true;
    }

    // 플랫폼 콜라이더 복원
    private void RestorePlatformColliders()
    {
        if (!platformCollidersDisabled) return;

        foreach (var platformCollider in connectedPlatformColliders)
        {
            if (platformCollider != null)
            {
                platformCollider.enabled = true;
                Debug.Log($"Restored platform collider: {platformCollider.gameObject.name}");
            }
        }

        connectedPlatformColliders.Clear();
        platformCollidersDisabled = false;
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

        // 좌우 이동으로 사다리 범위를 벗어났는지 확인
        if (currentLadderCollider != null && IsPlayerOutOfLadderRange())
        {
            Debug.Log("Player moved out of ladder horizontal range");
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

        // 좌우 이동 - 제한적으로 허용 (사다리 중앙을 기준으로 제한된 범위 내에서만)
        float horizontalMovement = moveInput.x * (climbSpeed * 0.1f);
        climbVelocity.x = horizontalMovement;

        // 사다리 중앙으로 서서히 당기는 힘 추가 (부드러운 중앙 정렬)
        if (currentLadderCollider != null)
        {
            float currentX = player.transform.position.x;
            float pullForce = (ladderCenterX - currentX) * 2.0f; // 가중치 계수 (높을수록 더 강하게 당김)

            // 최대 당김 힘 제한 (너무 갑작스럽게 움직이지 않도록)
            pullForce = Mathf.Clamp(pullForce, -1.0f, 1.0f);

            // 입력에 의한 이동이 있으면 중앙 정렬력을 줄임
            if (Mathf.Abs(moveInput.x) > 0.1f)
            {
                pullForce *= 0.3f; // 입력 시 중앙 정렬력 감소
            }

            // 최종 수평 속도에 중앙 정렬력 추가
            climbVelocity.x += pullForce;
        }

        // 사다리에서의 이동 적용
        movement.ClimbMove(climbVelocity);
    }

    // 플레이어가 사다리 범위를 벗어났는지 확인하는 메서드
    private bool IsPlayerOutOfLadderRange()
    {
        if (currentLadderCollider == null) return false;

        // 현재 플레이어 위치 가져오기
        float playerX = player.transform.position.x;

        // 사다리 너비의 절반 (안전 마진 포함)
        float ladderHalfWidth = currentLadderCollider.bounds.size.x / 2f;

        // 좌우 이동 최대 허용 범위 계산
        float minX = ladderCenterX - ladderHalfWidth - playerHorizontalMovementRange;
        float maxX = ladderCenterX + ladderHalfWidth + playerHorizontalMovementRange;

        // 플레이어가 허용 범위를 벗어났는지 확인
        bool isOutOfRange = playerX < minX || playerX > maxX;

        if (isOutOfRange)
        {
            Debug.Log($"Player out of ladder range: playerX={playerX}, range=[{minX}, {maxX}]");
        }

        return isOutOfRange;
    }    // 플레이어를 사다리 중앙에 위치시키는 메서드
    private void CenterPlayerOnLadder(Collider2D ladderCollider)
    {
        if (ladderCollider == null) return;

        // 현재 사다리 콜라이더 저장
        currentLadderCollider = ladderCollider;

        // 사다리의 중앙 X 좌표 계산
        ladderCenterX = ladderCollider.bounds.center.x;

        // 플레이어의 위치를 사다리 중앙 X 좌표로 즉시 설정
        var playerTransform = player.transform;
        Vector3 newPosition = playerTransform.position;
        newPosition.x = ladderCenterX;

        // 플레이어 위치 설정
        playerTransform.position = newPosition;

        Debug.Log($"Centered player at ladder X position: {ladderCenterX}");
    }
}