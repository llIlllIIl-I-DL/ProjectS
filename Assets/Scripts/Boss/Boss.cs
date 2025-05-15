using UnityEngine;
namespace BossFSM
{

    public class Boss : MonoBehaviour, IDamageable
    {
        public Animator Animator { get; private set; }
        private BossStateMachine stateMachine;
        private BossState currentState;
        private Rigidbody2D rb;
        public Rigidbody2D Rb => rb; // Rigidbody2D를 외부에서 접근할 수 있도록 공개합니다.
        [Header("이동 속도")]
        [SerializeField] private float moveSpeed = 2f; // 보스의 이동 속도  
        public float MoveSpeed => moveSpeed;
        [Header("공격 범위")]
        [SerializeField] private float attackRange = 1f; // 보스의 공격 범위  
        public float AttackRange => attackRange;
        [Header("공격력")]
        [SerializeField] private float attackDamage = 10f; // 보스의 공격력  
        public float AttackDamage => attackDamage;
        [Header("공격 쿨타임")]
        [SerializeField] private float attackCooldown = 1f; // 공격 쿨타임  
        public float AttackCooldown => attackCooldown;
        [Header("방어력")]
        [SerializeField] private float defence = 0f; // 보스의 방어력  
        public float Defence => defence;
        [Header("최대 체력")]
        [SerializeField] private float maxHealth = 100;
        public float MaxHealth => maxHealth;
        [Header("현재 체력")]
        [SerializeField] private float currentHealth;
        public float CurrentHealth => currentHealth;
        [Header("점프 지속 시간")]
        [SerializeField] private float jumpDuration = 0.5f; // 점프 지속 시간
        public float JumpDuration => jumpDuration;
        [Header("점프 힘")]
        [SerializeField] private float jumpForce = 7f; // 점프 힘
        public float JumpForce => jumpForce;
        private float contactDamageTimer = 0f;
        [Header("접촉 데미지")]
        [SerializeField] private float contactDamageCooldown = 1f; // 접촉 데미지 쿨타임
        [SerializeField] private float contactDamage = 5f; // 접촉 데미지

        [Header("산성 점액 프리팹")]
        [SerializeField] private GameObject acidPrefab; // 산성 점액 프리팹

        [SerializeField] private Transform acidSpawnPoint; // 생성 위치
        public Transform AcidSpawnPoint => acidSpawnPoint; 

        private bool isInvincible = false; // 무적 상태 플래그
        private float invincibleTime = 1f; // 무적 지속 시간(초)
        private float invincibleTimer = 0f;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            Animator = GetComponentInChildren<Animator>();
            stateMachine = GetComponent<BossStateMachine>();
            rb = GetComponent<Rigidbody2D>();
            currentHealth = MaxHealth;
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Start()
        {
            // 초기 상태를 Idle로 설정  
            currentState = new BossIdleState(stateMachine, this); // Initialize currentState  
            stateMachine.ChangeState(currentState);
        }

        public void TakeDamage(float damage)
        {
            if (isInvincible) return; // 무적 상태면 데미지 무시

            if (Animator != null)
                Animator.SetTrigger("IsHit"); // 피격 애니메이션 트리거

            isInvincible = true;
            invincibleTimer = 0f;

            if (damage - Defence == 0)
            {
                // 방어력이 피해를 완전히 상쇄한 경우  
                // 최소 데미지 1로 설정  
                currentHealth -= 1;
                return;
            }
            else if (damage - Defence < 0)
            {
                // 방어력이 피해를 일부 상쇄한 경우  
                currentHealth -= 0;
            }
            else
            {
                currentHealth -= damage - Defence;
            }
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        void Update()
        {
            contactDamageTimer += Time.deltaTime;
            if (isInvincible)
            {
                invincibleTimer += Time.deltaTime;

                // 깜빡임: 0.1초마다 흰색/원래색 반복
                if (spriteRenderer != null)
                {
                    float blinkInterval = 0.1f;
                    if (Mathf.FloorToInt(invincibleTimer / blinkInterval) % 2 == 0)
                        spriteRenderer.color = Color.white;
                    else
                        spriteRenderer.color = Color.red; // 원래 색(예시, 실제 원래색으로 바꿔도 됨)
                }

                if (invincibleTimer >= invincibleTime)
                {
                    isInvincible = false;
                    if (spriteRenderer != null)
                        spriteRenderer.color = Color.white; // 무적 끝나면 원래 색으로 복구
                }
            }
            else
            {
                if (spriteRenderer != null)
                    spriteRenderer.color = Color.white; // 평상시엔 원래 색
            }
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                if (contactDamageTimer >= contactDamageCooldown)
                {
                    other.GetComponent<IDamageable>().TakeDamage(contactDamage);
                    contactDamageTimer = 0f;
                }
            }

            // 공격 상태일 때만 산성 점액 처리
            if (stateMachine.CurrentState is BossAttackState attackState)
            {
                attackState.OnStayCollision2D(other);
            }
        }
        void OnTriggerEnter2D(Collider2D other)
        {
            if (stateMachine.CurrentState is BossAttackState attackState)
            {
                attackState.OnStayCollision2D(other);
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        private void Die()
        {
            stateMachine.ChangeState(new BossDieState(stateMachine, this));
        }
    }
}