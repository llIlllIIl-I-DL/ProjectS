using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerMovementStateMachine : MonoBehaviour
{
    // 상태 변수
    public MovementStateType CurrentMovementState { get; private set; }
    private Dictionary<MovementStateType, IPlayerMovementState> states = new Dictionary<MovementStateType, IPlayerMovementState>();
    
    // 필요한 컴포넌트 참조
    private PlayerInputHandler inputHandler;
    private PlayerMovement movement;
    private CollisionDetector collisionDetector;
    private PlayerAnimator animator;
    
    // 상태 플래그
    private bool isJumping = false;
    private bool isWallSliding = false;
    private bool isCrouching = false;
    private bool isClimbing = false;
    private bool isSprinting = false;

    void Awake()
    {
        // 컴포넌트 초기화
        inputHandler = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        collisionDetector = GetComponent<CollisionDetector>();
        animator = GetComponent<PlayerAnimator>();
        
        // 상태 초기화
        InitializeStates();
    }
    
    private void InitializeStates()
    {
        states[MovementStateType.Idle] = new PlayerIdleMovementState(this);
        states[MovementStateType.Running] = new PlayerRunningMovementState(this);
        states[MovementStateType.Jumping] = new PlayerJumpingMovementState(this);
        states[MovementStateType.Falling] = new PlayerFallingMovementState(this);
        states[MovementStateType.Dashing] = new PlayerDashingMovementState(this);
        states[MovementStateType.WallSliding] = new PlayerWallSlidingMovementState(this);
        states[MovementStateType.Crouching] = new PlayerCrouchingMovementState(this);
        states[MovementStateType.Climbing] = new PlayerClimbingMovementState(this);
        states[MovementStateType.Sprinting] = new PlayerSprintingMovementState(this);
        states[MovementStateType.Death] = new PlayerDeathMovementState(this);
        // Hit 상태에 PlayerHitMovementState 사용
        states[MovementStateType.Hit] = new PlayerHitMovementState(this);
    }

    void Start()
    {
        // 초기 상태 설정
        ChangeState(MovementStateType.Idle);
    }

    void Update()
    {
        // 현재 상태 업데이트
        states[CurrentMovementState].HandleInput();
        states[CurrentMovementState].Update();

        // 애니메이션 업데이트
        UpdateAnimation();
    }
    
    void FixedUpdate()
    {
        // 현재 상태 물리 업데이트
        states[CurrentMovementState].FixedUpdate();
    }
    
    // 상태 변경 메서드
    public void ChangeState(MovementStateType newState)
    {
        // 현재 상태 종료
        if (states.ContainsKey(CurrentMovementState))
        {
            states[CurrentMovementState].Exit();
        }
        
        // 새 상태로 변경
        CurrentMovementState = newState;
        states[CurrentMovementState].Enter();
        
        Debug.Log($"이동 상태 변경: {newState}");
    }
    
    // 애니메이션 업데이트
    private void UpdateAnimation()
    {
        if (animator != null)
        {
            animator.UpdateMovementAnimation(CurrentMovementState, inputHandler.IsMoving(), movement.Velocity);
        }
    }
    
    // 상태 플래그 설정 메서드
    public void SetJumping(bool value) => isJumping = value;
    public void SetWallSliding(bool value) => isWallSliding = value;
    public void SetCrouching(bool value) => isCrouching = value;
    public void SetClimbing(bool value) => isClimbing = value;
    public void SetSprinting(bool value) 
    {
        isSprinting = value;
        // PlayerMovement에 스프린트 상태 전달 (있는 경우)
        if (movement != null)
        {
            movement.SetSprinting(value);
        }
        // 애니메이터에도 상태 전달 (있는 경우)
        if (animator != null)
        {
            animator.SetSprinting(value);
        }
    }
    
    // 상태 플래그 확인 메서드
    public bool IsJumping => isJumping;
    public bool IsWallSliding => isWallSliding;
    public bool IsCrouching => isCrouching;
    public bool IsClimbing => isClimbing;
    public bool IsSprinting => isSprinting;

    // 컴포넌트 접근자
    public PlayerInputHandler GetInputHandler() => inputHandler;
    public PlayerMovement GetMovement() => movement;
    public CollisionDetector GetCollisionDetector() => collisionDetector;
}
