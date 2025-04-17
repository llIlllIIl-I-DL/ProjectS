using System.Collections.Generic;
using UnityEngine;

public class PlayerClimbingState : PlayerStateBase
{
    // 클래스 변수 추가
    private float climbSpeed = 4f;
    private Vector2 originalGravity;
    private float minClimbSpeedThreshold = 0.1f;
    private bool isMoving = false;
    private PlayerAnimator playerAnimator;

    // 플레이어 레이어 관리 변수 추가
    private int originalPlayerLayer;
    private int ladderLayer;
    private int platformLayer;
    private int noCollisionLayer;

    // 사다리 중앙 고정 관련 변수
    private Collider2D currentLadderCollider;
    private float ladderCenterX;
    private bool forceCenterAlignment = true; // 사다리 중앙에 강제 정렬

    // 사다리 진입/진출 감지 관련 변수
    private bool isExitingLadder = false;
    private const float LADDER_EXIT_DELAY = 0.1f; // 사다리에서 나가는 딜레이
    
    // 아래에서 사다리 진입 관련 변수 추가
    private bool isEnteringFromBottom = false;
    private bool isStartingFromPlatform = false;
    private float entryDelayTimer = 0f;
    private const float PLATFORM_ENTRY_DELAY = 0.1f; // 플랫폼에서 내려가기 딜레이

    // 사다리 바닥 체크 관련 변수 추가
    private bool isAtLadderBottom = false;
    
    // 플랫폼 관련 변수 추가
    private List<GameObject> modifiedPlatforms = new List<GameObject>();

    public PlayerClimbingState(PlayerStateManager stateManager) : base(stateManager)
    {
        var settings = stateManager.GetSettings();
        if (settings != null && settings.GetType().GetField("ClimbSpeed") != null)
        {
            climbSpeed = settings.climbSpeed;
        }

        playerAnimator = stateManager.GetComponent<PlayerAnimator>();
        
        // 레이어 미리 가져오기
        noCollisionLayer = LayerMask.NameToLayer("NoCollision");
        if (noCollisionLayer == -1)
        {
            Debug.LogError("NoCollision 레이어를 찾을 수 없습니다!");
        }
        
        // 플랫폼 레이어 미리 가져오기
        platformLayer = LayerMask.NameToLayer("Ground");
        if (platformLayer == -1)
        {
            Debug.LogError("Platform 레이어를 찾을 수 없습니다! Default를 사용합니다.");
            platformLayer = 0; // Default 레이어
        }
        
        // Ladder 레이어도 미리 가져오기
        ladderLayer = LayerMask.NameToLayer("Ladder");
        if (ladderLayer == -1)
        {
            Debug.LogError("Ladder 레이어를 찾을 수 없습니다!");
        }
    }

    public override void Enter()
    {
        Debug.Log("사다리 오르기 상태 진입");

        // 진입 상태 확인 (아래로 내려가는지, 플랫폼에서 시작하는지)
        var inputHandler = player.GetInputHandler();
        var collisionDetector = player.GetCollisionDetector();
        
        // 아래키를 눌러 접근하는지 체크
        isEnteringFromBottom = inputHandler.MoveDirection.y < 0;
        
        // 플랫폼에서 시작하는지 체크
        isStartingFromPlatform = collisionDetector.IsGrounded;
        
        Debug.Log($"사다리 진입 상태 - 아래키 접근: {isEnteringFromBottom}, 플랫폼 시작: {isStartingFromPlatform}");

        // 중력 비활성화 및 설정 저장
        var rigidbody = player.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            originalGravity = new Vector2(rigidbody.gravityScale, Physics2D.gravity.y);
            
            // 아래키를 눌러 사다리 진입 시 (플랫폼에서)
            if (isStartingFromPlatform && isEnteringFromBottom)
            {
                // 약간의 초기 속도 부여 (아래로)
                rigidbody.gravityScale = 0.1f;
                rigidbody.velocity = new Vector2(0, -0.8f);
                entryDelayTimer = PLATFORM_ENTRY_DELAY;
                Debug.Log("플랫폼에서 아래로 사다리 진입 - 초기 속도 부여");
            }
            else
            {
                // 일반 사다리 진입 시 중력 완전 제거
                rigidbody.gravityScale = 0f;
                rigidbody.velocity = Vector2.zero;
            }
        }

        // 애니메이터 참조 확인
        if (playerAnimator == null)
        {
            playerAnimator = player.GetComponent<PlayerAnimator>();
        }

        // 사다리 오르기 상태 플래그 설정
        player.SetClimbing(true);

        // 초기 애니메이션 상태 설정 (멈춰있는 상태로 시작)
        isMoving = false;
        if (playerAnimator != null)
        {
            playerAnimator.SetClimbing(true);
            playerAnimator.SetActuallyClimbing(false);
        }

        // 사다리 찾기 및 플레이어 중앙 정렬
        FindLadderAndCenterPlayer();
        
        // 주변 플랫폼 레이어 변경
        ChangeNearbyPlatformsLayer();
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
        if (playerAnimator != null)
        {
            playerAnimator.SetClimbing(false);
            playerAnimator.SetActuallyClimbing(false);
        }
        
        // 변경한 플랫폼 레이어 복원
        RestoreAllPlatformsLayer();
    }

    // 플레이어 레이어 변경
    private void ChangePlayerToLadderLayer()
    {
        // 이미 레이어가 변경되었는지 확인
        if (player.gameObject.layer == noCollisionLayer)
        {
            Debug.Log("플레이어 레이어가 이미 NoCollision으로 설정되어 있습니다.");
            return;
        }
        
        // 현재 플레이어 레이어 저장
        originalPlayerLayer = player.gameObject.layer;

        // 레이어가 올바르게 설정되었는지 확인
        if (noCollisionLayer == -1)
        {
            Debug.LogError("NoCollision 레이어가 정의되지 않았습니다. 레이어 설정을 확인하세요.");
            return;
        }

        Debug.Log($"플레이어 레이어 변경 전: {player.gameObject.name}, 현재 레이어: {LayerMask.LayerToName(player.gameObject.layer)}");

        // 플레이어 레이어를 NoCollision으로 변경
        player.gameObject.layer = noCollisionLayer;

        // 즉시 레이어가 변경되었는지 확인 (디버깅 목적)
        Debug.Log($"플레이어 레이어 변경 후: {player.gameObject.name}, 변경된 레이어: {LayerMask.LayerToName(player.gameObject.layer)}");

        // 플레이어의 모든 자식 콜라이더도 레이어 변경 (필요한 경우)
        Collider2D[] childColliders = player.GetComponentsInChildren<Collider2D>();
        foreach (var collider in childColliders)
        {
            if (collider.gameObject != player.gameObject)
            {
                collider.gameObject.layer = noCollisionLayer;
                Debug.Log($"자식 콜라이더 레이어 변경: {collider.gameObject.name}, 레이어: {LayerMask.LayerToName(collider.gameObject.layer)}");
            }
        }

        Debug.Log($"플레이어 레이어를 NoCollision으로 변경 완료: {LayerMask.LayerToName(originalPlayerLayer)} -> {LayerMask.LayerToName(noCollisionLayer)}");
    }

    // 플레이어 레이어 복원
    private void RestorePlayerLayer()
    {
        player.gameObject.layer = originalPlayerLayer;

        // 플레이어의 모든 자식 콜라이더도 레이어 복원
        Collider2D[] childColliders = player.GetComponentsInChildren<Collider2D>();
        foreach (var collider in childColliders)
        {
            if (collider.gameObject != player.gameObject)
            {
                collider.gameObject.layer = originalPlayerLayer;
            }
        }

        Debug.Log($"플레이어 레이어 복원: {LayerMask.LayerToName(noCollisionLayer)} -> {LayerMask.LayerToName(originalPlayerLayer)}");
    }
    
    // 주변 플랫폼 레이어를 NoCollision으로 변경
    private void ChangeNearbyPlatformsLayer()
    {
        if (noCollisionLayer == -1) return;
        if (currentLadderCollider == null) return;
        
        // 현재 사다리의 위치 주변 영역 계산
        Bounds ladderBounds = currentLadderCollider.bounds;
        Vector2 ladderCenter = ladderBounds.center;
        
        // 사다리 위쪽까지의 거리를 계산 (위쪽으로 더 넓게 검색)
        float searchRadius = 3f;
        float extraHeight = 2f;
        
        // 위쪽 방향으로 더 넓은 검색 영역 생성
        Vector2 searchCenter = new Vector2(ladderCenter.x, ladderCenter.y + (ladderBounds.size.y / 2) + (extraHeight / 2));
        Vector2 searchSize = new Vector2(searchRadius, ladderBounds.size.y + extraHeight);
        
        // 해당 영역 내 모든 플랫폼 찾기
        Collider2D[] colliders = Physics2D.OverlapBoxAll(searchCenter, searchSize, 0f);
        
        // 변경 전에 이전 목록 초기화
        modifiedPlatforms.Clear();
        
        Debug.Log($"사다리 주변 검색 영역: 중심={searchCenter}, 크기={searchSize}, 찾은 콜라이더 수={colliders.Length}");
        
        foreach (Collider2D collider in colliders)
        {
            // 플랫폼 레이어이고 사다리가 아닌 오브젝트만 처리
            if ((collider.gameObject.layer == platformLayer || 
                collider.CompareTag("Platform") || 
                collider.CompareTag("Ground")) && 
                !collider.CompareTag("Ladder"))
            {
                // 원래 레이어 정보를 저장하기 위해 태그에 임시 저장
                if (!collider.gameObject.CompareTag("Ladder"))
                {
                    collider.gameObject.tag = "Ladder";
                    // 목록에 추가
                    modifiedPlatforms.Add(collider.gameObject);
                    
                    // 레이어 변경
                    int originalLayer = collider.gameObject.layer;
                    collider.gameObject.layer = noCollisionLayer;
                    
                    Debug.Log($"플랫폼 레이어 변경: {collider.gameObject.name}, {LayerMask.LayerToName(originalLayer)} -> {LayerMask.LayerToName(noCollisionLayer)}");
                }
            }
        }
        
        Debug.Log($"총 {modifiedPlatforms.Count}개 플랫폼 레이어 변경 완료");
    }
    
    // 모든 플랫폼 레이어 복원
    private void RestoreAllPlatformsLayer()
    {
        foreach (GameObject platform in modifiedPlatforms)
        {
            if (platform != null)
            {
                platform.layer = platformLayer;
                platform.tag = "Ground"; // 원래 태그로 복원 (이 부분은 프로젝트에 맞게 수정 필요)
                Debug.Log($"플랫폼 레이어 복원: {platform.name}, {LayerMask.LayerToName(noCollisionLayer)} -> {LayerMask.LayerToName(platformLayer)}");
            }
        }
        
        modifiedPlatforms.Clear();
        Debug.Log("모든 플랫폼 레이어 복원 완료");
    }

    // 현재 플레이어가 닿아있는 사다리 찾기 및 플레이어 중앙 정렬
    private void FindLadderAndCenterPlayer()
    {
        // 플레이어와 겹치는 모든 콜라이더 찾기
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
        {
            Debug.LogError("플레이어 콜라이더를 찾을 수 없습니다!");
            return;
        }

        // 사다리 콜라이더 찾기 (Ladder 태그를 가진 오브젝트)
        int ladderMask = LayerMask.GetMask("Ladder"); // Ladder 레이어로 마스크 생성
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(ladderMask);
        filter.useTriggers = true;

        List<Collider2D> overlappingColliders = new List<Collider2D>();
        int numColliders = Physics2D.OverlapCollider(playerCollider, filter, overlappingColliders);

        if (overlappingColliders.Count == 0)
        {
            Debug.LogWarning("사다리 콜라이더를 찾을 수 없습니다!");
            return;
        }

        // 플레이어를 사다리 중앙에 위치시키기
        Collider2D ladderCollider = overlappingColliders[0]; // 첫 번째 사다리 콜라이더 사용
        CenterPlayerOnLadder(ladderCollider);

        Debug.Log($"사다리 콜라이더 찾음: {ladderCollider.name}, 위치: {ladderCollider.bounds.center}");
    }

    public override void HandleInput()
    {
        var inputHandler = player.GetInputHandler();
        var collisionDetector = player.GetCollisionDetector();

        // 입력이 상하 방향키가 아닌 경우 사다리에서 내리기
        Vector2 input = inputHandler.MoveDirection;

        // 상하 방향키가 눌려있지 않고 좌우 방향키가 눌려있으면 즉시 사다리에서 내림
        if (Mathf.Abs(input.y) < 0.1f && Mathf.Abs(input.x) > 0.1f)
        {
            Debug.Log("좌우 입력 감지 - 사다리에서 내리기");
            isExitingLadder = true;
            player.ExitClimbingState(false);
            return;
        }

        // 점프 버튼이 눌리면 사다리에서 점프하며 내림
        if (inputHandler.JumpPressed)
        {
            Debug.Log("점프 입력 감지 - 사다리에서 점프하여 내리기");
            isExitingLadder = true;
            player.ExitClimbingState(true);
            return;
        }

        // 움직임 여부 확인 (y축 값이 임계값보다 큰지)
        bool previouslyMoving = isMoving;
        isMoving = Mathf.Abs(input.y) > minClimbSpeedThreshold;

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
        var rigidbody = player.GetComponent<Rigidbody2D>();
        
        // 사다리에서 벗어났는지 확인
        if (!collisionDetector.IsOnLadder && !isExitingLadder)
        {
            Debug.Log("사다리에서 벗어남 - 상태 종료");
            player.ExitClimbingState(false);
            return;
        }

        // 사다리의 바닥에 도달했는지 확인
        CheckIfAtLadderBottom();

        // 지면에 닿고 아래 방향으로 이동 중일 때 (사다리 바닥에 있을 때만 내리기)
        if (collisionDetector.IsGrounded && inputHandler.MoveDirection.y < -0.1f && isAtLadderBottom)
        {
            Debug.Log("사다리 바닥에 도달하고 아래로 이동 중 - 상태 종료");
            player.ExitClimbingState(false);
            return;
        }

        // 플랫폼에서 아래로 내려가기 처리
        if (isStartingFromPlatform && isEnteringFromBottom)
        {
            if (entryDelayTimer > 0)
            {
                entryDelayTimer -= Time.deltaTime;
                
                // 지속적으로 아래로 이동하기
                if (rigidbody != null)
                {
                    rigidbody.velocity = new Vector2(0, -1.0f);
                }
                
                // 중앙 정렬 유지
                if (forceCenterAlignment && currentLadderCollider != null)
                {
                    Vector3 pos = player.transform.position;
                    pos.x = ladderCenterX;
                    player.transform.position = pos;
                }
            }
            else
            {
                // 딜레이 종료 후 정상 사다리 모드로 전환
                isStartingFromPlatform = false;
                isEnteringFromBottom = false;
                if (rigidbody != null)
                {
                    rigidbody.gravityScale = 0f;
                }
                Debug.Log("플랫폼에서 사다리 진입 딜레이 종료");
            }
        }

        // 사다리 중앙 강제 정렬 (Update에서도 지속적으로 중앙 정렬)
        if (forceCenterAlignment && currentLadderCollider != null)
        {
            Vector3 pos = player.transform.position;
            pos.x = ladderCenterX;
            player.transform.position = pos;
        }

        // 움직임 상태 업데이트
        bool currentlyMoving = Mathf.Abs(inputHandler.MoveDirection.y) > minClimbSpeedThreshold;
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
        var rigidbody = player.GetComponent<Rigidbody2D>();
        
        // 플랫폼에서 아래로 내려가기 처리
        if (isStartingFromPlatform && isEnteringFromBottom && entryDelayTimer > 0)
        {
            // 아래로 내려가는 이동 벡터 생성
            Vector2 downVelocity = new Vector2(0, -climbSpeed);
            movement.ClimbMove(downVelocity);
            return;
        }

        // 입력 확인 및 이동 속도 계산
        Vector2 moveInput = inputHandler.MoveDirection;

        // 사다리 오르내리기 - 수직 이동만 처리 (좌우 이동은 무시)
        Vector2 climbVelocity = new Vector2(0f, moveInput.y * climbSpeed);

        // 사다리 중간에 플랫폼과 충돌 시 아래로 이동 가능하도록 처리
        var collisionDetector = player.GetCollisionDetector();
        if (collisionDetector.IsGrounded && moveInput.y < 0 && !isAtLadderBottom)
        {
            // 사다리 중간 플랫폼에서는 계속 아래로 이동할 수 있도록 속도 보장
            climbVelocity.y = moveInput.y * climbSpeed;
            Debug.Log("사다리 중간 플랫폼 위에서 아래로 이동 중");
        }

        // 사다리에서의 이동 적용
        movement.ClimbMove(climbVelocity);

        // 강제 위치 고정
        if (forceCenterAlignment && currentLadderCollider != null)
        {
            Vector3 pos = player.transform.position;
            pos.x = ladderCenterX;
            player.transform.position = pos;
        }
    }

    // 플레이어를 사다리 중앙에 위치시키는 메서드
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

        Debug.Log($"플레이어를 사다리 X 위치에 중앙 정렬: {ladderCenterX}");
    }

    // 사다리 바닥 확인 메서드
    private void CheckIfAtLadderBottom()
    {
        if (currentLadderCollider == null) return;

        // 사다리의 하단부 위치 계산
        float ladderBottom = currentLadderCollider.bounds.min.y;
        float playerBottom = player.GetComponent<Collider2D>().bounds.min.y;
        
        // 플레이어가 사다리 하단 근처에 있는지 확인 (약간의 여유 추가)
        float threshold = 0.2f; // 사다리 바닥 판정 여유값
        isAtLadderBottom = Mathf.Abs(playerBottom - ladderBottom) < threshold;

        // 디버그 정보
        if (isAtLadderBottom)
        {
            Debug.Log($"사다리 바닥 근처에 있음: 플레이어 바닥={playerBottom}, 사다리 바닥={ladderBottom}");
        }
    }
}