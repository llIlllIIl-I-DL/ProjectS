using System;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask ladderLayer;
    [SerializeField] private bool showDebugRays = true;

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
        }

        if (wasOnLadder != IsOnLadder)
        {
            OnLadderTouchChanged?.Invoke(IsOnLadder);
        }
    }

    public void SetFacingDirection(int direction)
    {
        facingDirection = direction;
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
        RaycastHit2D hit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            new Vector2(0.2f, boxCollider.bounds.size.y * 0.8f),
            0f,
            new Vector2(facingDirection, 0),
            0.3f,
            wallLayer
        );

        if (hit.collider != null)
        {
            Debug.Log($"벽 감지됨! collider={hit.collider.name}, 거리={hit.distance}");
        }

        if (showDebugRays)
        {
            Color rayColor = hit.collider != null ? Color.green : Color.blue;

            Debug.DrawRay(
                boxCollider.bounds.center + new Vector3(boxCollider.bounds.extents.x * facingDirection, boxCollider.bounds.extents.y * 0.8f, 0),
                new Vector3(0.3f * facingDirection, 0, 0),
                rayColor,
                0.1f
            );

            Debug.DrawRay(
                boxCollider.bounds.center + new Vector3(boxCollider.bounds.extents.x * facingDirection, 0, 0),
                new Vector3(0.3f * facingDirection, 0, 0),
                rayColor,
                0.1f
            );

            Debug.DrawRay(
                boxCollider.bounds.center + new Vector3(boxCollider.bounds.extents.x * facingDirection, -boxCollider.bounds.extents.y * 0.8f, 0),
                new Vector3(0.3f * facingDirection, 0, 0),
                rayColor,
                0.1f
            );
        }

        return hit.collider != null;
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