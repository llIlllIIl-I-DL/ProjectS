using UnityEngine;

namespace BossFSM
{
    public class BossAttackState : BossState
    {
        private float attackTimer;
        private float attackDuration = 3f; // 공격 지속 시간(초)
        private float attackElapsed = 0f;
        private Transform player;

        public BossAttackState(BossStateMachine stateMachine, Boss boss) : base(stateMachine, boss)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        public override void EnterState()
        {
            attackTimer = 0f;
            attackElapsed = 0f;
            boss.Animator.SetBool("IsAttack", true);
        }

        public override void UpdateState()
        {
            if (player == null) return;

            attackTimer += Time.deltaTime;
            attackElapsed += Time.deltaTime;

            float distanceToPlayer = Vector3.Distance(boss.transform.position, player.position);

            if (distanceToPlayer > boss.AttackRange)
            {
                stateMachine.ChangeState(new BossMoveState(stateMachine, boss));
                return;
            }

            // 공격 지속시간이 끝나면 점프 또는 이동 중 랜덤하게 전환
            if (attackElapsed >= attackDuration)
            {
                if (Random.value < 0.5f)
                    stateMachine.ChangeState(new BossJumpState(stateMachine, boss));
                else
                    stateMachine.ChangeState(new BossMoveState(stateMachine, boss));
                return;
            }

            // 쿨타임이 끝나면 산성 점액 발사
            if (attackTimer >= boss.AttackCooldown)
            {
                boss.Animator.SetBool("IsAttack", false);
                if (boss.AcidSpawnPoint != null)
                {
                    FireAcidBullets(boss.AcidSpawnPoint, 5, 120f, 5f);
                }
                attackTimer = 0f;
            }
        }

        public void OnStayCollision2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                if (attackTimer >= boss.AttackCooldown)
                {
                    other.GetComponent<IDamageable>().TakeDamage(boss.AttackDamage);
                }
            }
        }

        public override void ExitState()
        {
            boss.Animator.SetBool("IsAttack", false);
        }

        public void FireAcidBullets(Transform spawnPoint, int bulletCount = 5, float spreadAngle = 120f, float bulletSpeed = 5f)
        {
            float minAngle = 30f;   // 2시 방향
            float maxAngle = 150f;  // 10시 방향향

            for (int i = 0; i < bulletCount; i++)
            {
                float angle = Random.Range(minAngle, maxAngle);
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

                Vector3 spawnPosition = spawnPoint.position + new Vector3(0, dir.y * 0.2f, 0);

                GameObject acid = ObjectPoolingManager.Instance.GetObject(ObjectPoolingManager.PoolType.AcidBullet);
                if (acid != null)
                {
                    acid.transform.position = spawnPosition;
                    acid.transform.rotation = Quaternion.Euler(0, 0, angle);
                    acid.SetActive(true);

                    Rigidbody2D rb = acid.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.velocity = dir * bulletSpeed;
                    }
                }
            }
        }
    }
}