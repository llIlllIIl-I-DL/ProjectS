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

    public bool IsGrounded { get; private set; }
    public bool IsTouchingWall { get; private set; }
    public bool IsOnLadder { get; private set; }
    public bool IsAtTopOfLadder { get; private set; }
    public int WallDirection => wallDirection; // 벽 방향 속성 추가

    public event Action<bool> OnGroundedChanged;
    public event Action<bool> OnWallTouchChanged;
    public event Action<bool> OnLadderTouchChanged;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        bool wasGrounded = IsGrounded;
        bool wasTouchingWall = IsTouchingWall;
        bool wasOnLadder = IsOnLadder;

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
        }

        if (wasOnLadder != IsOnLadder)
        {
            OnLadderTouchChanged?.Invoke(IsOnLadder);
        }
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
        if (rightWall) wallDirection = 1;      // 오른쪽 벽
        else if (leftWall) wallDirection = -1; // 왼쪽 벽
        else wallDirection = 0;                // 벽 없음
        
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