using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Enemy.States;

/// <summary>
/// 고정형 터렛 - 플레이어 감지 시 발사
/// </summary>
public class EnemySupuerDust : BaseEnemy
{
    #region Variables
    
    [Header("터렛 설정")]
    [SerializeField] private Transform firePoint;         // 발사 지점
    [SerializeField] private GameObject bulletPrefab;     // 총알 프리팹
    [SerializeField] private float fireRate;              // 발사 속도 (초당)
    [SerializeField] private float bulletSpeed;           // 총알 속도
    [SerializeField] private float maxRotationAngle;      // 최대 회전 각도 (제한된 각도만 회전하고 싶을 경우)
    [SerializeField] private bool rotateToPlayer = true;  // 플레이어 방향으로 회전 여부
    
    private AttackState attackState;
    
    // 참조 및 속성
    private Vector2 startPosition;
    private Quaternion initialRotation;
    private Vector3 fixedPosition; // 위치를 강제로 고정하기 위한 변수
    
    #endregion

    #region Properties
    
    // 현재 상태 가져오기 (상태 전환 로직용)
    public IEnemyState currentState => stateMachine.CurrentState;
    
    #endregion

    #region Unity Lifecycle Methods
    
    /// <summary>
    /// 컴포넌트 초기화 및 시작 위치 저장
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        startPosition = transform.position;
        initialRotation = transform.rotation;
        
        // 발사 지점이 할당되지 않았으면 자기 자신으로 설정
        if (firePoint == null)
            firePoint = transform;
    }
    
    /// <summary>
    /// 시작 시 위치 고정값 설정
    /// </summary>
    protected override void Start()
    {
        base.Start();
        fixedPosition = transform.position;
    }
    
    /// <summary>
    /// 프레임 기반 업데이트
    /// </summary>
    protected override void Update()
    {
        base.Update(); // BaseEnemy의 Update 호출
    }

    /// <summary>
    /// 물리 기반 업데이트 - 위치 고정
    /// </summary>
    protected override void FixedUpdate()
    {
        base.FixedUpdate(); // BaseEnemy의 FixedUpdate 호출
        
        // 위치 강제 고정
        transform.position = fixedPosition;
    }
    
    #endregion

    #region Core Methods
    
    /// <summary>
    /// 적 초기화 및 상태 설정
    /// </summary>
    protected override void InitializeEnemy()
    {
        // 상태 생성
        RegisterState(new IdleState(this, stateMachine));
        RegisterState(new AttackState(this, stateMachine, fireRate));
        RegisterState(new PatrolState(this, stateMachine, new Vector2[] { startPosition }, 0f)); // 고정 위치
        
        // 초기 상태 설정
        SwitchToState<IdleState>();
    }
    
    /// <summary>
    /// 상태 머신 업데이트 및 플레이어 감지에 따른 상태 전환
    /// </summary>
    protected override void UpdateAI()
    {
        stateMachine.Update();
        
        // 플레이어 감지 시 상태 전환
        if (playerDetected && currentState != attackState)
        {
            SwitchToState<AttackState>();
        }
        else if (!playerDetected && currentState == attackState)
        {
            SwitchToState<IdleState>();
        }
        
        // 플레이어를 향해 회전 (제한된 각도 내에서)
        if (rotateToPlayer && playerDetected)
        {
            RotateTowardsPlayer();
        }
    }
    
    /// <summary>
    /// 이동 로직은 터렛에서 필요 없음 (고정 위치)
    /// </summary>
    protected override void HandleMovement()
    {
        // 터렛은 이동하지 않음
        if (rb != null)
        {
            rb.velocity = Vector2.zero; // 속도 강제로 0으로 설정
        }
    }
    
    #endregion

    #region Combat Methods
    
    /// <summary>
    /// 플레이어를 향해 회전 (최대 회전 각도 제한 적용)
    /// </summary>
    private void RotateTowardsPlayer()
    {
        if (playerTransform == null) return;
        
        // 플레이어 방향 구하기
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 초기 각도에서 최대 회전 각도 제한 적용
        float initialAngle = initialRotation.eulerAngles.z;
        float clampedAngle = targetAngle;
        
        if (maxRotationAngle > 0)
        {
            // 최소/최대 허용 각도 계산
            float minAllowedAngle = initialAngle - maxRotationAngle;
            float maxAllowedAngle = initialAngle + maxRotationAngle;
            
            // 각도 제한 적용
            clampedAngle = Mathf.Clamp(targetAngle, minAllowedAngle, maxAllowedAngle);
        }
        
        // 회전 적용
        transform.rotation = Quaternion.Euler(0, 0, clampedAngle);
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
        
        // 총알에 속도와 데미지 적용
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();
        
        if (bulletRb != null && enemyBullet != null)
        {
            bulletRb.velocity = fireDirection * bulletSpeed;
            enemyBullet.SetDamage(attackPower); // attackPower를 총알 데미지로 설정
        }
        else
        {
            Debug.LogWarning("총알에 필요한 컴포넌트가 없습니다!");
            return;
        }
        
        // 총알 수명 (메모리 관리)
        Destroy(bullet, 5f);
        
        // 디버깅
        Debug.DrawRay(firePoint.position, fireDirection * 3f, Color.red, 0.5f);
        Debug.Log($"총알 발사 방향: {fireDirection}, 각도: {angle}도, 데미지: {attackPower}");
    }
    
    #endregion
}