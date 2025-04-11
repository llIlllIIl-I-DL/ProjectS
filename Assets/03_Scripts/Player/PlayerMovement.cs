using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerSettings settings;

    private Rigidbody2D rb;
    private int facingDirection = 1;

    // 상태 플래그
    private bool isSprinting = false;
    private bool canJumpAfterDash = false; // 대시 후 점프 가능 상태 플래그
    private bool isDashing = false; // 현재 대시 중인지 확인하는 플래그
    private float dashVelocity = 0f; // 대시 속도를 저장

    public int FacingDirection => facingDirection;
    public Vector2 Velocity => rb.velocity;
    public bool IsDashing => isDashing; // 대시 상태 확인용 프로퍼티

    public event System.Action<int> OnDirectionChanged;
    public event System.Action OnDashEnd;
    public event System.Action OnDashCooldownComplete;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
    }

    public void Move(Vector2 moveDirection, bool sprint = false)
    {
        // 방향 변경 - 대시 중에도 방향 전환 가능
        if (moveDirection.x != 0)
        {
            int newDirection = (int)Mathf.Sign(moveDirection.x);
            if (facingDirection != newDirection)
            {
                facingDirection = newDirection;
                transform.localScale = new Vector3(facingDirection, 1, 1);
                OnDirectionChanged?.Invoke(facingDirection);

                // 대시 중이라면 대시 방향도 업데이트
                if (isDashing)
                {
                    dashVelocity = facingDirection * Mathf.Abs(dashVelocity);
                }
            }
        }

        // 대시 중이면 이동 로직 생략 (대시 속도는 유지)
        if (isDashing) return;

        // 이동 입력이 없으면 빠르게 감속
        if (Mathf.Abs(moveDirection.x) < 0.01f)
        {
            ApplyFriction();
            return;
        }

        // 목표 속도 (스프린트 상태 반영)
        float currentMoveSpeed = settings.moveSpeed;
        if (sprint || isSprinting)
        {
            currentMoveSpeed *= settings.sprintMultiplier;
        }

        float targetSpeed = moveDirection.x * currentMoveSpeed;
        float speedDiff = targetSpeed - rb.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? settings.acceleration : settings.deceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, settings.velocityPower) * Mathf.Sign(speedDiff);

        rb.AddForce(movement * Vector2.right);
    }

    public Rigidbody2D GetRigidbody()
    {
        return rb;
    }

    // 낙하 중 중력 가속 메서드
    public void ApplyFallGravityMultiplier(float multiplier)
    {
        if (rb.velocity.y < 0)
        {
            // 낙하 중일 때 중력 가속도 증가
            rb.velocity += Vector2.up * Physics2D.gravity.y * (multiplier - 1) * Time.deltaTime;
        }
    }

    private void ApplyFriction()
    {
        // 속도가 매우 작으면 완전히 멈춤
        if (Mathf.Abs(rb.velocity.x) < 0.6f)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        // 강한 마찰력 적용
        float friction = Mathf.Min(Mathf.Abs(rb.velocity.x), settings.frictionAmount * 2);
        friction *= Mathf.Sign(rb.velocity.x);
        rb.AddForce(Vector2.right * -friction, ForceMode2D.Impulse);
    }
    public void Jump(float force)
    {
        // 수평 속도 제한 (대시 중이거나 대시 후 점프인 경우 대시 속도 유지)
        float currentXVelocity = rb.velocity.x;

        // 대시 중에도 점프 가능
        if (isDashing)
        {
            // 대시 중 점프하면 대시가 종료되고 대시 속도를 점프에 적용
            currentXVelocity = dashVelocity;
            isDashing = false;
            rb.gravityScale = 1;  // 중력 복원

            // 대시 종료 이벤트 발생
            OnDashEnd?.Invoke();

            // 점프 후에는 쿨다운 시작
            StartCoroutine(DashCooldownAfterJump());
        }
        // 대시 직후의 점프라면 대시 가속도를 유지
        else if (canJumpAfterDash)
        {
            // 대시 속도를 그대로 유지 (dashVelocity 사용)
            currentXVelocity = dashVelocity;
            canJumpAfterDash = false; // 대시 후 점프 기회 사용
        }
        else
        {
            // 일반 점프는 기존과 동일하게 속도 제한
            if (Mathf.Abs(currentXVelocity) > settings.moveSpeed * 1.5f)
            {
                currentXVelocity = Mathf.Sign(currentXVelocity) * settings.moveSpeed * 1.5f;
            }
        }

        // 점프 수행
        rb.velocity = new Vector2(currentXVelocity, force);
    }

    // 점프 가능 여부 체크 메서드 추가
    public bool CanJump()
    {
        // 대시 중이거나 대시 직후, 또는 일반적인 점프 조건을 만족하면 true
        return isDashing || canJumpAfterDash || true; // true 대신 지면 체크 등의 일반 점프 조건이 들어갈 수 있음 
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
        // 이미 대시 중이라면 중복 실행 방지
        if (isDashing) return;

        StartCoroutine(DashCoroutine(dashSpeed, dashDuration));
    }

    private IEnumerator DashCoroutine(float dashSpeed, float dashDuration)
    {
        isDashing = true;
        float startTime = Time.time;
        // 현재 속도 저장 (대시 시작 전)
        float currentSpeed = Mathf.Abs(rb.velocity.x);
        // 스프린트 상태 확인 및 기본 속도 계산
        float baseSpeed = settings.moveSpeed;
        if (isSprinting)
        {
            baseSpeed *= settings.sprintMultiplier;
        }
        // 현재 속도가 최소값보다 작으면 기본 속도의 일정 비율로 설정
        if (currentSpeed < baseSpeed * 0.5f)
        {
            currentSpeed = baseSpeed;
        }
        // 대시 시작 시 속도 설정
        dashVelocity = facingDirection * Mathf.Max(currentSpeed, baseSpeed) * dashSpeed / baseSpeed; // 대시 속도 저장
        rb.velocity = new Vector2(dashVelocity, 0f);
        rb.gravityScale = 0;

        while (Time.time < startTime + dashDuration)
        {
            // 대시 중 방향은 바뀔 수 있지만 속도의 크기는 유지
            rb.velocity = new Vector2(dashVelocity, 0f);
            yield return null;
        }

        // 대시 종료 시 중력 복원
        rb.gravityScale = 1;
        isDashing = false;

        // 대시 직후 점프 가능 상태로 설정
        canJumpAfterDash = true;

        // 대시 종료 이벤트 발생
        OnDashEnd?.Invoke();

        // 대시 직후 점프를 위한 짧은 타이머 (0.2초 동안만 대시 후 점프 가능)
        StartCoroutine(DashJumpWindow());

        yield return new WaitForSeconds(settings.dashCooldown);

        OnDashCooldownComplete?.Invoke();
    }

    // 대시 후 점프 가능 시간 윈도우
    private IEnumerator DashJumpWindow()
    {
        // 대시 후 점프 가능 시간 (0.2초로 설정, 필요에 따라 조정 가능)
        yield return new WaitForSeconds(0.2f);

        // 시간이 지나면 대시 후 점프 불가능으로 변경
        canJumpAfterDash = false;
    }

    // 대시 중 점프 후 쿨다운
    private IEnumerator DashCooldownAfterJump()
    {
        yield return new WaitForSeconds(settings.dashCooldown);
        OnDashCooldownComplete?.Invoke();
    }

    public void WallSlide(float slideSpeed, bool fastSlide = false)
    {
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
        rb.velocity = Vector2.zero;
        rb.AddForce(new Vector2(-facingDirection * direction.x, direction.y) * force, ForceMode2D.Impulse);
        StartCoroutine(DisableMovement(0.2f));
    }

    private IEnumerator DisableMovement(float duration)
    {
        // 임시 플래그 설정 (실제 구현에서는 PlayerInputHandler 또는 PlayerStateManager와 통신 필요)
        // 여기서는 간단히 구현
        yield return new WaitForSeconds(duration);
    }

    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting;
    }

    // 사다리 오르기 이동 메서드 추가
    public void ClimbMove(Vector2 moveVelocity)
    {
        if (rb == null) return;

        // 사다리 오르기는 X, Y 모두 직접 속도 설정
        rb.velocity = moveVelocity;

        // X 방향 이동이 있으면 방향 체크
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
}