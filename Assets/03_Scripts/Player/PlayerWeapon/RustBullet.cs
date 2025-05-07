using UnityEngine;

// 녹(산성) 총알
public class RustBullet : Bullet
{
    [SerializeField] private float rustDebuffDuration = 5f;     // 디버프 지속 시간
    [SerializeField] private float speedReductionPercent = 30f; // 속도 감소 비율 (%)
    [SerializeField] private float damageOverTimeAmount = 2f;   // 시간당 추가 데미지
    [SerializeField] private GameObject acidEffectPrefab;       // 산성 효과 VFX 프리팹
    [SerializeField] private float floatSpeed = 2f;      // 위로 떠오르는 속도
    [SerializeField] private float swayAmount = 0.5f;    // 좌우 흔들림 폭
    [SerializeField] private float swaySpeed = 2f;       // 흔들림 속도

    private Vector3 startPosition;
    private float elapsedTime = 0f;


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
        transform.position += new Vector3(0, sway * Time.deltaTime, 0);
    }

    protected override void ApplySpecialEffect(BaseEnemy enemy)
    {
        // 디버프가 이미 적용되어 있는지 확인
        DebuffManager existingDebuff = enemy.GetComponent<DebuffManager>();

        if (existingDebuff != null)
        {
            // 이미 디버프가 있다면 지속시간 리셋
            existingDebuff.RefreshDuration(rustDebuffDuration);
        }
        else
        {
            // 디버프 효과 추가
            DebuffManager debuff = enemy.gameObject.AddComponent<DebuffManager>();
            debuff.Initialize(rustDebuffDuration, speedReductionPercent, damageOverTimeAmount);

            // VFX 효과 표시 (있다면)
            if (acidEffectPrefab != null)
            {
                GameObject effect = Instantiate(acidEffectPrefab, enemy.transform);
                effect.transform.localPosition = Vector3.zero;
                // VFX는 디버프와 함께 자동 소멸
                Destroy(effect, rustDebuffDuration);
            }
        }
    }
}
