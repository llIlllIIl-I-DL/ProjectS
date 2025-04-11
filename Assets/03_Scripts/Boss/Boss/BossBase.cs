using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossBase : MonoBehaviour
{
    [Header("보스 기본 스탯")]
    [SerializeField] protected float maxHp; //최대 체력
    [SerializeField] protected float currentHp; //현재 체력
    [SerializeField] protected float moveSpeed; // 이동 속도
    [SerializeField] protected float attackPower; // 공격력
    [SerializeField] protected float attackRange; // 공격 범위

    [Header("충돌 피격 데미지")]
    [SerializeField] protected float contactDamage = 1f; // 충돌 데미지 값
    [SerializeField] protected bool dealsDamageOnContact = true; // 충돌 데미지 적용 여부

    // 상태 관련
    protected bool isDead = false; // 사망 여부
    protected bool isGroggy = false; // 그로기 여부
    protected Transform playerTransform;
    protected Rigidbody2D rb;
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;

    // 플레이어 감지
    protected bool playerDetected = false;
    protected Vector2 lastKnownPlayerPosition; // 마지막으로 감지된 플레이어 위치

}
