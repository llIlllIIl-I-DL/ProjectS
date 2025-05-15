using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class PlayerAttackStateMachine : MonoBehaviour
{
    // 상태 변수
    public AttackStateType CurrentAttackState { get; private set; }
    private Dictionary<AttackStateType, IPlayerAttackState> states = new Dictionary<AttackStateType, IPlayerAttackState>();
    
    // 필요한 컴포넌트 참조
    private PlayerInputHandler inputHandler;
    private PlayerMovement movement;
    private PlayerAnimator animator;
    private WeaponManager weaponManager;
    private PlayerMovementStateMachine movementStateMachine;
    
    // 상태 플래그
    private bool isAttacking = false;
    private bool isCharging = false;

    void Awake()
    {
        // 컴포넌트 초기화
        inputHandler = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        animator = GetComponent<PlayerAnimator>();
        weaponManager = GetComponent<WeaponManager>();
        movementStateMachine = GetComponent<PlayerMovementStateMachine>();
        
        // 상태 초기화
        InitializeStates();
    }
    
    private void InitializeStates()
    {
        states[AttackStateType.None] = new PlayerNoneAttackState(this);
        states[AttackStateType.Attacking] = new PlayerAttackingAttackState(this);
        states[AttackStateType.MoveAttacking] = new PlayerMoveAttackingAttackState(this);
        states[AttackStateType.Charging] = new PlayerChargingAttackState(this);
        states[AttackStateType.Overcharging] = new PlayerOverchargingAttackState(this);
    }

    void Start()
    {
        // 초기 상태 설정
        ChangeState(AttackStateType.None);
    }

    void Update()
    {
        // 현재 상태 업데이트
        states[CurrentAttackState].HandleInput();
        states[CurrentAttackState].Update();

        // 애니메이션 업데이트
        UpdateAnimation();

        // 공격키가 눌려있지 않으면 isAttacking을 false로 설정
        if (inputHandler != null && !inputHandler.IsAttackPressed)
        {
            SetAttacking(false);
        }
    }
    
    // 상태 변경 메서드
    public void ChangeState(AttackStateType newState)
    {
        // 현재 상태 종료
        if (states.ContainsKey(CurrentAttackState))
        {
            states[CurrentAttackState].Exit();
        }
        
        // 새 상태로 변경
        CurrentAttackState = newState;
        states[CurrentAttackState].Enter();
        
        Debug.Log($"공격 상태 변경: {newState}");
    }
    
    // 애니메이션 업데이트
    private void UpdateAnimation()
    {
        if (animator != null)
        {
            animator.UpdateAttackAnimation(CurrentAttackState);
        }
    }
    
    // 상태 플래그 설정 메서드
    public void SetAttacking(bool value) => isAttacking = value;
    public void SetCharging(bool value) => isCharging = value;
    
    // 상태 플래그 확인 메서드
    public bool IsAttacking => isAttacking;
    public bool IsCharging => isCharging;

    // 컴포넌트 접근자
    public PlayerInputHandler GetInputHandler() => inputHandler;
    public PlayerMovement GetMovement() => movement;
    public WeaponManager GetWeaponManager() => weaponManager;
    public PlayerMovementStateMachine GetMovementStateMachine() => movementStateMachine;
    public PlayerAnimator GetAnimator() => animator;
}
