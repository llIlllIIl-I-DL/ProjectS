using System;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private bool showDebugRays = true;

    private BoxCollider2D boxCollider;
    private int facingDirection = 1;

    public bool IsGrounded { get; private set; }
    public bool IsTouchingWall { get; private set; }

    public event Action<bool> OnGroundedChanged;
    public event Action<bool> OnWallTouchChanged;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        bool wasGrounded = IsGrounded;
        bool wasTouchingWall = IsTouchingWall;

        IsGrounded = CheckIsGrounded();
        IsTouchingWall = CheckIsTouchingWall();

        if (wasGrounded != IsGrounded)
        {
            OnGroundedChanged?.Invoke(IsGrounded);
        }

        if (wasTouchingWall != IsTouchingWall)
        {
            OnWallTouchChanged?.Invoke(IsTouchingWall);
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
}