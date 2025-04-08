using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
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
        }
        catch (System.Exception e)
        {
            Debug.LogError($"애니메이션 파라미터 설정 중 오류: {e.Message}");
        }
    }

    private bool HasParameter(string paramName)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }

        Debug.LogWarning($"애니메이터에 '{paramName}' 파라미터가 존재하지 않습니다.");
        return false;
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
}