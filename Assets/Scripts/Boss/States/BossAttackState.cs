using UnityEngine;

namespace BossFSM
{
    public class BossAttackState : BossState
    {
        private float attackTimer;
        private Transform player;

        public BossAttackState(BossStateMachine stateMachine, Boss boss) : base(stateMachine, boss)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        public override void EnterState()
        {
            attackTimer = 0f;
            boss.Animator.SetBool("IsAttack", true);
        }

        public override void UpdateState()
        {
            if (player == null) return;

            attackTimer += Time.deltaTime;

            float distanceToPlayer = Vector3.Distance(boss.transform.position, player.position);

            if (distanceToPlayer > boss.AttackRange)
            {
                stateMachine.ChangeState(new BossMoveState(stateMachine, boss));
                return;
            }

            // 쿨타임이 끝나면 산성 점액 발사
            if (attackTimer >= boss.AttackCooldown)
            {
                boss.Animator.SetBool("IsAttack", false);

                // 산성 점액 발사
                if (boss.AcidSpawnPoint != null)
                {
                    FireAcidBullets(boss.AcidSpawnPoint, 10, 120f, 10f); // 5발, 120도, 속도 5
                }

                attackTimer = 0f; // 쿨타임 초기화
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
            float startAngle = 150f; // 10시 방향
            float angleStep = spreadAngle / (bulletCount - 1);

            for (int i = 0; i < bulletCount; i++)
            {
                float angle = startAngle - i * angleStep;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

                // WeaponManager처럼 방향에 따라 x축 오프셋 적용
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