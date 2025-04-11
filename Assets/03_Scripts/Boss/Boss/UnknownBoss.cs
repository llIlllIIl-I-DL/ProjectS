using UnityEngine;

public class UnknownBoss : MonoBehaviour
{
    public BossBase Boss;

    [Header("보스 기본 스탯")]
    [SerializeField] protected float maxHp; //최대 체력
    [SerializeField] protected float currentHp; //현재 체력
    [SerializeField] protected float moveSpeed; // 이동 속도
    [SerializeField] protected float attackPower; // 공격력
    [SerializeField] protected float attackRange; // 공격 범위

    [Header("충돌 피격 데미지")]
    [SerializeField] protected float contactDamage; // 충돌 데미지 값

    // 상태 관련
    protected bool isDead = false; // 사망 여부
    protected bool isGroggy = false; // 그로기 여부
    protected Rigidbody2D rb;
    protected Transform transform;
    protected SpriteRenderer spriteRenderer;
    protected Animator animator;
}
