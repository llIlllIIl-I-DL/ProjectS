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

    private Vector3 startPosition;
    private float elapsedTime = 0f;

    private void OnEnable()
    {
        startPosition = transform.position;
        elapsedTime = 0f;
        IsOvercharged = false; // 보스는 항상 흔들리게
    }
    protected override void Start()
    {
        BulletType = ElementType.Rust;
        base.Start();
        startPosition = transform.position;
    }
    protected override void Update()
    {
        base.Update();
        elapsedTime += Time.deltaTime;
        // 좌우로 흔들리기
        float sway = Mathf.Sin(elapsedTime * swaySpeed) * swayAmount;
        // 현재 위치에서 y값만 흔들리게
        // 차지된 상태면 흔들리지 않음
        if (IsOvercharged)
        {
            sway = 0f;
        }
        transform.position += new Vector3(0, sway * Time.deltaTime, 0);
    }

    protected override void ApplySpecialEffect(BaseEnemy enemy)
    {
        // rustDebuffDuration, speedReductionPercent, damageOverTimeAmount는 RustBullet의 변수라고 가정
        DebuffManager.Instance.ApplyDebuff(
            enemy,
            DebuffType.Rust,
            rustDebuffDuration,         // duration
            speedReductionPercent,      // intensity (예: 0.3f = 30% 감소)
            damageOverTimeAmount        // tickDamage (초당 데미지)
        );
    }
}
