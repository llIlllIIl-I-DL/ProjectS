using UnityEngine;

/// <summary>
/// 적 상태의 기본 추상 클래스
/// </summary>
public abstract class BaseEnemyState : IEnemyState
{
    // 상태가 소속된 적 참조
    protected BaseEnemy enemy;
    
    // 상태 머신 참조
    protected EnemyStateMachine stateMachine;
    
    protected BaseEnemyState(BaseEnemy enemy, EnemyStateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }
    
    public virtual void Enter() { }
    
    public virtual void Exit() { }
    
    public virtual void Update() { }
    
    public virtual void FixedUpdate() { }
    
    public virtual void OnTriggerEnter2D(Collider2D other) { }
    
    // 상태 전환 헬퍼 메서드
    protected void ChangeState(IEnemyState newState)
    {
        stateMachine.ChangeState(newState);
    }
}