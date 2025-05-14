using System.Collections;
using UnityEngine;

/// <summary>
/// 지속딜을 주는 장판 트랩 - 플레이어가 장판에 서있으면 지속적으로 데미지를 입힘
/// </summary>
public class FireTrap : BaseTrap
{
    [Header("지속딜 설정")]
    [SerializeField] private float damageInterval = 1f; // 데미지 주는 간격
    [SerializeField] private bool damageOnEnter = true; // 입장 시 즉시 데미지를 줄지 여부

    [Header("시각 효과")]
    [SerializeField] private GameObject fireEffect; // 불 효과 게임오브젝트
    [SerializeField] private float effectIntensity = 1f; // 효과 강도 조절

    private bool isPlayerInTrap = false; // 플레이어가 트랩에 있는지 여부
    private Coroutine damageCoroutine; // 데미지를 주는 코루틴
    private IDamageable playerDamageable; // 플레이어의 데미지 인터페이스 캐싱

    protected override void Initialize()
    {
        base.Initialize();

        // 초기 상태 설정
        if (fireEffect != null)
            fireEffect.SetActive(isActive);
    }

    public override void ActivateTrap()
    {
        if (!isActive)
        {
            isActive = true;

            // 시각 효과 활성화
            if (fireEffect != null)
                fireEffect.SetActive(true);

            // 활성화 사운드 재생
            if (activationSound != null)
                AudioSource.PlayClipAtPoint(activationSound, transform.position);
                // 지금 오디오매니저가 있긴하지만 구조 방식을 조금 바꿔야할지 고민중이라 일단 대기

            // 만약 플레이어가 이미 트랩 위에 있다면 데미지 코루틴 시작
            if (isPlayerInTrap && playerDamageable != null)
                StartDamageCoroutine();
        }
    }

    public override void DeactivateTrap()
    {
        if (isActive)
        {
            isActive = false;

            // 시각 효과 비활성화
            if (fireEffect != null)
                fireEffect.SetActive(false);

            // 데미지 코루틴 정지
            StopDamageCoroutine();
        }
    }

    public override void ToggleAutoTrap()
    {
        // 아침에 장판에 오토기능필요한지 여쭤보고 구현할지 말지 결정
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);

        if (triggeredByPlayer && other.CompareTag("Player"))
        {
            isPlayerInTrap = true;
            playerDamageable = other.GetComponent<IDamageable>();

            Debug.Log("플레이어가 장판 트랩에 진입했습니다.");

            // 활성화된 상태에서만 데미지 코루틴 시작
            if (isActive && playerDamageable != null)
            {
                // 입장 시 즉시 데미지
                if (damageOnEnter)
                    playerDamageable.TakeDamage((int)damage);

                StartDamageCoroutine();
            }
        }
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerExit2D(other);

        if (triggeredByPlayer && other.CompareTag("Player"))
        {
            isPlayerInTrap = false;
            playerDamageable = null;

            Debug.Log("플레이어가 장판 트랩에서 벗어났습니다.");

            // 데미지 코루틴 정지
            StopDamageCoroutine();
        }
    }

    private void StartDamageCoroutine()
    {
        // 안전성을 위해 꼭 필요한 처리 - Good
        // 이미 실행 중인 코루틴이 있다면 정지
        StopDamageCoroutine();

        // 새 코루틴 시작
        damageCoroutine = StartCoroutine(DealDamagePeriodically());
    }

    private void StopDamageCoroutine()
    {
        if (damageCoroutine != null)
        {
            StopCoroutine(damageCoroutine);
            damageCoroutine = null;
        }
    }

    private IEnumerator DealDamagePeriodically()
    {
        // 첫 데미지는 damageOnEnter에서 이미 처리됐을 수 있으므로 대기부터 시작
        yield return new WaitForSeconds(damageInterval);

        // 플레이어가 트랩에 있고 트랩이 활성화된 동안 반복
        while (isActive && isPlayerInTrap && playerDamageable != null)
        {
            // 데미지 적용
            playerDamageable.TakeDamage((int)damage);
            Debug.Log($"장판 트랩이 {damage} 데미지를 입혔습니다.");

            // 효과 재생
            if (activationEffect != null)
                activationEffect.Play();

            // 간격만큼 대기
            yield return new WaitForSeconds(damageInterval);
        }
    }

    protected override void OnInteract(GameObject interactor)
    {
        // 필요시 상호작용 로직 추가
    }
}