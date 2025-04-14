using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerSettings settings;

    private Rigidbody2D rb;
    private int facingDirection = 1;

    // 상태 플래그
    private bool isSprinting = false;
    private bool hasWingsuit = true;
    private bool isJetpackActive = false;
    
    // 제트팩 시간 및 쿨타임 관련 변수
    private float jetpackMaxDuration = 15f;  // 제트팩 최대 사용 시간 (초)
    private float jetpackCooldown = 40f;     // 제트팩 쿨타임 (초)
    private float jetpackTimer = 0f;         // 제트팩 사용 시간 타이머
    private float jetpackCooldownTimer = 0f; // 제트팩 쿨타임 타이머
    private bool jetpackOnCooldown = false;  // 제트팩 쿨타임 상태 플래그
    
    // 더블탭 감지용 변수
    private float lastUpKeyTime = -10f;
    private float doubleTapTimeThreshold = 0.3f;

    public int FacingDirection => facingDirection;
    public Vector2 Velocity => rb.velocity;
    
    public bool HasWingsuit { get => hasWingsuit; set => hasWingsuit = value; }
    public bool JetpackOnCooldown => jetpackOnCooldown;
    public float JetpackCooldownRemaining => jetpackOnCooldown ? jetpackCooldown - jetpackCooldownTimer : 0f;
    public float JetpackTimeRemaining => isJetpackActive ? jetpackMaxDuration - jetpackTimer : 0f;

    public event System.Action<int> OnDirectionChanged;
    public event System.Action OnDashEnd;
    public event System.Action OnDashCooldownComplete;
    public event System.Action OnJetpackActivated;
    public event System.Action OnJetpackDeactivated;
    public event System.Action OnJetpackCooldownStarted;
    public event System.Action OnJetpackCooldownEnded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    private void Update()
    {
        // 제트팩 사용 중이면 타이머 증가
        if (isJetpackActive)
        {
            jetpackTimer += Time.deltaTime;
            
            // 최대 사용 시간 초과 시 비활성화
            if (jetpackTimer >= jetpackMaxDuration)
            {
                DeactivateJetpack();
                StartJetpackCooldown();
            }
        }
        
        // 쿨타임 진행 중이면 타이머 증가
        if (jetpackOnCooldown)
        {
            jetpackCooldownTimer += Time.deltaTime;
            
            // 쿨타임 종료 시 초기화
            if (jetpackCooldownTimer >= jetpackCooldown)
            {
                jetpackOnCooldown = false;
                jetpackCooldownTimer = 0f;
                OnJetpackCooldownEnded?.Invoke();
                Debug.Log("제트팩 쿨타임 종료");
            }
        }
    }

    public void Move(Vector2 moveDirection, bool sprint = false)
    {
        // 제트팩 모드일 때는 다른 이동 로직 사용
        if (isJetpackActive)
        {
            MoveJetpack(moveDirection);
            return;
        }
        
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
    
    // 쿨타임 시작 메서드
    private void StartJetpackCooldown()
    {
        jetpackOnCooldown = true;
        jetpackCooldownTimer = 0f;
        OnJetpackCooldownStarted?.Invoke();
        Debug.Log("제트팩 쿨타임 시작: " + jetpackCooldown + "초");
    }
    
    // 위 화살표 키 입력 처리 메서드 추가
    public void CheckUpKeyDoubleTap()
    {
        if (!hasWingsuit) return;
        if (jetpackOnCooldown) 
        {
            Debug.Log("제트팩 쿨타임 중: " + (jetpackCooldown - jetpackCooldownTimer).ToString("F1") + "초 남음");
            return;
        }
        
        float currentTime = Time.time;
        if (currentTime - lastUpKeyTime < doubleTapTimeThreshold)
        {
            // 더블탭 감지됨
            ToggleJetpackMode();
            lastUpKeyTime = -10f; // 연속 활성화 방지
        }
        else
        {
            lastUpKeyTime = currentTime;
        }
    }
    
    // 제트팩 모드 토글
    private void ToggleJetpackMode()
    {
        if (isJetpackActive)
        {
            DeactivateJetpack();
            StartJetpackCooldown();
        }
        else
        {
            ActivateJetpack();
        }
    }
    
    // 제트팩 모드 활성화
    public void ActivateJetpack()
    {
        if (jetpackOnCooldown)
        {
            Debug.Log("제트팩이 쿨타임 중입니다: " + (jetpackCooldown - jetpackCooldownTimer).ToString("F1") + "초 남음");
            return;
        }
        
        isJetpackActive = true;
        jetpackTimer = 0f;
        rb.gravityScale = 0f;
        OnJetpackActivated?.Invoke();
        Debug.Log("제트팩 활성화: " + jetpackMaxDuration + "초 사용 가능");
    }
    
    // 제트팩 모드 비활성화
    public void DeactivateJetpack()
    {
        if (!isJetpackActive) return;
        
        isJetpackActive = false;
        rb.gravityScale = 1f;
        OnJetpackDeactivated?.Invoke();
        Debug.Log("제트팩 비활성화: 사용 시간 " + jetpackTimer.ToString("F1") + "초");
    }
    
    // 제트팩 이동 처리
    private void MoveJetpack(Vector2 moveDirection)
    {
        // 방향 변경 처리
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
        
        // 제트팩 이동 속도 설정
        float jetpackSpeed = settings.moveSpeed * 1.2f;
        Vector2 targetVelocity = new Vector2(moveDirection.x * jetpackSpeed, moveDirection.y * jetpackSpeed);
        
        // 부드러운 이동을 위한 가속도 적용
        rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, Time.deltaTime * 5f);
    }
    
    // 제트팩 모드 상태 확인
    public bool IsJetpackActive()
    {
        return isJetpackActive;
    }
    
    // 제트팩 모드 자동 비활성화 (필요에 따라 호출)
    public void DeactivateJetpackAfterDelay(float delay)
    {
        if (isJetpackActive)
        {
            StartCoroutine(DeactivateJetpackDelayed(delay));
        }
    }
    
    private IEnumerator DeactivateJetpackDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        DeactivateJetpack();
        StartJetpackCooldown();
    }
    
    // 쿨타임 리셋 (디버그용)
    public void ResetJetpackCooldown()
    {
        jetpackOnCooldown = false;
        jetpackCooldownTimer = 0f;
        Debug.Log("제트팩 쿨타임 리셋됨");
    }
    
    // 제트팩 상태 초기화 (외부에서 호출 가능)
    public void ResetJetpack()
    {
        if (isJetpackActive)
        {
            DeactivateJetpack();
        }
        
        jetpackOnCooldown = false;
        jetpackCooldownTimer = 0f;
        jetpackTimer = 0f;
    }
    
    public Rigidbody2D GetRigidbody()
    {
        return rb;
    }

    // 낙하 중 중력 가속 메서드
    public void ApplyFallGravityMultiplier(float multiplier)
    {
        if (isJetpackActive) return;
        
        if (rb.velocity.y < 0)
        {
            // 낙하 중일 때 중력 가속도 증가
            rb.velocity += Vector2.up * Physics2D.gravity.y * (multiplier - 1) * Time.deltaTime;
        }
    }
    private void ApplyFriction()
    {
        // 제트팩 모드일 때는 마찰력 적용 안함
        if (isJetpackActive) return;
        
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
        // 제트팩 모드일 때는 점프 불가
        if (isJetpackActive) return;
        
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
        // 제트팩 모드일 때는 점프 컷 불가
        if (isJetpackActive) return;
        
        if (rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * settings.jumpCutMultiplier);
        }
    }

    public void Dash(float dashSpeed, float dashDuration)
    {
        // 제트팩 모드일 때는 대시 불가
        if (isJetpackActive) return;
        
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
        OnDashEnd?.Invoke();

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