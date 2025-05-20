public enum PlayerStateType
{
    Idle,
    Running,
    Sprinting,
    Jumping,
    Falling,
    WallSliding,
    Dashing,
    Attacking,
    MoveAttacking,
    Hit,
    Crouching,
    CrouchWalking,
    Climbing,
    Death
}

public enum EnemyStateType
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Jump,
    ChargeAttack,
    SlamAttack
}

public enum RotationType // ObjectRotatingPlatform에서 사용
{
    Center,
    Left,   
    Right    
}