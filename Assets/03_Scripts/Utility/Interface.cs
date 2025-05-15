using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage);
}

public interface IEnemyState
{
    void Enter();           // 상태에 진입했을 때
    void Exit();            // 상태에서 나갈 때
    void Update();          // 매 프레임 업데이트
    void FixedUpdate();     // 물리 업데이트
    void OnTriggerEnter2D(Collider2D other);  // 트리거 충돌 감지
}

public interface IDestructible : IDamageable
{
    void DropItem();
    void PlayDestructionEffect();
}

public interface IInteractable
{
    void Interact(GameObject interactor);
}

public interface ILaserInteractable
{
    void OnLaserHit(Vector2 hitpoint, Vector2 direction); 
}

// 기본 상태 인터페이스 (공통)
public interface IPlayerState
{
    void Enter();
    void Exit();
    void Update();
    void HandleInput();
}

// 이동 상태 인터페이스
public interface IPlayerMovementState: IPlayerState
{
    void FixedUpdate();
}

// 공격 상태 인터페이스
public interface IPlayerAttackState: IPlayerState
{

}