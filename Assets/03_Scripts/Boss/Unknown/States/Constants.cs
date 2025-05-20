using UnityEngine;

public static class GameConstants
{
    // 보스 관련 상수
    public static class Boss
    {
        public const float DETECTION_RANGE = 10f;
        public const float ATTACK_RANGE = 5f;
        public const float KICK_RANGE = 2f;
        public const float KICK_COOLDOWN = 20f;
        public const float CHARGED_ATTACK_COOLDOWN = 20f;

        // 이동 속도
        public const float NORMAL_MOVE_SPEED = 1f;
        public const float FAST_MOVE_SPEED = 3f;

        // 데미지 값
        public const int SLASH_DAMAGE = 20;
        public const int KICK_DAMAGE = 30;
        public const float NORMAL_PROJECTILE_DAMAGE = 10f;
        public const float CHARGED_PROJECTILE_DAMAGE = 50f;

        // 공격 관련 타이밍
        public const float ATTACK_DELAY = 0.5f;
        public const float KICK_ATTACK_DELAY = 0.3f;
        public const float RETURN_TO_IDLE_DELAY = 1.0f;
        
        // 투사체 공격 관련
        public const float NORMAL_PROJECTILE_SPEED = 10f;
        public const float CHARGED_PROJECTILE_SPEED = 8f;
        public const int MAX_PROJECTILE_COUNT = 3;
    }
    
    // 레이어 이름
    public static class Layers
    {
        public const string GROUND = "Ground";
        public const string WALL = "Wall";
        public const string PLAYER = "Player";
    }
    
    // 애니메이션 파라미터 이름
    public static class AnimParams
    {
        public const string IS_IDLE = "IsIdle";
        public const string IS_MOVING = "IsMoving";
        public const string IS_SLASHING = "IsSlashing";
        public const string IS_KICKING = "IsKicking";
        public const string MOVE_SPEED = "MoveSpeed";
        public const string FIRE_PROJECTILE = "FireProjectile";
        public const string CHARGE_ATTACK = "ChargeAttack";
        public const string HIT = "setHit";
        public const string DIE = "setDie";
        public const string DEAD = "setDead";
    }
    
    // 태그 이름
    public static class Tags
    {
        public const string PLAYER = "Player";
    }
}