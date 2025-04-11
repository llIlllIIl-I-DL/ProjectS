using UnityEngine;

// 보스의 상태를 정의하는 열거형
public enum BossState
{
    Idle,       // 대기 상태
    Attack,     // 공격 상태
    HitDelay,   // 피격 후 딜레이 상태
    Groggy,     // 그로기 상태 (피격 가능)
    Dead        // 사망 상태
}

public class Boss : MonoBehaviour
{
    public BossState currentState = BossState.Idle; // 현재 상태

    [Header("보스 기본 스탯")]
    public float maxHp; //최대 체력
    public float currentHp; //현재 체력
    public bool isInvincible = true; // 무적 여부

    [Header("Hit Delay")]
    public float hitDelayDuration; // 피격 후 무적 시간
    private float hitDelayTimer; // 피격 딜레이 타이머

    [Header("그로기 상태")]
    public int groggyThreshold; // 그로기 전까지 맞아야 할 횟수
    public float groggyDuration; // 그로기 지속 시간
    private float groggyTimer; // 그로기 타이머
    private int hitCount; // 누적 피격 횟수

    [Header("컴포넌트")]
    private Animator animator; // 애니메이터 컴포넌트
    //public GameObject hpUI; // 보스 HP UI

    void Start()
    {
        currentHp = maxHp;
        animator = GetComponent<Animator>();
        //hpUI.SetActive(false); // 처음엔 HP UI 숨김
    }

    void Update()
    {
        switch (currentState)
        {
            case BossState.Idle:
                HandleIdle();
                break;
            case BossState.Attack:
                HandleAttack();
                break;
            case BossState.HitDelay:
                HandleHitDelay();
                break;
            case BossState.Groggy:
                HandleGroggy();
                break;
            case BossState.Dead:
                HandleDead();
                break;
        }
    }

    void HandleIdle()
    {
        isInvincible = true; // 무적 상태
        animator.Play("Boss_Idle");

        // 플레이어가 범위에 들어오면 공격 상태로 전환
        if (PlayerInRange())
        {
            currentState = BossState.Attack;
        }
    }

    void HandleAttack()
    {
        isInvincible = true; // 공격 중 무적
        animator.Play("Boss_Attack");

        // 공격 도중 피격 시 피격 상태로 전이
        if (WasHit())
        {
            EnterHitDelay();
        }
    }

    void HandleHitDelay()
    {
        animator.Play("Hit");
        hitDelayTimer -= Time.deltaTime;

        if (hitDelayTimer <= 0)
        {
            // 누적 피격 수가 기준 이상이면 그로기 진입
            if (hitCount >= groggyThreshold)
            {
                EnterGroggy();
            }
            else
            {
                currentState = BossState.Idle; // 다시 대기 상태로
            }
        }
    }

    void HandleGroggy()
    {
        isInvincible = false; // 그로기 중엔 무적 해제
        animator.Play("Groggy");

        groggyTimer -= Time.deltaTime;
        if (groggyTimer <= 0)
        {
            hitCount = 0; // 피격 카운트 초기화
            currentState = BossState.Idle; // 회복 후 Idle
        }
    }

    void HandleDead()
    {
        isInvincible = true;
        animator.Play("Boss_Dead");
        // 드롭, 제거 로직 추가 가능
    }

    // 외부에서 데미지를 입히는 함수
    public void TakeDamage(float amount)
    {
        if (isInvincible) return;

        currentHp -= amount;
        hitCount++;

        if (currentHp <= 0)
        {
            currentHp = 0;
            currentState = BossState.Dead;
            return;
        }

        EnterHitDelay();
    }

    void EnterHitDelay()
    {
        currentState = BossState.HitDelay;
        hitDelayTimer = hitDelayDuration;
        isInvincible = true;
    }

    void EnterGroggy()
    {
        currentState = BossState.Groggy;
        groggyTimer = groggyDuration;
    }

    // 플레이어가 범위 안에 있는지 확인하는 임시 함수
    bool PlayerInRange()
    {
        return true;
    }

    // 피격 여부 체크하는 임시 함수
    bool WasHit()
    {
        return false;
    }
}
