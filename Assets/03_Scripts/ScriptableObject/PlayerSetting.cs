using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Player/Settings")]
public class PlayerSettings : ScriptableObject
{
    [Header("이동 설정")]
    public float moveSpeed = 7f;
    public float sprintMultiplier = 1.5f; //스프린트 속도
    public float doubleTapTime = 0.3f;
    public float acceleration = 60f;
    public float deceleration = 60f;
    public float velocityPower = 0.9f;
    public float frictionAmount = 0.2f;
    [Header("앉기 설정")]
    public float crouchSpeed = 3.5f;
    public float crouchHeightRatio = 0.6f; // 앉기 상태 콜라이더 높이 비율
    public float crouchOffsetY = -0.5f;

    [Header("점프 설정")]
    public float jumpForce = 12f;
    public float jumpCutMultiplier = 0.5f;
    public float fallGravityMultiplier = 1.7f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.1f;
    [Header("고급 점프 설정")]
    public float doubleJumpForceMultiplier = 0.8f; // 이중 점프는 첫 점프의 80% 힘
    public bool canDoubleJump = false; // 이중 점프 가능 여부
    [Header("대시 파라미터")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.4f;
    public float dashSpeedDecayRate = 0.95f; // 대시 후 점프 시 속도 감소율 (프레임당)
    public float dashJumpSpeedDuration = 0.5f; // 대시 점프 시 대시 속도 유지 시간 (초)

    [Header("벽 상호작용")]
    public float wallSlideSpeed = 3f;
    public float wallSlideAcceleration = 5f;
    public float wallFastSlideSpeed = 6f;
    public float wallJumpForce = 12f;
    public Vector2 wallJumpDirection = new Vector2(1f, 1.5f);
    public float wallStickTime = 0.2f;
    
    [Header("사다리 설정")]
    public float climbSpeed = 4f; // 사다리 오르기 속도
    public float climbHorizontalSpeed = 2f; // 사다리에서 좌우 이동 속도
    public bool allowClimbJump = true; // 사다리에서 점프 가능 여부
}

