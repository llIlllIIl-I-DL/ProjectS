using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDeathState : PlayerStateBase
{
    private float deathStartTime;
    private float deathAnimationDuration = 2.0f; // 사망 애니메이션 재생 시간
    private bool hasRespawned = false;
    
    // 외부에서 리스폰 상태 확인용 프로퍼티
    public bool HasRespawned => hasRespawned;

    public PlayerDeathState(PlayerStateManager stateManager) : base(stateManager)
    {
    }

    public override void Enter()
    {
        // 상태 초기화
        deathStartTime = Time.time;
        hasRespawned = false;
        
        player.SetJumping(false);
        player.SetWallSliding(false);
        player.SetSprinting(false);
        player.SetCrouching(false);
        player.SetClimbing(false);
        
        // 사망 애니메이션 재생
        var playerAnimator = player.GetComponent<PlayerAnimator>();
        if (playerAnimator != null)
        {
            // 애니메이션 초기화 후 사망 애니메이션 설정
            playerAnimator.SetDead(false); // 기존 상태 초기화
            playerAnimator.SetAnimatorSpeed(1); // 애니메이터 속도 정상화
            playerAnimator.SetDead(true); // 사망 상태 활성화
        }
        
        // 물리 효과 비활성화 - 사망 시 물리 충돌 방지
        DisablePhysics();
        
        // 사망 효과음 재생
        // AudioManager.Instance?.PlaySFX("PlayerDeath");
        
        // 게임 오버 상태로 전환
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameState.GameOver);
        }
        
        Debug.Log("플레이어 사망 상태 진입");
    }

    public override void Update()
    {
        // 이미 리스폰된 상태면 추가 처리 없음
        if (hasRespawned) return;
        
        // 사망 애니메이션 종료 후 리스폰 처리
        if (Time.time >= deathStartTime + deathAnimationDuration)
        {
            hasRespawned = true;
            
            // 애니메이션 반복 방지를 위해 애니메이터 정지
            var playerAnimator = player.GetComponent<PlayerAnimator>();
            if (playerAnimator != null)
            {
                // 애니메이터 속도를 0으로 설정하여 애니메이션 정지
                playerAnimator.SetAnimatorSpeed(0);
                
                // 사망 애니메이션 상태를 마지막 프레임으로 고정
                Animator animator = playerAnimator.GetAnimator();
                if (animator != null)
                {
                    // 현재 상태의 정규화된 시간을 1로 설정 (마지막 프레임)
                    animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 1f);
                }
            }
            
            Debug.Log("사망 애니메이션 종료, 리스폰 또는 게임 오버 처리 필요");
        }
    }

    public override void FixedUpdate()
    {
        // 사망 상태에서는 물리 이동 없음
    }

    public override void HandleInput()
    {
        // 사망 상태에서는 입력 처리 없음
        // 모든 입력을 무시하도록 PlayerInputHandler를 비활성화할 수도 있음
        var inputHandler = player.GetInputHandler();
        if (inputHandler != null && inputHandler.enabled)
        {
            inputHandler.enabled = false;
        }
    }

    public override void Exit()
    {
        Debug.Log("PlayerDeathState Exit 메서드 호출 - 사망 상태 종료");
        
        // 상태 종료 시 정리
        var playerAnimator = player.GetComponent<PlayerAnimator>();
        if (playerAnimator != null)
        {
            playerAnimator.SetDead(false);
            Debug.Log("사망 상태 종료 시 IsDead를 false로 설정");
        }
        
        // 물리 효과 다시 활성화
        EnablePhysics();
        Debug.Log("사망 상태 종료 시 물리 효과 다시 활성화");
        
        // 입력 처리 다시 활성화
        var inputHandler = player.GetInputHandler();
        if (inputHandler != null && !inputHandler.enabled)
        {
            inputHandler.enabled = true;
            Debug.Log("사망 상태 종료 시 입력 처리 다시 활성화");
        }
        
        // 상태 변수 초기화
        hasRespawned = false;
        Debug.Log("사망 상태 종료 시 hasRespawned 초기화");
    }

    private void DisablePhysics()
    {
        // Rigidbody2D 비활성화 또는 kinematic 설정
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }
        
        // 콜라이더 비활성화 (선택적)
        var colliders = player.GetComponents<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
    }
    
    private void EnablePhysics()
    {
        // Rigidbody2D 다시 활성화
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        
        // 콜라이더 다시 활성화
        var colliders = player.GetComponents<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = true;
        }
    }
} 