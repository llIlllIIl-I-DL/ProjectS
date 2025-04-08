using UnityEngine;

public class PlayerSprintingState : PlayerStateBase
{
    private float originalMoveSpeed;
    private float sprintTimer;
    private const float MAX_SPRINT_TIME = 3f; // 최대 스프린트 지속 시간(초)
    private const float SPRINT_COOLDOWN = 1.5f; // 스프린트 쿨다운 시간(초)
    private bool isSprintExhausted = false;

    public PlayerSprintingState(PlayerController playerController) : base(playerController) { }

    public override void Enter()
    {
        player.UpdateAnimations(null);

        // 쿨다운 중이면 스프린트 불가
        if (isSprintExhausted)
        {
            player.ChangeState(PlayerStateType.Running);
            return;
        }

        // 스프린트 타이머 초기화
        sprintTimer = MAX_SPRINT_TIME;

        // 이펙트 재생 (선택적)
        PlaySprintEffect();
    }

    public override void Exit()
    {
        player.IsSprinting = false;
        // 스프린트 이펙트 종료
        StopSprintEffect();
    }

    public override void HandleInput()
    {
        // 스프린트 중 점프 입력 처리
        if (player.JumpPressed && player.IsGrounded())
        {
            player.Jump();
            player.ChangeState(PlayerStateType.Jumping);
            player.JumpPressed = false;
        }

        // 대시 입력 처리
        if (player.DashPressed && player.CanDash)
        {
            player.StartDash();
            player.DashPressed = false;
        }
    }

    public override void Update()
    {
        // 스프린트 타이머 감소
        sprintTimer -= Time.deltaTime;

        // 스프린트 게이지 UI 업데이트 (필요한 경우)
        UpdateSprintUI();

        // 스프린트 타이머가 다 되면 일반 달리기로 전환
        if (sprintTimer <= 0)
        {
            isSprintExhausted = true;
            player.ChangeState(PlayerStateType.Running);

            // 스프린트 쿨다운 시작 - 플레이어 코루틴으로 실행
            player.StartCoroutine(SprintCooldown());
            return;
        }

        // 움직임이 없으면 Idle 상태로 전환
        if (!player.IsMoving())
        {
            player.ChangeState(PlayerStateType.Idle);
            return;
        }

        // 스프린트 상태가 끝나면 일반 달리기로 전환
        if (!player.IsSprinting)
        {
            player.ChangeState(PlayerStateType.Running);
            return;
        }

        // 지면에서 떨어지면 낙하 상태로 전환
        if (!player.IsGrounded())
        {
            player.ChangeState(PlayerStateType.Falling);
            return;
        }

        // 애니메이션 업데이트
        player.UpdateAnimations(null);
    }

    public override void FixedUpdate()
    {
        // 스프린트 이동 실행
        player.Move();

        // 발자국 효과는 일단 주석 처리 (미구현 상태에서 오류 방지)
        // if (Time.frameCount % 5 == 0) // 5프레임마다 발자국 생성
        // {
        //     CreateFootstep();
        // }
    }

    // 스프린트 이펙트 재생
    private void PlaySprintEffect()
    {
        // 스프린트 이펙트 구현 (파티클, 소리 등)
        // 예: 먼지 파티클 생성
        // GameObject dustEffect = Object.Instantiate(dustPrefab, player.transform.position, Quaternion.identity);
    }

    // 스프린트 이펙트 중지
    private void StopSprintEffect()
    {
        // 스프린트 이펙트 중지 로직
    }

    // 발자국 생성
    private void CreateFootstep()
    {
        // 발자국 효과는 나중에 구현
        // 예: Vector3 footPosition = player.transform.position - new Vector3(0, 0.5f, 0);
        // GameObject footprint = Object.Instantiate(footprintPrefab, footPosition, Quaternion.identity);
    }

    // 스프린트 UI 업데이트
    private void UpdateSprintUI()
    {
        // 실제 UI 참조가 없으므로 로그만 출력하거나 아무 작업도 하지 않음
        // 필요시 UI 참조를 설정하고 업데이트
    }

    // 스프린트 쿨다운 코루틴
    private System.Collections.IEnumerator SprintCooldown()
    {
        yield return new WaitForSeconds(SPRINT_COOLDOWN);
        isSprintExhausted = false;
    }
}