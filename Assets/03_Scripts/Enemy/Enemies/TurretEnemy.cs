using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Enemy.States;

/// <summary>
/// 고정형 터렛 - 플레이어 감지 시 발사
/// </summary>
public class TurretEnemy : BaseEnemy
{
    [Header("터렛 설정")]
    [SerializeField] private Transform firePoint;         // 발사 지점
    [SerializeField] private GameObject bulletPrefab;     // 총알 프리팹
    [SerializeField] private float fireRate;       // 발사 속도 (초당)
    [SerializeField] private float bulletSpeed;      // 총알 속도
    [SerializeField] private float maxRotationAngle; // 최대 회전 각도 (제한된 각도만 회전하고 싶을 경우)
    [SerializeField] private bool rotateToPlayer = true;  // 플레이어 방향으로 회전 여부
    
    // 상태
    private IdleState idleState;
    private AttackState attackState;
    
    private Vector2 startPosition;
    private Quaternion initialRotation;

    // 위치를 강제로 고정하기 위한 변수
    private Vector3 fixedPosition;

    protected override void Awake()
    {
        base.Awake();
        startPosition = transform.position;
        initialRotation = transform.rotation;
        
        // 발사 지점이 할당되지 않았으면 자기 자신으로 설정
        if (firePoint == null)
            firePoint = transform;
    }
    
    protected override void Start()
    {
        base.Start();
        fixedPosition = transform.position;
    }
    
    protected override void Update()
    {
        base.Update(); // BaseEnemy의 Update 호출
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // BaseEnemy의 FixedUpdate 호출
        
        // 위치 강제 고정
        transform.position = fixedPosition;
    }

    /// <summary>
    /// 적 초기화 및 상태 설정
    /// </summary>
    protected override void InitializeEnemy()
    {
        // 상태 생성
        idleState = new IdleState(this, stateMachine);
        attackState = new AttackState(this, stateMachine, fireRate);
        
        // 초기 상태 설정
        stateMachine.ChangeState(idleState);
    }
    
    /// <summary>
    /// 상태 머신 업데이트
    /// </summary>
    protected override void UpdateAI()
    {
        stateMachine.Update();
        
        // 플레이어 감지 시 상태 전환
        if (playerDetected && currentState != attackState)
        {
            stateMachine.ChangeState(attackState);
        }
        else if (!playerDetected && currentState == attackState)
        {
            stateMachine.ChangeState(idleState);
        }
        
        // 플레이어를 향해 회전 (제한된 각도 내에서)
        if (rotateToPlayer && playerDetected)
        {
            RotateTowardsPlayer();
        }
    }
    
    /// <summary>
    /// 이동 로직은 터렛에서 필요 없음
    /// </summary>
    protected override void HandleMovement()
    {
        // 터렛은 이동하지 않음
        if (rb != null)
        {
            rb.velocity = Vector2.zero; // 속도 강제로 0으로 설정
        }
    }
    
    /// <summary>
    /// 플레이어를 향해 회전
    /// </summary>
    private void RotateTowardsPlayer()
    {

    }
    
    /// <summary>
    /// 공격 실행 - 총알 발사
    /// </summary>
    public override void PerformAttack()
    {
        // 총알 프리팹이 없으면 반환
        if (bulletPrefab == null) 
        {
            Debug.LogWarning("총알 프리팹이 할당되지 않았습니다!");
            return;
        }
        
        // 발사 방향 계산 - 플레이어 방향으로
        Vector2 fireDirection;
        
        if (playerTransform != null && playerDetected)
        {
            // 플레이어 방향 계산
            fireDirection = (playerTransform.position - firePoint.position).normalized;
        }
        else
        {
            // 플레이어가 없거나 감지되지 않은 경우, 기본 방향으로 발사
            fireDirection = firePoint.right;
        }
        
        // 총알 생성 - 방향에 맞는 회전값 적용
        float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;
        Quaternion bulletRotation = Quaternion.Euler(0, 0, angle);
        
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, bulletRotation);
        
        // 총알에 속도 적용
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.velocity = fireDirection * bulletSpeed; // 실제 플레이어 방향으로 발사
        }
        else
        {
            Debug.LogWarning("총알에 Rigidbody2D가 없습니다!");
            return;
        }
        
        // 총알 수명 (메모리 관리)
        Destroy(bullet, 5f);
        
        // 디버깅
        Debug.DrawRay(firePoint.position, fireDirection * 3f, Color.red, 0.5f);
        Debug.Log($"총알 발사 방향: {fireDirection}, 각도: {angle}도");
    }
    
    // 현재 상태 가져오기 (상태 전환 로직용)
    public IEnemyState currentState => stateMachine.CurrentState;
    
    // 상태 접근자 메서드들
    public IdleState GetIdleState() => idleState;
    public AttackState GetAttackState() => attackState;
}