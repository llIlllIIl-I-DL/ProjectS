using System;
using UnityEngine;

/// <summary>
/// 플레이어 충돌 감지 시스템
/// 지면, 벽, 사다리 등과의 충돌을 감지하고 관련 이벤트를 발생시킵니다.
/// </summary>
public class CollisionDetector : MonoBehaviour
{
    #region 변수 및 프로퍼티
    
    [Header("레이어 설정")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask ladderLayer;
    
    [Header("디버그 설정")]
    [SerializeField] private bool showDebugRays = true;
    
    [Header("충돌 감지 설정")]
    [SerializeField] private float wallCheckDistance = 0.3f;
    
    private BoxCollider2D boxCollider;
    private int facingDirection = 1;
    private int wallDirection = 0; // 0: 벽 없음, 1: 오른쪽 벽, -1: 왼쪽 벽
    private Animator animator; // 애니메이터 참조
    private PlayerMovement movement; // PlayerMovement 참조
    private int prevWallDirection = 0; // 이전 벽 방향 저장

    // 상태 프로퍼티
    public bool IsGrounded { get; private set; }
    public bool IsTouchingWall { get; private set; }
    public bool IsOnLadder { get; private set; }
    public bool IsAtTopOfLadder { get; private set; }
    public int WallDirection => wallDirection; // 벽 방향 속성

    // 이벤트
    public event Action<bool> OnGroundedChanged;
    public event Action<bool> OnWallTouchChanged;
    public event Action<bool> OnLadderTouchChanged;
    public event Action<int> OnWallDirectionChanged; // 벽 방향 변경 이벤트

    #endregion

    #region 초기화

    private void Awake()
    {
        InitializeComponents();
    }

    /// <summary>
    /// 필요한 컴포넌트 참조 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // 박스 콜라이더 찾기
        boxCollider = GetComponent<BoxCollider2D>();
        
        // 애니메이터 찾기
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // PlayerMovement 컴포넌트 참조 캐싱
        movement = GetComponent<PlayerMovement>();
        if (movement == null)
        {
            movement = GetComponentInParent<PlayerMovement>();
        }
    }

    #endregion

    #region 업데이트 및 상태 체크

    private void Update()
    {
        // 이전 상태 저장
        bool wasGrounded = IsGrounded;
        bool wasTouchingWall = IsTouchingWall;
        bool wasOnLadder = IsOnLadder;
        prevWallDirection = wallDirection;

        // 상태 업데이트
        UpdateCollisionStates();

        // 상태 변화 확인 및 이벤트 발생
        CheckStateChanges(wasGrounded, wasTouchingWall, wasOnLadder);
    }

    /// <summary>
    /// 모든 충돌 상태 업데이트
    /// </summary>
    private void UpdateCollisionStates()
    {
        IsGrounded = CheckIsGrounded();
        CheckWallContact();
        IsOnLadder = CheckIsOnLadder();
        IsAtTopOfLadder = CheckIsAtTopOfLadder();
    }

    /// <summary>
    /// 상태 변화 확인 및 이벤트 발생
    /// </summary>
    private void CheckStateChanges(bool wasGrounded, bool wasTouchingWall, bool wasOnLadder)
    {
        // 지면 상태 변화 체크
        if (wasGrounded != IsGrounded)
        {
            OnGroundedChanged?.Invoke(IsGrounded);
        }

        // 벽 접촉 상태 변화 체크
        if (wasTouchingWall != IsTouchingWall)
        {
            HandleWallTouchChange(wasTouchingWall);
        }

        // 벽 방향 변화 체크
        if (prevWallDirection != wallDirection)
        {
            HandleWallDirectionChange();
        }

        // 사다리 상태 변화 체크
        if (wasOnLadder != IsOnLadder)
        {
            OnLadderTouchChanged?.Invoke(IsOnLadder);
        }
    }

    /// <summary>
    /// 벽 접촉 상태 변화 처리
    /// </summary>
    private void HandleWallTouchChange(bool wasTouchingWall)
    {
        OnWallTouchChanged?.Invoke(IsTouchingWall);
        Debug.Log($"벽 접촉 상태 변경: {IsTouchingWall}, 벽 방향: {wallDirection}");
        
        // 벽에서 떨어질 때 애니메이션 파라미터 초기화
        if (!IsTouchingWall && animator != null)
        {
            SetWallAnimationParams(0);
        }
    }

    /// <summary>
    /// 벽 방향 변화 처리
    /// </summary>
    private void HandleWallDirectionChange()
    {
        OnWallDirectionChanged?.Invoke(wallDirection);
        
        // 애니메이션 파라미터 설정
        if (animator != null)
        {
            SetWallAnimationParams(wallDirection);
        }
    }

    #endregion

    #region 애니메이션 파라미터 설정
    
    /// <summary>
    /// 벽 애니메이션 파라미터 설정
    /// </summary>
    private void SetWallAnimationParams(int wallDir)
    {
        Debug.Log($"벽 애니메이션 파라미터 설정 시작: 벽 방향={wallDir}");
        
        // 공통 파라미터 설정
        SetCommonWallParams(wallDir);
        
        // 개별 슬라이드 파라미터 초기화
        ResetWallSlideParams();
        
        // 현재 벽 방향에 맞는 파라미터 설정
        SetDirectionalWallParams(wallDir);
        
        // 디버그 로그 출력
        LogWallSlideParamValues();
    }

    /// <summary>
    /// 벽 공통 애니메이션 파라미터 설정
    /// </summary>
    private void SetCommonWallParams(int wallDir)
    {
        // IsWallSliding 파라미터 설정 (벽 방향이 0이 아니면 true)
        if (HasParameter("IsWallSliding"))
        {
            animator.SetBool("IsWallSliding", wallDir != 0);
        }
        
        // WallDirection 파라미터 설정
        if (HasParameter("WallDirection"))
        {
            animator.SetInteger("WallDirection", wallDir);
        }
    }

    /// <summary>
    /// 좌우 벽 슬라이드 파라미터 리셋
    /// </summary>
    private void ResetWallSlideParams()
    {
        if (HasParameter("LeftWallSlide"))
        {
            animator.SetBool("LeftWallSlide", false);
        }
        
        if (HasParameter("RightWallSlide"))
        {
            animator.SetBool("RightWallSlide", false);
        }
    }

    /// <summary>
    /// 벽 방향에 따른 애니메이션 파라미터 설정
    /// </summary>
    private void SetDirectionalWallParams(int wallDir)
    {
        // 벽 방향이 0이면 (벽이 감지되지 않음) 애니메이션 설정 불필요
        if (wallDir == 0)
        {
            return;
        }
        
        // 벽 방향과 플레이어 방향이 일치하는지 확인
        bool isFacingWall = movement.FacingDirection == wallDir;
        
        // 플레이어가 벽을 바라보는 방향일 때만 애니메이션 설정
        if (isFacingWall)
        {
            if (wallDir < 0) // 왼쪽 벽
            {
                if (HasParameter("LeftWallSlide"))
                {
                    animator.SetBool("LeftWallSlide", true);
                    Debug.Log("왼쪽 벽 감지 및 플레이어가 바라봄: LeftWallSlide = true");
                }
            }
            else if (wallDir > 0) // 오른쪽 벽
            {
                if (HasParameter("RightWallSlide"))
                {
                    animator.SetBool("RightWallSlide", true);
                    Debug.Log("오른쪽 벽 감지 및 플레이어가 바라봄: RightWallSlide = true");
                }
            }
        }
        else
        {
            // 벽을 등지고 있는 경우 벽 슬라이딩 비활성화
            if (HasParameter("IsWallSliding"))
            {
                animator.SetBool("IsWallSliding", false);
                Debug.Log("벽을 보고 있지 않아 벽 슬라이딩 애니메이션 비활성화");
            }
        }
    }

    /// <summary>
    /// 벽 슬라이드 파라미터 값 로그 출력
    /// </summary>
    private void LogWallSlideParamValues()
    {
        if (HasParameter("LeftWallSlide"))
        {
            bool isLeft = animator.GetBool("LeftWallSlide");
            Debug.Log($"최종 LeftWallSlide 값: {isLeft}");
        }
        
        if (HasParameter("RightWallSlide"))
        {
            bool isRight = animator.GetBool("RightWallSlide");
            Debug.Log($"최종 RightWallSlide 값: {isRight}");
        }
    }
    
    /// <summary>
    /// 애니메이션 파라미터 존재 여부 확인
    /// </summary>
    private bool HasParameter(string paramName)
    {
        if (animator == null) return false;
        
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
            {
                return true;
            }
        }
        return false;
    }

    #endregion

    #region 충돌 검사 메서드

    /// <summary>
    /// 지면 접촉 확인
    /// </summary>
    private bool CheckIsGrounded()
    {
        RaycastHit2D hit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            new Vector2(boxCollider.bounds.size.x * 0.95f, 0.2f),
            0f,
            Vector2.down,
            0.5f,
            groundLayer
        );

        DrawGroundDebugRays(hit);

        return hit.collider != null;
    }

    /// <summary>
    /// 벽 접촉 확인 및 방향 설정
    /// </summary>
    private void CheckWallContact()
    {
        // 레이캐스트 시작점
        Vector2[] rayOrigins = GetWallRayOrigins();
        
        // 플레이어가 바라보는 방향만 체크
        Vector2 checkDirection = movement.FacingDirection > 0 ? Vector2.right : Vector2.left;
        bool wallDetected = CheckWallSide(rayOrigins, checkDirection);
        
        // 벽 방향 설정 (플레이어 방향과 일치하는 경우만)
        if (wallDetected)
        {
            wallDirection = movement.FacingDirection;
            IsTouchingWall = true;
            
            string wallSide = wallDirection > 0 ? "오른쪽" : "왼쪽";
            Debug.Log($"플레이어 방향({movement.FacingDirection})의 벽 감지됨! 벽 위치: {wallSide}");
        }
        else
        {
            wallDirection = 0;
            IsTouchingWall = false;
        }
    }

    /// <summary>
    /// 벽 레이캐스트 시작점 배열 반환
    /// </summary>
    private Vector2[] GetWallRayOrigins()
    {
        Vector2 rayOriginTop = boxCollider.bounds.center + new Vector3(0, boxCollider.bounds.extents.y * 0.7f, 0);
        Vector2 rayOriginMiddle = boxCollider.bounds.center;
        Vector2 rayOriginBottom = boxCollider.bounds.center - new Vector3(0, boxCollider.bounds.extents.y * 0.7f, 0);
        
        return new Vector2[] { rayOriginTop, rayOriginMiddle, rayOriginBottom };
    }

    /// <summary>
    /// 특정 방향(좌/우)의 벽 감지
    /// </summary>
    private bool CheckWallSide(Vector2[] rayOrigins, Vector2 direction)
    {
        bool hitDetected = false;
        Color rayColorBase = direction == Vector2.right ? Color.blue : Color.yellow;
        Color rayColorHit = direction == Vector2.right ? Color.green : Color.red;
        
        // 3개 지점에서 레이캐스트 발사
        for (int i = 0; i < rayOrigins.Length; i++)
        {
            RaycastHit2D hit = Physics2D.Raycast(rayOrigins[i], direction, wallCheckDistance, wallLayer);
            
            // 디버그 레이 그리기
            if (showDebugRays)
            {
                Color rayColor = hit.collider != null ? rayColorHit : rayColorBase;
                Debug.DrawRay(rayOrigins[i], direction * wallCheckDistance, rayColor);
            }
            
            // 하나라도 감지되면 벽 감지됨
            if (hit.collider != null)
            {
                hitDetected = true;
            }
        }
        
        return hitDetected;
    }

    /// <summary>
    /// 사다리 접촉 확인
    /// </summary>
    private bool CheckIsOnLadder()
    {
        RaycastHit2D hit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            new Vector2(boxCollider.bounds.size.x * 0.5f, boxCollider.bounds.size.y * 0.9f),
            0f,
            Vector2.zero,
            0.1f,
            ladderLayer
        );
        
        if (showDebugRays && hit.collider != null)
        {
            Debug.DrawRay(
                boxCollider.bounds.center,
                Vector2.up * 0.5f,
                Color.yellow,
                0.1f
            );
            Debug.Log($"사다리 감지됨! collider={hit.collider.name}");
        }
        
        return hit.collider != null;
    }

    /// <summary>
    /// 사다리 상단 접촉 확인
    /// </summary>
    private bool CheckIsAtTopOfLadder()
    {
        if (!IsOnLadder) return false;
        
        RaycastHit2D hit = Physics2D.BoxCast(
            boxCollider.bounds.center + new Vector3(0, boxCollider.bounds.extents.y + 0.1f, 0),
            new Vector2(boxCollider.bounds.size.x * 0.5f, 0.1f),
            0f,
            Vector2.up,
            0.2f,
            groundLayer
        );
        
        if (showDebugRays && hit.collider != null)
        {
            Debug.DrawRay(
                boxCollider.bounds.center + new Vector3(0, boxCollider.bounds.extents.y, 0),
                Vector2.up * 0.2f,
                Color.cyan,
                0.1f
            );
            Debug.Log("사다리 상단 감지됨!");
        }
        
        return hit.collider != null;
    }

    #endregion

    #region 디버그 시각화

    /// <summary>
    /// 지면 디버그 레이 그리기
    /// </summary>
    private void DrawGroundDebugRays(RaycastHit2D hit)
    {
        if (!showDebugRays) return;
        
        Debug.DrawRay(
            boxCollider.bounds.center + new Vector3(boxCollider.bounds.extents.x * 0.9f, -boxCollider.bounds.extents.y, 0),
            Vector2.down * 0.2f,
            Color.red
        );
        Debug.DrawRay(
            boxCollider.bounds.center - new Vector3(boxCollider.bounds.extents.x * 0.9f, boxCollider.bounds.extents.y, 0),
            Vector2.down * 0.2f,
            Color.red
        );
    }

    #endregion

    #region 공개 메서드

    /// <summary>
    /// 캐릭터 방향 설정
    /// </summary>
    public void SetFacingDirection(int direction)
    {
        facingDirection = direction;
        Debug.Log($"방향이 변경되었습니다: {facingDirection}");
    }

    #endregion
}