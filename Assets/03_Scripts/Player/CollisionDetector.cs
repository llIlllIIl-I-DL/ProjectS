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

    public bool IsGrounded { get; private set; }
    public bool IsTouchingWall { get; private set; }
    public bool IsOnLadder { get; private set; }
    public bool IsAtTopOfLadder { get; private set; }

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
        IsTouchingWall = CheckIsTouchingWall();
        IsOnLadder = CheckIsOnLadder();
        IsAtTopOfLadder = CheckIsAtTopOfLadder();

        if (wasGrounded != IsGrounded)
        {
            OnGroundedChanged?.Invoke(IsGrounded);
        }

        if (wasTouchingWall != IsTouchingWall)
        {
            OnWallTouchChanged?.Invoke(IsTouchingWall);
            Debug.Log($"벽 접촉 상태 변경: {IsTouchingWall}");
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

    private bool CheckIsTouchingWall()
    {
        Vector2 rayOriginTop = boxCollider.bounds.center + new Vector3(0, boxCollider.bounds.extents.y * 0.7f, 0);
        Vector2 rayOriginMiddle = boxCollider.bounds.center;
        Vector2 rayOriginBottom = boxCollider.bounds.center - new Vector3(0, boxCollider.bounds.extents.y * 0.7f, 0);
        
        Vector2 rayDirection = new Vector2(facingDirection, 0);
        
        RaycastHit2D hitTop = Physics2D.Raycast(rayOriginTop, rayDirection, wallCheckDistance, wallLayer);
        RaycastHit2D hitMiddle = Physics2D.Raycast(rayOriginMiddle, rayDirection, wallCheckDistance, wallLayer);
        RaycastHit2D hitBottom = Physics2D.Raycast(rayOriginBottom, rayDirection, wallCheckDistance, wallLayer);
        
        if (showDebugRays)
        {
            Color rayColorTop = hitTop.collider != null ? Color.green : Color.blue;
            Color rayColorMiddle = hitMiddle.collider != null ? Color.green : Color.blue;
            Color rayColorBottom = hitBottom.collider != null ? Color.green : Color.blue;
            
            Debug.DrawRay(rayOriginTop, rayDirection * wallCheckDistance, rayColorTop);
            Debug.DrawRay(rayOriginMiddle, rayDirection * wallCheckDistance, rayColorMiddle);
            Debug.DrawRay(rayOriginBottom, rayDirection * wallCheckDistance, rayColorBottom);
        }
        
        if (hitTop.collider != null || hitMiddle.collider != null || hitBottom.collider != null)
        {
            string hitObjectName = (hitTop.collider != null) ? hitTop.collider.name : 
                                  (hitMiddle.collider != null) ? hitMiddle.collider.name : 
                                  (hitBottom.collider != null) ? hitBottom.collider.name : "알 수 없음";
            Debug.Log($"벽 감지됨! 충돌체: {hitObjectName}, 방향: {facingDirection}");
        }
        
        return hitTop.collider != null || hitMiddle.collider != null || hitBottom.collider != null;
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