using UnityEngine;

/// <summary>
/// 벽 슬라이딩 상태를 처리하는 클래스
/// 플레이어가 벽에 붙어 미끄러지는 동작을 관리
/// </summary>
public class PlayerWallSlidingState : IPlayerState
{
    #region 변수

    private readonly PlayerStateManager stateManager;
    private float wallSlideStartTime;
    private int wallDirection; // 벽의 방향: -1(왼쪽), 1(오른쪽)
    
    private PlayerMovement movement;
    private CollisionDetector collisionDetector;
    private PlayerInputHandler inputHandler;

    #endregion

    #region 초기화

    public PlayerWallSlidingState(PlayerStateManager stateManager)
    {
        this.stateManager = stateManager;
    }

    #endregion

    #region 상태 메서드

    /// <summary>
    /// 벽 슬라이딩 상태 진입 시 호출
    /// </summary>
    public void Enter()
    {
        // 필요한 컴포넌트 캐싱
        movement = stateManager.GetMovement();
        collisionDetector = stateManager.GetCollisionDetector();
        inputHandler = stateManager.GetInputHandler();
        
        // 벽 슬라이딩 상태 설정
        stateManager.SetWallSliding(true);
        wallSlideStartTime = Time.time;
        
        // 벽 방향 감지
        wallDirection = collisionDetector.WallDirection;
        
        Debug.Log($"벽 슬라이딩 상태 시작 - 벽 방향: {wallDirection}");
    }

    /// <summary>
    /// 벽 슬라이딩 중 입력 처리
    /// </summary>
    public void HandleInput()
    {
        // 벽에서 반대 방향으로 입력하면 벽에서 떨어짐
        if (ShouldDetachFromWall())
        {
            ExitToFallingState();
        }
    }

    /// <summary>
    /// 벽 슬라이딩 중 매 프레임 업데이트
    /// </summary>
    public void Update()
    {
        // 상태가 변경되었는지 확인 (지상에 착지 또는 벽에서 떨어짐)
        if (CheckStateTransitions())
        {
            return;
        }
    }

    /// <summary>
    /// 벽 슬라이딩 중 물리 업데이트 (고정 타임스텝)
    /// </summary>
    public void FixedUpdate()
    {
        ApplyWallSlide();
    }

    /// <summary>
    /// 벽 슬라이딩 상태 종료 시 호출
    /// </summary>
    public void Exit()
    {
        // 벽 슬라이딩 상태 종료
        stateManager.SetWallSliding(false);
        Debug.Log("벽 슬라이딩 상태 종료");
    }

    #endregion

    #region 유틸리티 메서드

    /// <summary>
    /// 플레이어가 벽에서 떨어져야 하는지 확인
    /// </summary>
    private bool ShouldDetachFromWall()
    {
        // 벽 방향과 반대로 입력이 들어오면 벽에서 떨어짐
        bool isDetachInput = (wallDirection < 0 && inputHandler.IsRightPressed) ||
                             (wallDirection > 0 && inputHandler.IsLeftPressed);
                             
        return isDetachInput;
    }

    /// <summary>
    /// 상태 전환 여부 확인 (지상 착지 또는 벽에서 떨어짐)
    /// </summary>
    private bool CheckStateTransitions()
    {
        // 땅에 닿으면 Idle 또는 Running 상태로 전환
        if (collisionDetector.IsGrounded)
        {
            TransitionToGroundState();
            return true;
        }

        // 벽에서 떨어지거나 플레이어 방향이 벽 방향과 일치하지 않으면 Falling 상태로 전환
        if (!IsWallSlideValid())
        {
            ExitToFallingState();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 벽 슬라이딩이 유효한지 확인
    /// </summary>
    private bool IsWallSlideValid()
    {
        // 벽에 닿아있는지 확인
        bool isTouchingWall = collisionDetector.IsTouchingWall;
        
        // 벽 방향이 설정되어 있는지 확인 (CollisionDetector에서 이미 플레이어 방향과 일치할 때만 wallDirection을 설정함)
        bool isWallDirectionValid = collisionDetector.WallDirection != 0;
        
        // 플레이어 방향과 벽 방향이 일치하는지 확인 (중복 체크지만 안전을 위해 유지)
        bool isPlayerFacingWall = movement.FacingDirection == wallDirection;
        
        // 모든 조건을 충족해야 유효한 벽 슬라이딩으로 간주
        bool isValid = isTouchingWall && isWallDirectionValid && isPlayerFacingWall;
        
        if (!isValid)
        {
            Debug.Log($"벽 슬라이딩 무효 - 벽 접촉: {isTouchingWall}, 벽 방향 유효: {isWallDirectionValid}, 플레이어가 벽 바라봄: {isPlayerFacingWall}");
        }
        
        return isValid;
    }

    /// <summary>
    /// 지상 상태로 전환
    /// </summary>
    private void TransitionToGroundState()
    {
        // 이동 중이면 Running, 아니면 Idle
        var targetState = inputHandler.IsMoving() ? 
                         PlayerStateType.Running : 
                         PlayerStateType.Idle;
                         
        stateManager.ChangeState(targetState);
    }

    /// <summary>
    /// 낙하 상태로 전환
    /// </summary>
    private void ExitToFallingState()
    {
        stateManager.ChangeState(PlayerStateType.Falling);
    }

    /// <summary>
    /// 벽 슬라이딩 물리 적용
    /// </summary>
    private void ApplyWallSlide()
    {
        // 아래 방향키를 누르고 있으면 빠르게 슬라이딩
        bool fastSlide = inputHandler.IsDownPressed;
        
        // 벽 슬라이딩 물리 적용
        float slideSpeed = stateManager.GetSettings().wallSlideSpeed;
        movement.WallSlide(slideSpeed, fastSlide);
    }

    #endregion
}