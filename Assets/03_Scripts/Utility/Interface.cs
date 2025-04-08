public interface Idamageable
{
    void TakeDamage(int damage);
}

public interface IPlayerState
{
    void Enter();
    void HandleInput();
    void Update();
    void FixedUpdate();
    void Exit();
}