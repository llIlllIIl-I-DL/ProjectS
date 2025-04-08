using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Player/Settings")]
public class PlayerSettings : ScriptableObject
{
    [Header("이동 파라미터")]
    public float moveSpeed = 7f;
    public float sprintMultiplier = 1.5f;
    public float doubleTapTime = 0.3f;
    public float acceleration = 60f;
    public float deceleration = 60f;
    public float velocityPower = 0.9f;
    public float frictionAmount = 0.2f;

    [Header("점프 파라미터")]
    public float jumpForce = 12f;
    public float jumpCutMultiplier = 0.5f;
    public float fallGravityMultiplier = 1.7f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.1f;

    [Header("대시 파라미터")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.4f;

    [Header("벽 파라미터")]
    public float wallSlideSpeed = 3f;
    public float wallSlideAcceleration = 5f;
    public float wallFastSlideSpeed = 6f;
    public float wallJumpForce = 12f;
    public Vector2 wallJumpDirection = new Vector2(1f, 1.5f);
    public float wallStickTime = 0.2f;
}

