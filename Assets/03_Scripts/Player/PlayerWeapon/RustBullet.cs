using UnityEngine;

// 녹(산성) 총알
public class RustBullet : Bullet
{
    [SerializeField] private float rustDebuffDuration = 2f;     // 디버프 지속 시간
    [SerializeField] private float speedReductionPercent = 0.3f; // 속도 감소 비율 (%)
    [SerializeField] private float damageOverTimeAmount = 2f;   // 시간당 추가 데미지
    [SerializeField] private GameObject acidEffectPrefab;       // 산성 효과 VFX 프리팹
    [SerializeField] private float swayAmount = 1.5f;    // 좌우 흔들림 폭
    [SerializeField] private float swaySpeed = 2f;       // 흔들림 속도

    private Vector3 originalPosition; // 원래 진행 방향의 위치
    private Vector3 lastFramePosition; // 마지막 프레임의 위치
    private float elapsedTime = 0f;
    private Vector3 swayOffset = Vector3.zero; // 흔들림 오프셋

    private void OnEnable()
    {
        originalPosition = transform.position;
        lastFramePosition = transform.position;
        elapsedTime = 0f;
        swayOffset = Vector3.zero;
        IsOvercharged = false;
    }

    protected override void Start()
    {
        BulletType = ElementType.Rust;
        base.Start();
        originalPosition = transform.position;
        lastFramePosition = transform.position;
    }

    protected override void Update()
    {
        // 기본 총알 이동 로직 실행
        base.Update();

        // 기본 이동 후의 위치 계산 (흔들림 제외)
        Vector3 baseMovement = transform.position - lastFramePosition;
        originalPosition += baseMovement;
        
        // 오버차지 상태가 아닐 때만 흔들림 적용
        if (!IsOvercharged)
        {
            elapsedTime += Time.deltaTime;
            // 좌우로 흔들리는 오프셋 계산
            float sway = Mathf.Sin(elapsedTime * swaySpeed) * swayAmount;
            swayOffset = new Vector3(0, sway, 0) * Time.deltaTime;
            // 흔들림 오프셋 적용
            transform.position = originalPosition + swayOffset;
        }
        else
        {
            // 오버차지 상태에서는 원래 경로로 설정
            transform.position = originalPosition;
            swayOffset = Vector3.zero;
        }

        // 마지막 프레임 위치 업데이트
        lastFramePosition = transform.position;
    }

    protected override void ApplySpecialEffect(IDebuffable target)
    {
        DebuffManager.Instance.ApplyDebuff(
            target,
            DebuffType.Rust,
            rustDebuffDuration,         // duration
            speedReductionPercent,      // intensity (예: 0.3f = 30% 감소)
            damageOverTimeAmount        // tickDamage (초당 데미지)
        );
    }
}
