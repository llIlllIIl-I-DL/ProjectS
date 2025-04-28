using System;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask ladderLayer;
    [SerializeField] private bool showDebugRays = true;
    [SerializeField] private float wallCheckDistance = 0.3f;

    private BoxCollider2D boxCollider;
    private int facingDirection = 1;
    private int wallDirection = 0; // 0: 벽 없음, 1: 오른쪽 벽, -1: 왼쪽 벽
    private Animator animator; // 애니메이터 참조 추가
    private int prevWallDirection = 0; // 이전 벽 방향 저장

    public bool IsGrounded { get; private set; }
    public bool IsTouchingWall { get; private set; }
    public bool IsOnLadder { get; private set; }
    public bool IsAtTopOfLadder { get; private set; }
    public int WallDirection => wallDirection; // 벽 방향 속성 추가

    public event Action<bool> OnGroundedChanged;
    public event Action<bool> OnWallTouchChanged;
    public event Action<bool> OnLadderTouchChanged;
    public event Action<int> OnWallDirectionChanged; // 벽 방향 변경 이벤트 추가

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        // 애니메이터 찾기
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        bool wasGrounded = IsGrounded;
        bool wasTouchingWall = IsTouchingWall;
        bool wasOnLadder = IsOnLadder;
        prevWallDirection = wallDirection; // 이전 벽 방향 저장

        IsGrounded = CheckIsGrounded();
        CheckWallContact(); // 벽 접촉 확인과 방향 설정
        IsOnLadder = CheckIsOnLadder();
        IsAtTopOfLadder = CheckIsAtTopOfLadder();

        if (wasGrounded != IsGrounded)
        {
            OnGroundedChanged?.Invoke(IsGrounded);
        }

        if (wasTouchingWall != IsTouchingWall)
        {
            OnWallTouchChanged?.Invoke(IsTouchingWall);
            Debug.Log($"벽 접촉 상태 변경: {IsTouchingWall}, 벽 방향: {wallDirection}");
            
            // 벽에서 떨어질 때 애니메이션 파라미터 초기화
            if (!IsTouchingWall && animator != null)
            {
                SetWallAnimationParams(0);
            }
        }

        // 벽 방향이 변경되었을 때 이벤트 발생 및 애니메이션 파라미터 설정
        if (prevWallDirection != wallDirection)
        {
            OnWallDirectionChanged?.Invoke(wallDirection);
            
            // 애니메이션 파라미터 설정
            if (animator != null)
            {
                SetWallAnimationParams(wallDirection);
            }
        }

        if (wasOnLadder != IsOnLadder)
        {
            OnLadderTouchChanged?.Invoke(IsOnLadder);
        }
    }
    
    // 벽 애니메이션 파라미터 설정 메서드
    private void SetWallAnimationParams(int wallDir)
    {
        Debug.Log($"벽 애니메이션 파라미터 설정 시작: 벽 방향={wallDir}");
        
        // IsWallSliding 파라미터 설정
        if (HasParameter("IsWallSliding"))
        {
            animator.SetBool("IsWallSliding", wallDir != 0);
            Debug.Log($"IsWallSliding 파라미터 설정: {wallDir != 0}");
        }
        
        // WallDirection 파라미터 설정
        if (HasParameter("WallDirection"))
        {
            animator.SetInteger("WallDirection", wallDir);
            Debug.Log($"WallDirection 파라미터 설정: {wallDir}");
        }
        
        // 양쪽 애니메이션 파라미터 먼저 모두 false로 초기화
        if (HasParameter("LeftWallSlide"))
        {
            animator.SetBool("LeftWallSlide", false);
        }
        
        if (HasParameter("RightWallSlide"))
        {
            animator.SetBool("RightWallSlide", false);
        }
        
        // 이제 현재 벽 방향에 맞는 파라미터만 true로 설정
        if (wallDir < 0) // 왼쪽 벽
        {
            if (HasParameter("LeftWallSlide"))
            {
                animator.SetBool("LeftWallSlide", true);
                Debug.Log("오른쪽 벽 감지: LeftWallSlide = true");
            }
        }
        else if (wallDir > 0) // 오른쪽 벽
        {
            if (HasParameter("RightWallSlide"))
            {
                animator.SetBool("RightWallSlide", true);
                Debug.Log("왼쪽 벽 감지: RightWallSlide = true");
            }
        }
        
        // 모든 파라미터 값 로그 출력
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
    
    // 애니메이션 파라미터 존재 여부 확인
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

    public void SetFacingDirection(int direction)
    {
        facingDirection = direction;
        Debug.Log($"방향이 변경되었습니다: {facingDirection}");
    }

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

        if (showDebugRays)
        {
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

        return hit.collider != null;
    }

    private void CheckWallContact()
    {
        // 오른쪽과 왼쪽 두 방향으로 레이캐스트 발사
        Vector2 rayOriginTop = boxCollider.bounds.center + new Vector3(0, boxCollider.bounds.extents.y * 0.7f, 0);
        Vector2 rayOriginMiddle = boxCollider.bounds.center;
        Vector2 rayOriginBottom = boxCollider.bounds.center - new Vector3(0, boxCollider.bounds.extents.y * 0.7f, 0);
        
        // 오른쪽 방향 체크
        RaycastHit2D hitRightTop = Physics2D.Raycast(rayOriginTop, Vector2.right, wallCheckDistance, wallLayer);
        RaycastHit2D hitRightMiddle = Physics2D.Raycast(rayOriginMiddle, Vector2.right, wallCheckDistance, wallLayer);
        RaycastHit2D hitRightBottom = Physics2D.Raycast(rayOriginBottom, Vector2.right, wallCheckDistance, wallLayer);
        
        // 왼쪽 방향 체크
        RaycastHit2D hitLeftTop = Physics2D.Raycast(rayOriginTop, Vector2.left, wallCheckDistance, wallLayer);
        RaycastHit2D hitLeftMiddle = Physics2D.Raycast(rayOriginMiddle, Vector2.left, wallCheckDistance, wallLayer);
        RaycastHit2D hitLeftBottom = Physics2D.Raycast(rayOriginBottom, Vector2.left, wallCheckDistance, wallLayer);
        
        // 디버그 레이 그리기
        if (showDebugRays)
        {
            // 오른쪽 레이 그리기
            Color rayColorRightTop = hitRightTop.collider != null ? Color.green : Color.blue;
            Color rayColorRightMiddle = hitRightMiddle.collider != null ? Color.green : Color.blue;
            Color rayColorRightBottom = hitRightBottom.collider != null ? Color.green : Color.blue;
            
            Debug.DrawRay(rayOriginTop, Vector2.right * wallCheckDistance, rayColorRightTop);
            Debug.DrawRay(rayOriginMiddle, Vector2.right * wallCheckDistance, rayColorRightMiddle);
            Debug.DrawRay(rayOriginBottom, Vector2.right * wallCheckDistance, rayColorRightBottom);
            
            // 왼쪽 레이 그리기
            Color rayColorLeftTop = hitLeftTop.collider != null ? Color.red : Color.yellow;
            Color rayColorLeftMiddle = hitLeftMiddle.collider != null ? Color.red : Color.yellow;
            Color rayColorLeftBottom = hitLeftBottom.collider != null ? Color.red : Color.yellow;
            
            Debug.DrawRay(rayOriginTop, Vector2.left * wallCheckDistance, rayColorLeftTop);
            Debug.DrawRay(rayOriginMiddle, Vector2.left * wallCheckDistance, rayColorLeftMiddle);
            Debug.DrawRay(rayOriginBottom, Vector2.left * wallCheckDistance, rayColorLeftBottom);
        }
        
        bool rightWall = hitRightTop.collider != null || hitRightMiddle.collider != null || hitRightBottom.collider != null;
        bool leftWall = hitLeftTop.collider != null || hitLeftMiddle.collider != null || hitLeftBottom.collider != null;
        
        // 벽 방향 설정
        int newWallDirection = 0;
        if (rightWall) newWallDirection = 1;      // 오른쪽 벽
        else if (leftWall) newWallDirection = -1; // 왼쪽 벽
        
        // 벽 방향 업데이트
        wallDirection = newWallDirection;
        
        // 벽 접촉 상태 업데이트
        IsTouchingWall = rightWall || leftWall;
        
        if (IsTouchingWall)
        {
            string wallSide = wallDirection > 0 ? "오른쪽" : "왼쪽";
            Debug.Log($"벽 감지됨! 벽 위치: {wallSide}, 방향: {wallDirection}");
        }
    }

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
}