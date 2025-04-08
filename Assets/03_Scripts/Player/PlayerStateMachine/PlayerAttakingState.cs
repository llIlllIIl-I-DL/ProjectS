using UnityEngine;

public class PlayerAttackState : PlayerStateBase
{
    private float attackStartTime;
    private float attackDuration = 0.25f; // 공격 모션 지속 시간
    private float attackCooldown = 0.2f;  // 공격 쿨다운
    private bool canAttackAgain = true;

    private Vector2 lastAimDirection;

    public PlayerAttackState(PlayerStateManager stateManager) : base(stateManager)
    {
    }

    public override void Enter()
    {
        attackStartTime = Time.time;

        // 플레이어가 바라보는 방향 저장
        lastAimDirection = new Vector2(player.GetMovement().FacingDirection, 0).normalized;

        // 총알 발사
        FireWeapon();

        Debug.Log("공격 상태 시작: 총 발사");
    }

    public override void Update()
    {
        // 공격 모션 종료 체크
        if (Time.time >= attackStartTime + attackDuration)
        {
            // 공격 끝난 후 상태 전환
            var collisionDetector = player.GetCollisionDetector();
            var inputHandler = player.GetInputHandler();

            if (!collisionDetector.IsGrounded)
            {
                player.ChangeState(PlayerStateType.Falling);
            }
            else if (inputHandler.IsMoving())
            {
                player.ChangeState(PlayerStateType.Running);
            }
            else
            {
                player.ChangeState(PlayerStateType.Idle);
            }
        }
    }

    public override void FixedUpdate()
    {
        // 공격 중에도 이동은 가능하게 처리
        var inputHandler = player.GetInputHandler();
        var movement = player.GetMovement();

        // 공격 중 속도 감소 (선택적)
        movement.Move(inputHandler.MoveDirection * 0.5f);
    }

    private void FireWeapon()
    {
        // 총알 생성 및 발사
        var weaponManager = player.GetComponent<WeaponManager>();
        if (weaponManager != null)
        {
            weaponManager.FireWeapon(lastAimDirection);
        }
        else
        {
            // 임시 총알 생성 로직 (WeaponManager가 없을 경우)
            CreateBullet(lastAimDirection);
        }

        // 공격 쿨다운 시작
        player.StartCoroutine(AttackCooldown());
    }

    private System.Collections.IEnumerator AttackCooldown()
    {
        canAttackAgain = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttackAgain = true;
    }

    // 임시 총알 생성 메서드
    private void CreateBullet(Vector2 direction)
    {
        // 총알 프리팹 (추후 WeaponManager로 이동할 코드)
        GameObject bulletPrefab = Resources.Load<GameObject>("Prefabs/Bullet");
        if (bulletPrefab == null)
        {
            Debug.LogError("총알 프리팹을 찾을 수 없습니다. Resources/Prefabs/Bullet 경로 확인 필요");
            return;
        }

        // 총구 위치 (플레이어 앞쪽)
        Vector3 spawnPosition = player.transform.position + new Vector3(direction.x * 0.5f, 0.1f, 0);

        // 총알 생성
        GameObject bullet = GameObject.Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

        // 총알 속도 및 방향 설정
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            float bulletSpeed = 15f;
            bulletRb.velocity = direction * bulletSpeed;
        }

        // 3초 후 총알 자동 제거
        GameObject.Destroy(bullet, 3f);
    }

    public override void Exit()
    {
        Debug.Log("공격 상태 종료");
    }

    // 연속 공격 가능 여부 체크
    public bool CanAttack()
    {
        return canAttackAgain;
    }
}