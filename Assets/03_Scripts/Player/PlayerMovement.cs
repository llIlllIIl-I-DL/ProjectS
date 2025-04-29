using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerSettings settings;
    //싱글톤 선언
    public static PlayerMovement Instance { get; private set; }

    private Rigidbody2D rb;
    private int facingDirection = 1;

    // 상태 플래그
    private bool isSprinting = false;
    private bool hasWingsuit = true;
    private bool isOnSlope = false;
    private float slopeAngle;
    private Vector2 slopeNormalPerpendicular;
    
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stateManager = GetComponent<PlayerStateManager>();
        
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
        float currentMoveSpeed = settings.moveSpeed;
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
        OnDashEnd?.Invoke();

        yield return new WaitForSeconds(settings.dashCooldown);

        OnDashCooldownComplete?.Invoke();
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
        float currentSpeed = settings.moveSpeed;
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
}