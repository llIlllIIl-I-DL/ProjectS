using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BossState { Idle, Attack, Groggy, Dead }

public class Boss : MonoBehaviour
{
    public BossState currentState = BossState.Idle;
    public float hp = 100;
    public bool isInvincible = true;

    private float groggyGauge = 0;
    private float groggyThreshold = 100;
    private float groggyTimer;

    void Update() //상태 업데이트
    {
        switch (currentState)
        {
            case BossState.Idle:
                IdleState();
                break;
            case BossState.Attack:
                AttackState();
                break;
            case BossState.Groggy:
                GroggyState();
                break;
            case BossState.Dead:
                DeadState();
                break;
        }
    }

    void IdleState()
    {
        // 대기 상태 (피격 불가)
        isInvincible = true;

        // 조건에 따라 공격 상태 전환
        if (PlayerInRange())
        {
            currentState = BossState.Attack;
        }
    }

    void AttackState()
    {
        // 공격 수행
        isInvincible = true;

        // 일정 시간 후 혹은 누적 피해량 등으로 그로기 전환
        if (ShouldGroggy())
        {
            currentState = BossState.Groggy;
        }
    }

    void GroggyState()
    {
        // 피격 가능 상태
        isInvincible = false;

        // 일정 시간 후 다시 Idle로
        if (GroggyTimeOver())
        {
            currentState = BossState.Idle;
        }
    }

    void DeadState()
    {
        // 사망 처리
        isInvincible = true;
        // 애니메이션, 드롭 등
    }

    public void TakeDamage(float amount)
    {
        if (isInvincible) return;

        hp -= amount;
        if (hp <= 0)
            currentState = BossState.Dead;
    }

    public void ApplyStagger(float amount)
    {
        groggyGauge += amount;
        if (groggyGauge >= groggyThreshold) EnterGroggyState();
    }

    void EnterGroggyState()
    {
        currentState = BossState.Groggy;
        isInvincible = false;
        groggyTimer = groggyDuration;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            bossUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            bossUI.SetActive(false);
        }
    }



    bool PlayerInRange() => true; // 임시 조건
    bool ShouldGroggy() => false;
    bool GroggyTimeOver() => false;
}

