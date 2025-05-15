using UnityEngine;
using System.Collections;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private bool isActuallyClimbing; // 실제로 사다리를 오르고 있는지 여부
    private bool isPaused = false; // 애니메이션이 일시정지 상태인지

    [SerializeField] private float climbAnimFrameRate = 8f;
    private float lastFrameChangeTime = 0f;
    private int currentClimbFrame = 0;
    private int totalClimbFrames = 4;

    // 사다리 애니메이션 프레임들 (스프라이트 애니메이션용, Sprite Renderer 사용 시)
    [SerializeField] private Sprite[] climbFrames;
    private SpriteRenderer spriteRenderer;

    // 애니메이터 기반 클라이밍 설정
    [SerializeField] private bool useAnimatorForClimbing = true; // true면 Animator 사용, false면 스프라이트 직접 교체

    // 현재 설정된 상태 추적을 위한 변수
    private bool isDeadState = false;

    private void Awake()
    {
        // 애니메이터 참조 가져오기 (자식 오브젝트에 있을 수 있음)
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // 스프라이트 렌더러 참조 (직접 스프라이트 교체 방식 사용 시)
        if (!useAnimatorForClimbing)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }
    }

    private void Update()
    {
        // 애니메이터 기반이 아니고, 스프라이트 렌더러가 있고, 실제로 오르고 있는 경우에만 프레임 업데이트
        if (!useAnimatorForClimbing && spriteRenderer != null && isActuallyClimbing && climbFrames != null && climbFrames.Length > 0)
        {

            UpdateClimbFrames();
        }
    }

    private void UpdateClimbFrames()
    {
        // 프레임 레이트에 맞춰 다음 프레임으로 진행
        if (Time.time - lastFrameChangeTime > (1f / climbAnimFrameRate))
        {
            // 다음 프레임 인덱스 계산
            currentClimbFrame = (currentClimbFrame + 1) % totalClimbFrames;

            // 스프라이트 업데이트
            if (currentClimbFrame < climbFrames.Length)
            {
                spriteRenderer.sprite = climbFrames[currentClimbFrame];
            }

            lastFrameChangeTime = Time.time;
        }
    }

    public void UpdateAnimation(MovementStateType state, bool isMoving, Vector2 velocity)
    {
        if (animator == null) return;

        // 이동 상태
        if (HasParameter("IsMoving"))
        {
            bool actuallyMoving = isMoving && Mathf.Abs(velocity.x) > 0.1f;
            animator.SetBool("IsMoving", actuallyMoving);
        }

        // 상태별 파라미터
        if (HasParameter("IsSprinting"))
        {
            animator.SetBool("IsSprinting", state == MovementStateType.Sprinting);
        }

        if (HasParameter("IsJumping"))
            animator.SetBool("IsJumping", state == MovementStateType.Jumping);

        if (HasParameter("IsFalling"))
            animator.SetBool("IsFalling", state == MovementStateType.Falling);

        if (HasParameter("IsWallSliding"))
            animator.SetBool("IsWallSliding", state == MovementStateType.WallSliding);

        if (HasParameter("IsDashing"))
            animator.SetBool("IsDashing", state == MovementStateType.Dashing);

        if (HasParameter("IsHit"))
            animator.SetBool("IsHit", state == MovementStateType.Hit);

        if (HasParameter("IsAttacking"))
            animator.SetBool("IsAttacking", false); // 공격은 별도의 상태 머신에서 처리

        // 앉기 상태 파라미터
        if (HasParameter("IsCrouching"))
            animator.SetBool("IsCrouching", state == MovementStateType.Crouching);
            
        // 사망 상태 파라미터 - 이미 설정된 상태면 중복 설정하지 않음
        if (HasParameter("IsDead"))
        {
            bool shouldBeDead = state == MovementStateType.Death;
            
            // 상태가 변경되었을 때만 설정
            if (isDeadState != shouldBeDead)
            {
                isDeadState = shouldBeDead;
                animator.SetBool("IsDead", shouldBeDead);
                Debug.Log($"UpdateAnimation에서 IsDead 상태 변경: {shouldBeDead}");
            }
        }

        // 사다리 오르기 상태 파라미터
        if (HasParameter("IsClimbing"))
        {
            bool isClimbingState = state == MovementStateType.Climbing;
            animator.SetBool("IsClimbing", isClimbingState);

            // 사다리 상태가 아니면 애니메이션 일시정지 해제
            if (!isClimbingState && isPaused)
            {
                ResumeAnimation();
            }
        }
    }


    private void PauseAnimation()
    {
        if (animator == null || isPaused) return;

        // 애니메이터 기반 클라이밍일 경우
        if (useAnimatorForClimbing)
        {
            // 현재 애니메이션 프레임 유지를 위해 애니메이션 속도를 0으로 설정
            animator.speed = 0;
        }

        isPaused = true;
    }

    // 애니메이션 재개 \
    private void ResumeAnimation()
    {
        if (animator == null || !isPaused) return;

        // 애니메이터 기반 클라이밍일 경우
        if (useAnimatorForClimbing)
        {
            // 애니메이션 속도 복원
            animator.speed = 1;
        }

        isPaused = false;
    }

    // 앉기 상태 설정 메서드
    public void SetCrouching(bool isCrouching)
    {
        if (animator != null && HasParameter("IsCrouching"))
        {
            animator.SetBool("IsCrouching", isCrouching);
        }
    }

    // 달리기 상태 설정 메서드
    public void SetSprinting(bool isSprinting)
    {
        if (animator != null && HasParameter("IsSprinting"))
        {
            animator.SetBool("IsSprinting", isSprinting);
        }
    }

    // 사다리 오르기 상태 설정 메서드
    public void SetClimbing(bool isClimbing)
    {
        if (animator != null && HasParameter("IsClimbing"))
        {
            animator.SetBool("IsClimbing", isClimbing);

            // 사다리에 들어가면 처음에는 실제 오르기 애니메이션 비활성화
            if (HasParameter("IsActuallyClimbing"))
            {
                animator.SetBool("IsActuallyClimbing", false);
                isActuallyClimbing = false;
            }

            // 사다리 모드 진입/해제 시 애니메이션 속도 관리
            if (isClimbing)
            {
                // 사다리 모드 진입 시 애니메이션 준비
                animator.speed = 1;
                isPaused = false;

                // 직접 스프라이트 교체 방식일 경우 초기 프레임 설정
                if (!useAnimatorForClimbing && spriteRenderer != null && climbFrames != null && climbFrames.Length > 0)
                {
                    currentClimbFrame = 0;
                    spriteRenderer.sprite = climbFrames[currentClimbFrame];
                }
            }
            else
            {
                // 사다리 모드 해제 시 애니메이션 정상화
                if (isPaused)
                {
                    ResumeAnimation();
                }

                // 애니메이션 속도 복원
                animator.speed = 1;
            }
        }
    }

    public void SetActuallyClimbing(bool isActuallyClimbing)
    {
        if (animator == null) return;

        bool wasClimbing = this.isActuallyClimbing;
        this.isActuallyClimbing = isActuallyClimbing;

        // 애니메이터 기반 클라이밍일 경우
        if (useAnimatorForClimbing && HasParameter("IsActuallyClimbing"))
        {
            animator.SetBool("IsActuallyClimbing", isActuallyClimbing);
        }

        // 움직임 유무에 따라 애니메이션 일시정지/재개
        if (isActuallyClimbing && isPaused)
        {
            // 움직이기 시작하면 애니메이션 재개
            ResumeAnimation();

            // 직접 스프라이트 교체 방식일 경우 시간 초기화
            if (!useAnimatorForClimbing)
            {
                lastFrameChangeTime = Time.time;
            }
        }
        else if (!isActuallyClimbing && !isPaused)
        {
            // 움직임이 없을 때는 현재 프레임 유지
            PauseAnimation();
        }
    }

    // 트리거 설정 메서드
    public void SetTrigger(string triggerName)
    {
        if (animator != null && HasParameter(triggerName))
        {
            animator.SetTrigger(triggerName);
        }
    }

    // 트리거 초기화 메서드
    public void ResetTrigger(string triggerName)
    {
        if (animator != null && HasParameter(triggerName))
        {
            animator.ResetTrigger(triggerName);
        }
    }

    // 애니메이션 파라미터 존재 여부 확인
    private bool HasParameter(string paramName)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
            {
                return true;
            }
        }

        return false;
    }

    // 애니메이터 속도 설정 메서드
    public void SetAnimatorSpeed(float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
        }
    }
    
    // 애니메이터 가져오기
    public Animator GetAnimator()
    {
        return animator;
    }

    // 사망 상태 설정 메서드
    public void SetDead(bool isDead)
    {
        if (animator != null && HasParameter("IsDead"))
        {
            isDeadState = isDead;
            animator.SetBool("IsDead", isDead);
            Debug.Log($"SetDead 메서드에서 IsDead 상태 변경: {isDead}");
        }
    }

    // 벽 슬라이딩 상태 설정 메서드
    public void SetWallSliding(bool isWallSliding, int wallDirection)
    {
        if (animator != null)
        {
            // 애니메이터 파라미터 설정
            if (HasParameter("IsWallSliding"))
            {
                animator.SetBool("IsWallSliding", isWallSliding);
            }
            
            // 벽 방향에 따라 애니메이션 좌우 반전 설정
            if (isWallSliding && wallDirection != 0)
            {
                // 스프라이트 렌더러 찾기
                SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    // 벽 방향에 따라 반전 설정 (벽 방향의 반대로 플레이어가 바라봄)
                    // wallDirection: 1 = 오른쪽 벽, -1 = 왼쪽 벽
                    // 벽이 오른쪽에 있으면 플레이어는 왼쪽을 봐야 함 (flipX = true)
                    // 벽이 왼쪽에 있으면 플레이어는 오른쪽을 봐야 함 (flipX = false)
                    spriteRenderer.flipX = (wallDirection > 0);
                    
                    Debug.Log($"벽 슬라이딩 애니메이션 반전: 벽 방향 {wallDirection}, flipX={spriteRenderer.flipX}");
                }
            }
        }
    }

    // 이동 애니메이션 업데이트
    public void UpdateMovementAnimation(MovementStateType state, bool isMoving, Vector2 velocity)
    {
        if (animator == null) return;

        // 이동 관련 파라미터 설정
        animator.SetBool("IsMoving", isMoving);
        
        // 속도 설정
        animator.SetFloat("MoveSpeed", Mathf.Abs(velocity.x));
        animator.SetFloat("VerticalSpeed", velocity.y);
        
        // 상태별 애니메이션 파라미터 설정
        animator.SetBool("IsRunning", state == MovementStateType.Running);
        animator.SetBool("IsSprinting", state == MovementStateType.Sprinting);
        animator.SetBool("IsJumping", state == MovementStateType.Jumping);
        animator.SetBool("IsFalling", state == MovementStateType.Falling);
        animator.SetBool("IsWallSliding", state == MovementStateType.WallSliding);
        animator.SetBool("IsDashing", state == MovementStateType.Dashing);
        animator.SetBool("IsHit", state == MovementStateType.Hit);
        animator.SetBool("IsCrouching", state == MovementStateType.Crouching);
        
        // 사망 상태 처리
        bool shouldBeDead = state == MovementStateType.Death;
        SetDead(shouldBeDead);
        
        // 사다리 상태 처리
        bool isClimbingState = state == MovementStateType.Climbing;
        SetClimbing(isClimbingState);
    }

    // 공격 상태 애니메이션 업데이트를 위한 새 메서드 추가
    public void UpdateAttackAnimation(AttackStateType state)
    {
        if (animator == null) return;
        
        // 공격 관련 파라미터 설정
        animator.SetBool("IsAttacking", state == AttackStateType.Attacking || state == AttackStateType.MoveAttacking);
        animator.SetBool("IsCharging", state == AttackStateType.Charging);
        animator.SetBool("IsOvercharging", state == AttackStateType.Overcharging);
    }
}