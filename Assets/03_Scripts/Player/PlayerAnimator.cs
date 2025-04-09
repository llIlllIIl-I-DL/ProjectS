using UnityEngine;
using System.Collections;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private bool isActuallyClimbing; // 실제로 사다리를 오르고 있는지 여부
    
    [SerializeField] private float minClimbSpeedThreshold = 0.1f; // 오르기 애니메이션 재생을 위한 최소 속도
    [SerializeField] private float climbAnimaitionBlendSpeed = 5f; // 사다리 오르기 애니메이션 속도 조절용
    
    private float currentClimbSpeed = 0f; // 현재 사다리 오르기 속도
    private float targetClimbSpeed = 0f; // 목표 사다리 오르기 속도
    private int lastFrameIndex = 0; // 마지막 애니메이션 프레임 인덱스
    private bool isPaused = false; // 애니메이션이 일시정지 상태인지
    private string climbingStateFullPath = ""; // 사다리 애니메이션 상태의 전체 경로

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }
    
    private void LateUpdate()
    {
        // 사다리 애니메이션 속도 처리
        if (animator != null && HasParameter("IsActuallyClimbing"))
        {
            // 애니메이션 일시정지 제어
            if (isActuallyClimbing && !isPaused)
            {
                // 움직이는 중이면 애니메이션 계속 진행
                ResumeAnimation();
            }
            else if (!isActuallyClimbing && !isPaused)
            {
                // 움직임이 멈추면 현재 프레임에서 애니메이션 정지
                PauseAnimation();
            }
        }
    }
    
    // 애니메이션 일시정지 - 현재 프레임에서 멈춤
    private void PauseAnimation()
    {
        if (animator == null || isPaused) return;
        
        // 현재 재생 중인 애니메이션 정보 가져오기
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // 사다리 오르기 상태인지 확인
        if (stateInfo.IsName("Climbing") || stateInfo.IsName("Base Layer.Climbing") || 
            (climbingStateFullPath != "" && stateInfo.IsName(climbingStateFullPath)))
        {
            // 현재 정규화된 시간 (0~1 사이)
            float normalizedTime = stateInfo.normalizedTime % 1;
            
            // 현재 프레임 인덱스 계산 (애니메이션에 프레임 수에 따라 조정)
            lastFrameIndex = Mathf.FloorToInt(normalizedTime * 30); // 30은 예상 프레임 수
            
            // 애니메이션 속도를 0으로 설정해 멈춤
            animator.speed = 0;
            isPaused = true;
            
            Debug.Log($"애니메이션 일시정지: 프레임 {lastFrameIndex}, 시간 {normalizedTime}");
        }
    }
    
    // 애니메이션 재개 - 마지막 프레임에서부터 계속
    private void ResumeAnimation()
    {
        if (animator == null || !isPaused) return;
        
        // 애니메이션 속도 복원
        animator.speed = 1;
        isPaused = false;
        
        Debug.Log("애니메이션 재개");
    }

    public void UpdateAnimation(PlayerStateType state, bool isMoving, Vector2 velocity)
    {
        if (animator == null) return;

        try
        {
            // 디버그 로그
            Debug.Log($"애니메이션 업데이트: 상태={state}, IsMoving={isMoving}, Velocity={velocity}");

            // 이동 상태
            if (HasParameter("IsMoving"))
            {
                bool actuallyMoving = isMoving && Mathf.Abs(velocity.x) > 0.1f;
                animator.SetBool("IsMoving", actuallyMoving);
            }

            // 상태별 파라미터
            bool hasSprinting = HasParameter("IsSprinting");
            Debug.Log($"IsSprinting 파라미터 존재 여부: {hasSprinting}, 현재 상태: {state == PlayerStateType.Sprinting}");

            if (hasSprinting)
            {
                animator.SetBool("IsSprinting", state == PlayerStateType.Sprinting);
                Debug.Log($"IsSprinting 파라미터 설정: {state == PlayerStateType.Sprinting}");
            }

            if (HasParameter("IsJumping"))
                animator.SetBool("IsJumping", state == PlayerStateType.Jumping);

            if (HasParameter("IsFalling"))
                animator.SetBool("IsFalling", state == PlayerStateType.Falling);

            if (HasParameter("IsWallSliding"))
                animator.SetBool("IsWallSliding", state == PlayerStateType.WallSliding);

            if (HasParameter("IsDashing"))
                animator.SetBool("IsDashing", state == PlayerStateType.Dashing);
            
            if (HasParameter("IsHit"))
                animator.SetBool("IsHit", state == PlayerStateType.Hit);

            if (HasParameter("IsAttacking"))
                animator.SetBool("IsAttacking", state == PlayerStateType.Attacking);
                
            // 앉기 상태 파라미터 추가
            if (HasParameter("IsCrouching"))
                animator.SetBool("IsCrouching", state == PlayerStateType.Crouching);
                
            // 사다리 오르기 상태 파라미터 추가 - 움직임이 있을 때만 실제 오르기 애니메이션 재생
            if (HasParameter("IsClimbing"))
            {
                bool isClimbingState = state == PlayerStateType.Climbing;
                animator.SetBool("IsClimbing", isClimbingState);
                
                // 사다리 오르기 상태가 아니면 일시정지 해제
                if (!isClimbingState && isPaused)
                {
                    ResumeAnimation();
                }
                
                // 사다리 애니메이션 상태 경로 저장 (있을 경우)
                if (isClimbingState && climbingStateFullPath == "")
                {
                    // 다음 프레임에서 현재 상태 정보 확인
                    StartCoroutine(GetClimbingStatePath());
                }
                
                // IsActuallyClimbing 파라미터가 있으면 실제 움직임에 따라 설정
                if (HasParameter("IsActuallyClimbing") && isClimbingState)
                {
                    bool actuallyClimbing = Mathf.Abs(velocity.y) > minClimbSpeedThreshold;
                    animator.SetBool("IsActuallyClimbing", actuallyClimbing);
                    isActuallyClimbing = actuallyClimbing;
                    
                    // 움직임 유무에 따라 애니메이션 일시정지/재개
                    if (actuallyClimbing && isPaused)
                    {
                        ResumeAnimation();
                    }
                    else if (!actuallyClimbing && !isPaused && state == PlayerStateType.Climbing)
                    {
                        PauseAnimation();
                    }
                }
            }
                
            // 사다리 이동 속도 파라미터 - 상하 움직임 애니메이션 속도 조절용
            if (HasParameter("ClimbSpeed") && state == PlayerStateType.Climbing)
            {
                float climbSpeed = Mathf.Abs(velocity.y);
                animator.SetFloat("ClimbSpeed", climbSpeed);

                // 움직임이 없을 때는 애니메이션을 일시정지
                if (climbSpeed < minClimbSpeedThreshold && !isPaused)
                {
                    PauseAnimation();
                }
                else if (climbSpeed >= minClimbSpeedThreshold && isPaused)
                {
                    ResumeAnimation();
                }
            }
        
        }
        catch (System.Exception e)
        {
            Debug.LogError($"애니메이션 파라미터 설정 중 오류: {e.Message}");
        }
    }
    
    // 사다리 애니메이션 상태의 전체 경로 가져오기
    private System.Collections.IEnumerator GetClimbingStatePath()
    {
        // 다음 프레임을 기다려 애니메이션이 변경된 후 상태 정보 확인
        yield return null;
        
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            // 현재 클립 이름 가져오기 시도
            if (animator.GetCurrentAnimatorClipInfo(0).Length > 0)
            {
                string clipName = animator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
                if (clipName.Contains("Climb") || clipName.Contains("climb") || clipName.Contains("Ladder"))
                {
                    // 사다리 애니메이션 상태 경로 저장
                    climbingStateFullPath = stateInfo.fullPathHash.ToString();
                    Debug.Log($"사다리 애니메이션 상태 경로 저장: {clipName}, 해시: {climbingStateFullPath}");
                }
            }
        }
    }

    public void SetSprinting(bool isSprinting)
    {
        if (animator == null) return;
        
        try
        {
            if (HasParameter("IsSprinting"))
            {
                animator.SetBool("IsSprinting", isSprinting);
                Debug.Log($"스프린트 애니메이션 상태 변경: {isSprinting}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"스프린트 애니메이션 설정 중 오류: {e.Message}");
        }
    }
    
    // 앉기 상태 설정 메서드 추가
    public void SetCrouching(bool isCrouching)
    {
        if (animator == null) return;
        
        try
        {
            if (HasParameter("IsCrouching"))
            {
                animator.SetBool("IsCrouching", isCrouching);
                Debug.Log($"앉기 애니메이션 상태 변경: {isCrouching}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"앉기 애니메이션 설정 중 오류: {e.Message}");
        }
    }
    
    // 사다리 오르기 상태 설정 메서드 추가
    public void SetClimbing(bool isClimbing)
    {
        if (animator == null) return;
        
        try
        {
            if (HasParameter("IsClimbing"))
            {
                animator.SetBool("IsClimbing", isClimbing);
                Debug.Log($"사다리 오르기 애니메이션 상태 변경: {isClimbing}");
            }
            
            // 사다리에 들어가면 처음에는 실제 오르기 애니메이션 비활성화
            if (HasParameter("IsActuallyClimbing"))
            {
                animator.SetBool("IsActuallyClimbing", false);
                isActuallyClimbing = false;

                // 사다리 오르기 애니메이션 속도 초기화
                currentClimbSpeed = 0f;
                targetClimbSpeed = 0f;
            }

            // 사다리 모드 진입/해제 시 애니메이션 속도 관리
            if (isClimbing)
            {
                // 사다리 모드 진입 시 현재 프레임에서 일시정지
                if (!isPaused)
                {
                    PauseAnimation();
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
        catch (System.Exception e)
        {
            Debug.LogError($"사다리 오르기 애니메이션 설정 중 오류: {e.Message}");
        }
    }

    // 실제 사다리 오르기 애니메이션 상태 직접 제어
    public void SetActuallyClimbing(bool isActuallyClimbing)
    {
        if (animator == null) return;

        try
        {
            if (HasParameter("IsActuallyClimbing"))
            {
                bool wasClimbing = this.isActuallyClimbing;
                this.isActuallyClimbing = isActuallyClimbing;
                animator.SetBool("IsActuallyClimbing", isActuallyClimbing);

                // 움직임 상태가 변경된 경우에만 처리
                if (wasClimbing != isActuallyClimbing)
                {
                    if (isActuallyClimbing && isPaused)
                    {
                        // 움직임 시작 시 애니메이션 재개
                        ResumeAnimation();
                    }
                    else if (!isActuallyClimbing && !isPaused)
                    {
                        // 움직임 정지 시 현재 프레임에서 멈춤
                        PauseAnimation();
                    }
                    
                    Debug.Log($"실제 사다리 오르기 애니메이션 변경: {isActuallyClimbing}, 일시정지: {isPaused}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"실제 사다리 오르기 애니메이션 설정 중 오류: {e.Message}");
        }
    }

    // 트리거 설정 메서드
    public void SetTrigger(string triggerName)
    {
        if (animator == null) return;
        
        if (HasParameter(triggerName))
        {
            animator.SetTrigger(triggerName);
            Debug.Log($"애니메이션 트리거 설정: {triggerName}");
        }
        else
        {
            Debug.LogWarning($"애니메이션에 {triggerName} 트리거가 없습니다.");
        }
    }
    
    // 트리거 초기화 메서드
    public void ResetTrigger(string triggerName)
    {
        if (animator == null) return;
        
        if (HasParameter(triggerName))
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
}