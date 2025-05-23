using System.Collections;
using System.Linq;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerSettings settings;
    private Rigidbody2D rb;
    private PlayerHP stats;
    public static PlayerMovement Instance { get; private set; }

    private int facingDirection = 1;
    private bool isSprinting = false;
    private bool hasWingsuit = true;
    private bool isOnSlope = false;
    private float slopeAngle;
    private Vector2 slopeNormalPerpendicular;
    
    // 대시 관련 변수
    private float currentDashSpeed = 0f;
    private bool wasDashing = false;
    private float dashSpeedDecayRate = 0.95f;
    private float dashSpeedTimer = 0f;
    private bool jumpRequested = false;
    
    [Header("경사로 설정")]
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private PhysicsMaterial2D noFriction;
    [SerializeField] private PhysicsMaterial2D fullFriction;
    [SerializeField] private float slopeCheckDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer;
    
    private PlayerStateManager stateManager;
    private bool isGroundedLastFrame = false;

    public int FacingDirection => facingDirection;
    public Vector2 Velocity => rb.velocity;
    public bool HasWingsuit { get => hasWingsuit; set => hasWingsuit = value; }
    public bool IsOnSlope => isOnSlope;

    public event System.Action<int> OnDirectionChanged;
    public event System.Action OnDashEnd;
    public event System.Action OnDashCooldownComplete;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerHP>();
        stateManager = GetComponent<PlayerStateManager>();
        
        if (settings != null)
        {
            dashSpeedDecayRate = settings.dashSpeedDecayRate;
        }
        
        if (noFriction == null || fullFriction == null)
        {
            Debug.LogWarning("마찰력 자료가 설정되지 않았습니다. 경사로 이동에 문제가 생길 수 있습니다.");
        }
    }
    
    private void FixedUpdate()
    {
        if (IsGrounded())
        {
            SlopeCheck();
        }
        else
        {
            isOnSlope = false;
            rb.sharedMaterial = noFriction;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    public void Move(Vector2 moveDirection, bool sprint = false)
    {
        bool isCrouching = stateManager != null && stateManager.IsCrouching;
        
        if (moveDirection.x != 0)
        {
            int newDirection = (int)Mathf.Sign(moveDirection.x);
            if (facingDirection != newDirection)
            {
                facingDirection = newDirection;
                transform.localScale = new Vector3(facingDirection, 1, 1);
                OnDirectionChanged?.Invoke(facingDirection);
            }
        }

        if (Mathf.Abs(moveDirection.x) < 0.01f)
        {
            ApplyFriction();
            return;
        }

        float currentMoveSpeed = stats != null ? stats.MoveSpeed : settings.moveSpeed;
        if ((sprint || isSprinting) && !isCrouching)
        {
            currentMoveSpeed += settings.sprintMultiplier;
        }

        float targetSpeed = moveDirection.x * currentMoveSpeed;
        
        if (isOnSlope)
        {
            MoveOnSlope(targetSpeed);
        }
        else
        {
            float speedDiff = targetSpeed - rb.velocity.x;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? settings.acceleration : settings.deceleration;
            float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, settings.velocityPower) * Mathf.Sign(speedDiff);

            rb.AddForce(movement * Vector2.right);
        }
    }
    
    private void SlopeCheck()
    {
        Vector2 checkPos = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckDistance, groundLayer);
        
        if (hit)
        {
            slopeNormalPerpendicular = Vector2.Perpendicular(hit.normal).normalized;
            slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            isOnSlope = slopeAngle != 0f && slopeAngle <= maxSlopeAngle;
            
            Debug.DrawRay(hit.point, hit.normal, Color.green);
            Debug.DrawRay(hit.point, slopeNormalPerpendicular, Color.red);
            
            if (IsGrounded() && isOnSlope && rb.velocity.y <= 0.1f)
            {
                rb.sharedMaterial = fullFriction;
            }
            else
            {
                rb.sharedMaterial = noFriction;
            }
        }
        else
        {
            isOnSlope = false;
            rb.sharedMaterial = noFriction;
        }
    }
    
    private void MoveOnSlope(float targetSpeed)
    {
        Vector2 slopeForce = slopeNormalPerpendicular * targetSpeed;
        float currentSlopeVelocity = Vector2.Dot(rb.velocity, slopeNormalPerpendicular);
        float speedDiff = targetSpeed - currentSlopeVelocity;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? settings.acceleration : settings.deceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, settings.velocityPower) * Mathf.Sign(speedDiff);
        
        rb.AddForce(movement * slopeNormalPerpendicular);
        
        if (IsGrounded() && Mathf.Abs(targetSpeed) < 0.1f && rb.velocity.magnitude < 0.1f)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        else
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }
    
    private bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.1f, groundLayer);
        
        if (hit.collider != null && !isGroundedLastFrame && wasDashing)
        {
            ResetDashState();
            Debug.Log("지면에 착지하여 대시 상태 초기화");
        }
        
        isGroundedLastFrame = hit.collider != null;
        return hit.collider != null;
    }
    
    public void CheckUpKeyDoubleTap()
    {
        Debug.Log("제트팩 기능이 제거되었습니다.");
    }
    
    public Rigidbody2D GetRigidbody()
    {
        return rb;
    }

    public void ApplyFallGravityMultiplier(float multiplier)
    {
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (multiplier - 1) * Time.deltaTime;
        }
    }
    
    private void ApplyFriction()
    {
        if (Mathf.Abs(rb.velocity.x) < 0.6f)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        float friction = Mathf.Min(Mathf.Abs(rb.velocity.x), settings.frictionAmount * 2);
        friction *= Mathf.Sign(rb.velocity.x);
        rb.AddForce(Vector2.right * -friction, ForceMode2D.Impulse);
    }

    public void Jump(float force)
    {
        jumpRequested = true;
        float jumpForce = force;
        
        if (wasDashing && Mathf.Abs(currentDashSpeed) > 0.1f)
        {
            float inputDirection = Input.GetAxisRaw("Horizontal");
            float jumpDirection = Mathf.Abs(inputDirection) > 0.1f ? Mathf.Sign(inputDirection) : Mathf.Sign(currentDashSpeed);
            float jumpSpeed = Mathf.Abs(currentDashSpeed) * jumpDirection;
            rb.velocity = new Vector2(jumpSpeed, jumpForce);
            Debug.Log($"대시 점프: 입력 방향 {inputDirection}, 점프 방향 {jumpDirection}, X축 속도 {jumpSpeed}, Y축 속도 {jumpForce}");
        }
        else
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            
            if (wasDashing)
            {
                ResetDashState();
                Debug.Log("일반 점프로 대시 상태 초기화");
            }
        }
    }

    public void JumpCut()
    {
        if (rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * settings.jumpCutMultiplier);
        }
    }

    public void Dash(float dashSpeed, float dashDuration)
    {
        currentDashSpeed = facingDirection * dashSpeed;
        wasDashing = true;
        rb.velocity = new Vector2(0, rb.velocity.y);
        StartCoroutine(DashCoroutine(dashSpeed, dashDuration));
    }

    private IEnumerator DashCoroutine(float dashSpeed, float dashDuration)
    {
        float startTime = Time.time;
        rb.velocity = new Vector2(facingDirection * dashSpeed, 0f);
        rb.gravityScale = 0;
        bool dashInterrupted = false;
        jumpRequested = false;
        int dashDirection = facingDirection;

        while (Time.time < startTime + dashDuration && !dashInterrupted)
        {
            if (UtilityChangedStatController.Instance.currentUtilityList.Any(u => u.id == 1015))
            {
                UtilityChangedStatController.Instance.InvincibleWhenDash();
            }

            if (jumpRequested || rb.velocity.y > 0.1f)
            {
                dashInterrupted = true;
                Debug.Log("대시 중 점프로 인해 대시가 중단되었습니다. 대시 속도는 유지됨");
                break;
            }

            rb.velocity = new Vector2(dashDirection * dashSpeed, 0f);
            yield return null;
        }

        rb.gravityScale = 1;
        
        if (!dashInterrupted)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
        
        OnDashEnd?.Invoke();
        UtilityChangedStatController.Instance.isInvincibleDash = false;
        
        float cooldownTime = dashInterrupted ? settings.dashCooldown * 0.5f : settings.dashCooldown;
        
        if (!dashInterrupted)
        {
            currentDashSpeed = 0f;
            wasDashing = false;
        }
        
        jumpRequested = false;
        
        yield return new WaitForSeconds(cooldownTime);
        OnDashCooldownComplete?.Invoke();
        
        if (dashInterrupted)
        {
            StartCoroutine(ResetDashStateAfterDelay(0.2f));
        }
    }

    private IEnumerator ResetDashStateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetDashState();
        Debug.Log("대시 상태 종료 후 0.2초 뒤 초기화 완료");
    }

    public void ResetDashState()
    {
        currentDashSpeed = 0f;
        wasDashing = false;
        dashSpeedTimer = 0f;
        rb.velocity = new Vector2(rb.velocity.x * 0.5f, rb.velocity.y);
        Debug.Log("대시 상태 초기화됨");
    }

    public void WallSlide(float slideSpeed, bool fastSlide = false)
    {
        Vector2 raycastDirection = new Vector2(facingDirection, 0);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, raycastDirection, 0.7f, groundLayer);
        
        Debug.DrawRay(transform.position, raycastDirection * 0.7f, Color.blue);
        
        if (!hit.collider)
        {
            Debug.Log("벽이 감지되지 않아 벽 슬라이딩을 수행하지 않습니다.");
            return;
        }
        
        float dotProduct = Vector2.Dot(raycastDirection, hit.normal);
        
        if (dotProduct >= 0)
        {
            Debug.Log("플레이어의 방향과 벽의 방향이 일치하지 않아 벽 슬라이딩을 수행하지 않습니다.");
            return;
        }
        
        Debug.Log($"벽 슬라이딩 실행 중: 속도={slideSpeed}, 빠른 슬라이딩={fastSlide}");
        
        float targetSpeed = fastSlide ? -settings.wallFastSlideSpeed : -settings.wallSlideSpeed;

        if (rb.velocity.y < targetSpeed)
        {
            float speedDif = targetSpeed - rb.velocity.y;
            float movement = speedDif * settings.wallSlideAcceleration;
            movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime),
                                Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));

            rb.AddForce(movement * Vector2.up);
        }
    }

    public void WallJump(float force, Vector2 direction)
    {
        Debug.Log($"벽 점프 실행: 힘={force}, 방향=({-facingDirection * direction.x}, {direction.y})");
        
        rb.velocity = Vector2.zero;
        rb.AddForce(new Vector2(-facingDirection * direction.x, direction.y) * force, ForceMode2D.Impulse);
        StartCoroutine(DisableMovement(0.2f));
    }

    private IEnumerator DisableMovement(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    public void SetSprinting(bool sprinting)
    {
        if (stateManager != null && stateManager.IsCrouching && sprinting)
        {
            isSprinting = false;
            return;
        }
        isSprinting = sprinting;
    }

    public void ClimbMove(Vector2 moveVelocity)
    {
        if (rb == null) return;
        
        rb.velocity = moveVelocity;
        
        if (!Mathf.Approximately(moveVelocity.x, 0f))
        {
            CheckDirectionChange(moveVelocity.x);
        }
    }

    private void CheckDirectionChange(float moveX)
    {
        if ((moveX > 0 && facingDirection < 0) || (moveX < 0 && facingDirection > 0))
        {
            facingDirection = (int)Mathf.Sign(moveX);
            transform.localScale = new Vector3(facingDirection, 1, 1);
            OnDirectionChanged?.Invoke(facingDirection);
        }
    }

    public float GetCurrentMoveSpeed()
    {
        float currentSpeed = stats != null ? stats.MoveSpeed : settings.moveSpeed;
        if (isSprinting)
        {
            currentSpeed *= settings.sprintMultiplier;
        }
        
        return Mathf.Max(currentSpeed, 5f);
    }

    public void SetFacingDirection(int direction)
    {
        if (facingDirection != direction)
        {
            facingDirection = direction;
            OnDirectionChanged?.Invoke(facingDirection);
            Debug.Log($"PlayerMovement: 캐릭터 방향이 {facingDirection}로 변경되었습니다.");
        }
    }

    public void ApplyDashSpeedDecay()
    {
        if (wasDashing && Mathf.Abs(currentDashSpeed) > 0.1f)
        {
            if (dashSpeedTimer <= 0f && settings != null)
            {
                dashSpeedTimer = settings.dashJumpSpeedDuration;
                Debug.Log($"대시 점프: 대시 속도 {currentDashSpeed} 유지 시간 {dashSpeedTimer}초");
            }
            
            dashSpeedTimer -= Time.deltaTime;
            currentDashSpeed *= 0.998f;
            
            if (dashSpeedTimer <= 0f)
            {
                dashSpeedTimer = 0f;
                Debug.Log("대시 속도 유지 시간 종료");
                return;
            }
            
            rb.velocity = new Vector2(currentDashSpeed, rb.velocity.y);
        }
    }
}