using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerSettings settings;

    private Rigidbody2D rb;
    private int facingDirection = 1;

    // 상태 플래그
    private bool isSprinting = false;

    public int FacingDirection => facingDirection;
    public Vector2 Velocity => rb.velocity;

    public event System.Action<int> OnDirectionChanged;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Move(Vector2 moveDirection, bool sprint = false)
    {
        // 방향 변경
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

        // 이동 입력이 없으면 빠르게 감속
        if (Mathf.Abs(moveDirection.x) < 0.01f)
        {
            ApplyFriction();
            return;
        }

        // 목표 속도 (스프린트 상태 반영)
        float currentMoveSpeed = settings.moveSpeed;
        if (sprint)
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
        // 수평 속도 제한
        float currentXVelocity = rb.velocity.x;
        if (Mathf.Abs(currentXVelocity) > settings.moveSpeed * 1.5f)
        {
            currentXVelocity = Mathf.Sign(currentXVelocity) * settings.moveSpeed * 1.5f;
        }
        rb.velocity = new Vector2(currentXVelocity, 0);

        // 수직 속도 설정
        rb.velocity = new Vector2(rb.velocity.x, force);
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
        StartCoroutine(DashCoroutine(dashSpeed, dashDuration));
    }

    private IEnumerator DashCoroutine(float dashSpeed, float dashDuration)
    {
        float startTime = Time.time;

        // 대시 시작 시 속도 설정
        rb.velocity = new Vector2(facingDirection * dashSpeed, 0f);
        rb.gravityScale = 0;

        while (Time.time < startTime + dashDuration)
        {
            // 대시 중 일정한 속도 유지
            rb.velocity = new Vector2(facingDirection * dashSpeed, 0f);
            yield return null;
        }

        // 대시 종료 시 중력 복원
        rb.gravityScale = 1;
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
}