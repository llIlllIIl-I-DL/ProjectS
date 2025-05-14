using System;
using System.Collections.Generic;
using UnityEngine;
using BossFSM;
using System.Linq;



[System.Serializable] // 태그 선택기 클래스 커스텀에디터를 위한 클래스
public class TagSelector
{
    [Tag]
    public string tag;
}



// 총알 추상 클래스
public abstract class Bullet : MonoBehaviour
{
    [SerializeField] private float bulletSpeed = 4f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private int currentAmmo;
    [SerializeField] private ElementType bulletType = ElementType.Normal;
    [SerializeField] private List<TagSelector> targetTags = new List<TagSelector>();
    public List<TagSelector> TargetTags => targetTags;
    
    [Header("오버차지 설정")]
    [SerializeField] private bool isOvercharged = false;  // 과열 상태 여부
    [SerializeField] private bool isReloading = false;

    private float playerIgnoreTime = 0.1f;
    private float playerIgnoreTimer = 0f;
    private bool canHitPlayer = true;// 피격 가능여부 체크
    private bool ignoreCollisionReset = false; // 중복 해제 방지
    public GameObject Shooter { get; set; }
    // 축약가능하면 줄이기
    public bool IsReloading
    {
        get => isReloading;
        set => isReloading = value;
    }

    public float BulletSpeed
    {
        get => bulletSpeed;
        set => bulletSpeed = value;
    }
    public float Damage
    {
        get => damage;
        set => damage = value;
    }
    public float KnockbackForce
    {
        get => knockbackForce;
        set => knockbackForce = value;
    }
    public ElementType BulletType
    {
        get => bulletType;
        set => bulletType = value;
    }
    public bool IsOvercharged
    {
        get => isOvercharged;
        set => isOvercharged = value;
    }
    public int MaxAmmo
    {
        get => maxAmmo;
        set => maxAmmo = value;
    }
    public int CurrentAmmo
    {
        get => currentAmmo;
        set => currentAmmo = value;
    }

    protected bool hasHitEnemy = false;
    protected GameObject playerObject; // 플레이어 게임오브젝트 참조

    private Dictionary<string, Action<Collider2D>> collisionHandlers;

    // 총알 특수 효과 추상 메서드
    protected abstract void ApplySpecialEffect(BaseEnemy enemy);

    protected virtual void Awake()
    {
        // 태그별 충돌 처리 등록
        collisionHandlers = new Dictionary<string, Action<Collider2D>>();

        foreach (var tagSelector in targetTags) // 충돌 가능 목록
        {
            switch (tagSelector.tag)
            {
                case "Mirror":
                    collisionHandlers["Mirror"] = HandleMirrorCollision;
                    break;
                case "Destructible":
                    collisionHandlers["Destructible"] = HandleDestructibleCollision;
                    break;
                case "Boss":
                    collisionHandlers["Boss"] = HandleBossCollision;
                    break;
                case "Enemy":
                    collisionHandlers["Enemy"] = HandleEnemyCollision;
                    break;
                case "Player":
                    collisionHandlers["Player"] = HandlePlayerCollision;
                    break;
                // 필요하다면 추가
            }
        }
    }

    protected virtual void Start()
    {
        playerObject = GameObject.FindGameObjectWithTag("Player");

        playerIgnoreTimer = 0f;
        canHitPlayer = false;
    }
    
    // 업데이트 가상 메서드 추가
    protected virtual void Update()
    {
        //
    }
    // 총알 상태 초기화 (풀에서 가져올 때 호출)
    public void ResetBullet()
    {
        // 필요한 초기화 작업
        // 예: 충돌 카운터 리셋, 효과 초기화 등
    }
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {

        // targetTags에 포함된 태그만 충돌 처리
        string otherTag = other.tag;
        bool isTargetTag = false;
        foreach (var t in targetTags)
        {
            if (t.tag == otherTag)
            {
                isTargetTag = true;
                break;
            }
        }

        if (isTargetTag)
        {
            // targetTags에 포함된 태그만 딕셔너리에서 처리
            if (collisionHandlers != null && collisionHandlers.TryGetValue(otherTag, out var handler))
            {
                handler(other);
            }
            else
            {
                // targetTags에 포함되어 있지만 별도 핸들러가 없는 경우(예: 벽 등)
                if (((1 << other.gameObject.layer) & (LayerMask.GetMask("Ground", "Wall"))) != 0)
                {
                    Debug.Log("벽과 충돌하여 총알 제거됨: " + other.gameObject.name);
                    ObjectPoolingManager.Instance.ReturnBullet(gameObject, BulletType);
                }
                else
                {
                    Debug.Log("타겟 태그이지만 별도 핸들러 없음: " + other.gameObject.name + ", 태그: " + other.tag + ", 레이어: " + LayerMask.LayerToName(other.gameObject.layer));
                }
            }
        }
        // targetTags에 없는 태그는 무시

        if (isOvercharged)
        {
            ApplyOverchargeRecoil();
        }
    }
    
    // 총알이 파괴될 때 호출되는 함수
    protected virtual void OnDestroy()
    {
        // 파생 클래스에서 오버라이드 가능
    }

    // 충돌 지점 계산 메서드 추가
    private Vector2 CalculateHitPoint(Collider2D otherCollider)// 충돌 지점 계산
    {
        // 총알의 진행 방향 계산
        Vector2 direction = GetComponent<Rigidbody2D>().velocity.normalized;

        // 총알 콜라이더 정보 가져오기
        Collider2D bulletCollider = GetComponent<Collider2D>();
        float bulletRadius = 0f;

        if (bulletCollider is CircleCollider2D)
        {
            bulletRadius = ((CircleCollider2D)bulletCollider).radius * transform.localScale.x;
        }
        else if (bulletCollider is BoxCollider2D)
        {
            BoxCollider2D boxCollider = (BoxCollider2D)bulletCollider;
            bulletRadius = Mathf.Max(boxCollider.size.x, boxCollider.size.y) * 0.5f * transform.localScale.x;
        }

        // 총알의 최전방 위치 계산
        Vector2 bulletFrontPoint = (Vector2)transform.position + direction * bulletRadius;

        // Raycast로 정확한 충돌 지점 찾기
        RaycastHit2D hit = Physics2D.Raycast(
            (Vector2)transform.position - direction * bulletRadius,  // 총알 뒤쪽에서 시작
            direction,                                              // 총알 진행 방향
            bulletRadius * 2 + 0.5f,                               // 충분한 거리
            1 << otherCollider.gameObject.layer                    // 충돌 레이어
        );

        if (hit.collider != null && hit.collider.gameObject == otherCollider.gameObject)
        {
            return hit.point; // Raycast 충돌 지점 반환
        }

        // Raycast 실패 시 대략적인 충돌 지점 반환
        return bulletFrontPoint;
    }

    // 각 태그별 처리 함수
    private void HandleMirrorCollision(Collider2D other)
    {
        ObjectMirror mirror = other.GetComponent<ObjectMirror>();
        if (mirror != null)
        {
            mirror.TakeDamage(damage);
            ObjectPoolingManager.Instance.ReturnBullet(gameObject, BulletType);
        }
    }

    private void HandleDestructibleCollision(Collider2D other) // 부서지는 물체 충돌 처리
    {
        DestructibleObject destructible = other.GetComponent<DestructibleObject>();
        if (destructible != null)
        {
            destructible.TakeDamage(damage);
            ObjectPoolingManager.Instance.ReturnBullet(gameObject, BulletType);
        }
    }

    private void HandleBossCollision(Collider2D other) // 보스 충돌 처리
    {
        BossHealth boss = other.GetComponent<BossHealth>();
        Boss boss2 = other.GetComponent<Boss>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
            ObjectPoolingManager.Instance.ReturnBullet(gameObject, BulletType);
        }
        if (boss2 != null)
        {
            boss2.TakeDamage(damage);
            ObjectPoolingManager.Instance.ReturnBullet(gameObject, BulletType);
        }
    }

    private void HandleEnemyCollision(Collider2D other) // 에너미 충돌 처리
    {
        BaseEnemy enemy = other.GetComponent<BaseEnemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            hasHitEnemy = true;

            Vector2 hitPoint = CalculateHitPoint(other);
            ApplySpecialEffect(enemy);
            Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
            enemy.ApplyKnockback(knockbackDirection, knockbackForce);
            enemy.PlayHitEffect(hitPoint);
        }
        ObjectPoolingManager.Instance.ReturnBullet(gameObject, BulletType);
    }

    private void HandlePlayerCollision(Collider2D other)
    {
        PlayerHP playerHP = other.GetComponent<PlayerHP>();
        if (playerHP != null)
        {
            playerHP.TakeDamage(damage);
            Debug.Log($"플레이어가 총알에 맞아 {damage} 데미지를 입었습니다!");
        }
        ObjectPoolingManager.Instance.ReturnBullet(gameObject, BulletType);
    }

    private void ApplyOverchargeRecoil()// 기존 오버차징 로직 메서드로 분리
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerHP playerHP = player.GetComponent<PlayerHP>();
            if (playerHP != null)
            {
                float selfDamage = playerHP.MaxHP * 0.05f;
                playerHP.TakeDamage(selfDamage);
                Debug.Log($"과열 공격 반동으로 플레이어가 {selfDamage} 데미지를 입었습니다!");
            }
        }
    }
}

