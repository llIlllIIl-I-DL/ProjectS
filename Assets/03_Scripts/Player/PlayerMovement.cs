using System.Collections;
using System.Linq;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerSettings settings;
    private Rigidbody2D rb;
    private PlayerHP stats; // PlayerStats로 이름 변경 예정
    //싱글톤 선언
    public static PlayerMovement Instance { get; private set; }

    private int facingDirection = 1;

    // 상태 플래그
    private bool isSprinting = false;
    private bool hasWingsuit = true;
    private bool isOnSlope = false;
    private float slopeAngle;
    private Vector2 slopeNormalPerpendicular;
    
    // 대시 관련 변수
    private float currentDashSpeed = 0f;
    private bool wasDashing = false;
    private float dashSpeedDecayRate = 0.95f; // 대시 속도 감소율 (settings에서 초기화됨)
    private float dashSpeedTimer = 0f; // 대시 속도 유지 타이머
    
    // 점프 관련 변수
    private bool jumpRequested = false; // 점프 요청 플래그 추가
    
    [Header("경사로 설정")]
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private PhysicsMaterial2D noFriction;
    [SerializeField] private PhysicsMaterial2D fullFriction;
    [SerializeField] private float slopeCheckDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer;
    
    private PlayerStateManager stateManager;
    
    public int FacingDirection => facingDirection;
    public Vector2 Velocity => rb.velocity;
    
    public bool HasWingsuit { get => hasWingsuit; set => hasWingsuit = value; }
    public bool IsOnSlope => isOnSlope;

    public event System.Action<int> OnDirectionChanged;
    public event System.Action OnDashEnd;
    public event System.Action OnDashCooldownComplete;

    // 이전 프레임의 지면 상태를 저장할 변수 추가
    private bool isGroundedLastFrame = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerHP>(); // PlayerStats로 변경 예정
        stateManager = GetComponent<PlayerStateManager>();
        
        // 설정에서 값 초기화
        if (settings != null)
        {
            dashSpeedDecayRate = settings.dashSpeedDecayRate;
        }
        
        // 마찰력 자료 확인
        if (noFriction == null || fullFriction == null)
        {
            Debug.LogWarning("마찰력 자료가 설정되지 않았습니다. 경사로 이동에 문제가 생길 수 있습니다.");
        }
    }
    
    
    private void FixedUpdate()
    {
        // 경사로 체크 (지상에 있을 때만)
        if (IsGrounded())
        {
            SlopeCheck();
        }
        else
        {
            // 공중에 있을 때는 경사로 상태를 리셋하고 마찰력 제거
            isOnSlope = false;
            rb.sharedMaterial = noFriction;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 회전만 고정
        }
    }

    public void Move(Vector2 moveDirection, bool sprint = false)
    {
        // 앉기 상태일 때 스프린트 불가
        bool isCrouching = stateManager != null && stateManager.IsCrouching;
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

        // 목표 속도 (스프린트 상태 반영, 단 앉기 상태에서는 스프린트 불가)
        float currentMoveSpeed = stats != null ? stats.MoveSpeed : settings.moveSpeed;
        if ((sprint || isSprinting) && !isCrouching)
        {
            currentMoveSpeed += settings.sprintMultiplier;
        }

        float targetSpeed = moveDirection.x * currentMoveSpeed;
        
        // 경사로에 있을 때 이동 처리
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
    
    // 경사로 체크
    private void SlopeCheck()
    {
        Vector2 checkPos = transform.position;
        
        // 아래 방향으로 레이캐스트 발사
        RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, slopeCheckDistance, groundLayer);
        
        if (hit)
        {
            // 경사로 법선 벡터와 수직 벡터 계산
            slopeNormalPerpendicular = Vector2.Perpendicular(hit.normal).normalized;
            
            // 경사로 각도 계산
            slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            
            // 경사로 여부 확인
            isOnSlope = slopeAngle != 0f && slopeAngle <= maxSlopeAngle;
            
            // 디버그 시각화
            Debug.DrawRay(hit.point, hit.normal, Color.green);
            Debug.DrawRay(hit.point, slopeNormalPerpendicular, Color.red);
            
            // 마찰력 조정 - 지상에 있고 경사로에 있을 때만 마찰력 적용
            if (IsGrounded() && isOnSlope && rb.velocity.y <= 0.1f)
            {
                rb.sharedMaterial = fullFriction; // 경사로에서는 마찰력 있음
            }
            else
            {
                rb.sharedMaterial = noFriction; // 경사로 아니거나 움직일 때는 마찰력 없음
            }
        }
        else
        {
            isOnSlope = false;
            rb.sharedMaterial = noFriction; // 경사로 감지되지 않으면 마찰력 없음
        }
    }
    
    // 경사로에서 이동
    private void MoveOnSlope(float targetSpeed)
    {
        // 경사로를 따라 움직이는 힘 계산
        Vector2 slopeForce = slopeNormalPerpendicular * targetSpeed;
        
        // 힘을 가하기 전에 현재 속도와 목표 속도의 차이 계산
        float currentSlopeVelocity = Vector2.Dot(rb.velocity, slopeNormalPerpendicular);
        float speedDiff = targetSpeed - currentSlopeVelocity;
        
        // 가속도 계산
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? settings.acceleration : settings.deceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, settings.velocityPower) * Mathf.Sign(speedDiff);
        
        // 경사로 방향으로 힘을 가함
        rb.AddForce(movement * slopeNormalPerpendicular);
        
        // 경사로에서 중력에 의한 미끄러짐 방지 (지상에 있을 때만 적용)
        if (IsGrounded() && Mathf.Abs(targetSpeed) < 0.1f && rb.velocity.magnitude < 0.1f)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        else
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }
    
    // 지상에 있는지 체크하는 메서드 추가
    private bool IsGrounded()
    {
        // 간단한 지상 체크, 아래 방향으로 짧은 레이캐스트
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.1f, groundLayer);
        
        // 이전에 공중에 있었는데 지면에 착지했다면 대시 상태 초기화
        if (hit.collider != null && !isGroundedLastFrame && wasDashing)
        {
            ResetDashState();
            Debug.Log("지면에 착지하여 대시 상태 초기화");
        }
        
        // 현재 지면 상태 저장
        isGroundedLastFrame = hit.collider != null;
        
        return hit.collider != null;
    }
    
    // Player.cs 스크립트와의 호환성을 위한 더미 메서드
    public void CheckUpKeyDoubleTap()
    {
        // 제트팩 기능이 제거되어 비어있는 메서드
        Debug.Log("제트팩 기능이 제거되었습니다.");
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
        // 점프 요청 플래그 설정
        jumpRequested = true;
        
        float jumpForce = force;
        
        // 대시 중 점프시 대시 속도 유지
        if (wasDashing && Mathf.Abs(currentDashSpeed) > 0.1f)
        {
            // 대시 중 점프 - X축 속도는 대시 속도 유지
            rb.velocity = new Vector2(currentDashSpeed, jumpForce);
            Debug.Log($"대시 점프: X축 속도 {currentDashSpeed} 유지, Y축 속도 {jumpForce} 적용");
        }
        else
        {
            // 일반 점프
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
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
        // 대시 속도 저장
        currentDashSpeed = facingDirection * dashSpeed;
        wasDashing = true;
        
        StartCoroutine(DashCoroutine(dashSpeed, dashDuration));
    }

    private IEnumerator DashCoroutine(float dashSpeed, float dashDuration)
    {
        float startTime = Time.time;

        // 대시 시작 시 속도 설정
        rb.velocity = new Vector2(facingDirection * dashSpeed, 0f);
        rb.gravityScale = 0;

        // 대시 중 상태를 확인하기 위한 플래그
        bool dashInterrupted = false;
        
        // 시작할 때 점프 요청 플래그 초기화
        jumpRequested = false;

        while (Time.time < startTime + dashDuration && !dashInterrupted)
        {
            if (UtilityChangedStatController.Instance.currentUtilityList.Any(u => u.id == 1015))
            {
                UtilityChangedStatController.Instance.InvincibleWhenDash();
            }

            // 점프 요청 플래그가 설정되었거나 y 속도가 0보다 크면 점프로 간주
            if (jumpRequested || rb.velocity.y > 0.1f)
            {
                // 점프로 인해 대시가 중단됨
                dashInterrupted = true;
                
                // 대시는 취소되지만 대시 속도는 유지
                // currentDashSpeed와 wasDashing은 초기화하지 않음
                // 이 상태값은 Jump 메서드에서 활용됨
                
                Debug.Log("대시 중 점프로 인해 대시가 중단되었습니다. 대시 속도는 유지됨");
                break;
            }

            // 대시 중 일정한 속도 유지 (x 축만)
            rb.velocity = new Vector2(facingDirection * dashSpeed, rb.velocity.y);
            yield return null;
        }

        // 대시 종료 시 중력 복원 (점프로 중단된 경우 포함)
        rb.gravityScale = 1;
        OnDashEnd?.Invoke();

        UtilityChangedStatController.Instance.isInvincibleDash = false;
        
        // 점프로 인해 대시가 중단된 경우에는 쿨다운 시간을 단축
        float cooldownTime = dashInterrupted ? settings.dashCooldown * 0.5f : settings.dashCooldown;
        
        // 점프로 중단된 경우에는 대시 상태를 초기화하지 않음 (Jump 메서드에서 활용)
        if (!dashInterrupted)
        {
            currentDashSpeed = 0f;
            wasDashing = false;
        }
        
        // 작업 완료 후 점프 요청 플래그 초기화
        jumpRequested = false;
        
        yield return new WaitForSeconds(cooldownTime);

        OnDashCooldownComplete?.Invoke();
    }

    // 대시 상태 리셋 메서드 추가
    public void ResetDashState()
    {
        currentDashSpeed = 0f;
        wasDashing = false;
        dashSpeedTimer = 0f;
        Debug.Log("대시 상태 초기화됨");
    }

    public void WallSlide(float slideSpeed, bool fastSlide = false)
    {
        // 플레이어가 바라보는 방향으로 레이캐스트를 발사하여 벽 감지
        Vector2 raycastDirection = new Vector2(facingDirection, 0);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, raycastDirection, 0.7f, groundLayer);
        
        // 디버그용 레이 시각화
        Debug.DrawRay(transform.position, raycastDirection * 0.7f, Color.blue);
        
        // 벽이 감지되지 않았거나, 플레이어가 바라보는 방향과 벽의 방향이 일치하지 않으면 리턴
        if (!hit.collider)
        {
            // 벽이 감지되지 않음
            Debug.Log("벽이 감지되지 않아 벽 슬라이딩을 수행하지 않습니다.");
            return;
        }
        
        // 플레이어의 방향과 벽의 법선 벡터를 비교하여 방향 일치 확인
        float dotProduct = Vector2.Dot(raycastDirection, hit.normal);
        
        // 플레이어가 바라보는 방향과 벽의 방향이 반대일 때 (닷 프로덕트가 음수일 때) 벽 슬라이딩 수행
        if (dotProduct >= 0)
        {
            Debug.Log("플레이어의 방향과 벽의 방향이 일치하지 않아 벽 슬라이딩을 수행하지 않습니다.");
            return;
        }
        
        // 벽 슬라이딩 디버그 로그 추가
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
        // 벽 점프 디버그 로그 추가
        Debug.Log($"벽 점프 실행: 힘={force}, 방향=({-facingDirection * direction.x}, {direction.y})");
        
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
        // 앉기 상태일 때 스프린트 불가
        if (stateManager != null && stateManager.IsCrouching && sprinting)
        {
            isSprinting = false;
            return;
        }
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

    public float GetCurrentMoveSpeed()
    {
        // settings.moveSpeed에 스프린트 상태를 고려한 값을 반환
        float currentSpeed = stats != null ? stats.MoveSpeed : settings.moveSpeed;
        if (isSprinting)
        {
            currentSpeed *= settings.sprintMultiplier;
        }
        
        // 최소값 보장
        return Mathf.Max(currentSpeed, 5f);
    }

    // 직접 캐릭터 방향을 설정하는 메서드
    public void SetFacingDirection(int direction)
    {
        if (facingDirection != direction)
        {
            facingDirection = direction;
            OnDirectionChanged?.Invoke(facingDirection);
            Debug.Log($"PlayerMovement: 캐릭터 방향이 {facingDirection}로 변경되었습니다.");
        }
    }

    // 대시 속도 감소 메서드 (점프 상태에서 호출)
    public void ApplyDashSpeedDecay()
    {
        if (wasDashing && Mathf.Abs(currentDashSpeed) > 0.1f)
        {
            // 타이머가 0이면 대시 점프를 한 시점이므로 타이머 초기화
            if (dashSpeedTimer <= 0f && settings != null)
            {
                dashSpeedTimer = settings.dashJumpSpeedDuration;
                Debug.Log($"대시 점프: 대시 속도 {currentDashSpeed} 유지 시간 {dashSpeedTimer}초");
            }
            
            // 타이머 감소
            dashSpeedTimer -= Time.deltaTime;
            
            // 점진적으로 대시 속도 감소 적용
            currentDashSpeed *= dashSpeedDecayRate;
            
            // 타이머가 종료되거나 속도가 임계값 이하면 대시 상태 초기화
            if (dashSpeedTimer <= 0f || Mathf.Abs(currentDashSpeed) < 1.0f)
            {
                ResetDashState();
                Debug.Log("대시 속도 유지 시간 종료 또는 속도 임계값 도달");
                return;
            }
            
            // 실제 X축 속도를 대시 속도로 설정 (타이머 동안 점진적으로 감소)
            rb.velocity = new Vector2(currentDashSpeed, rb.velocity.y);
        }
    }
}