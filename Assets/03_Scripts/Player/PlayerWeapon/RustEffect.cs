using UnityEngine;

public class RustEffect : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private ParticleSystem acidParticles;
    [SerializeField] private float effectDuration = 5f;
    [SerializeField] private Color acidColor = new Color(0.7f, 1f, 0.3f, 0.8f);
    [SerializeField] private float bubbleFrequency = 0.5f;

    [Header("Acid Drip Settings")]
    [SerializeField] private GameObject acidDripPrefab;
    [SerializeField] private float dripFrequency = 1f;

    [Header("Sound")]
    [SerializeField] private AudioClip acidSizzleSound;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.2f;

    private ParticleSystem.MainModule particleMain;
    private AudioSource audioSource;
    private float bubbleTimer = 0f;
    private float dripTimer = 0f;
    private float elapsedTime = 0f;

    private void Awake()
    {
        // 파티클 시스템이 없으면 생성
        if (acidParticles == null)
        {
            acidParticles = GetComponentInChildren<ParticleSystem>();
            if (acidParticles == null)
            {
                acidParticles = gameObject.AddComponent<ParticleSystem>();
                SetupParticleSystem();
            }
        }

        particleMain = acidParticles.main;
        particleMain.startColor = acidColor;

        // 오디오 소스 설정
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && acidSizzleSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = acidSizzleSound;
            audioSource.loop = true;
            audioSource.volume = 0.5f;
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.spatialBlend = 1f; // 3D 사운드
            audioSource.Play();
        }
    }

    private void SetupParticleSystem()
    {
        // 파티클 시스템 기본 설정
        var main = acidParticles.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = 2.0f;
        main.startSpeed = 0.2f;
        main.startSize = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // 파티클 모양 설정
        var shape = acidParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        shape.radiusThickness = 0.1f;

        // 파티클 색상 설정
        var colorOverLifetime = acidParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(acidColor, 0.0f),
                new GradientColorKey(new Color(acidColor.r, acidColor.g, acidColor.b, 0.5f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorOverLifetime.color = gradient;

        // 파티클 크기 설정
        var sizeOverLifetime = acidParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;

        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.0f);
        curve.AddKey(0.2f, 1.0f);
        curve.AddKey(0.8f, 0.8f);
        curve.AddKey(1.0f, 0.0f);

        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        // 파티클 텍스처 시트 애니메이션 설정
        var textureSheetAnimation = acidParticles.textureSheetAnimation;
        textureSheetAnimation.enabled = true;
        textureSheetAnimation.numTilesX = 4;
        textureSheetAnimation.numTilesY = 4;

        // 시작
        acidParticles.Play();
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        // 산성 거품 효과 생성
        bubbleTimer += Time.deltaTime;
        if (bubbleTimer >= bubbleFrequency)
        {
            CreateAcidBubble();
            bubbleTimer = 0f;
        }

        // 산성 방울 떨어뜨리기
        dripTimer += Time.deltaTime;
        if (acidDripPrefab != null && dripTimer >= dripFrequency)
        {
            CreateAcidDrip();
            dripTimer = 0f;
        }

        // 지속 시간이 끝나면 효과 페이드아웃
        if (elapsedTime > effectDuration * 0.7f)
        {
            float fadeOut = 1 - ((elapsedTime - (effectDuration * 0.7f)) / (effectDuration * 0.3f));
            fadeOut = Mathf.Clamp01(fadeOut);

            var emission = acidParticles.emission;
            emission.rateOverTime = 10 * fadeOut;

            if (audioSource != null)
            {
                audioSource.volume = 0.5f * fadeOut;
            }
        }

        // 효과 종료
        if (elapsedTime >= effectDuration)
        {
            Destroy(gameObject);
        }
    }

    private void CreateAcidBubble()
    {
        Vector3 randomOffset = Random.insideUnitSphere * 0.5f;
        acidParticles.Emit(new ParticleSystem.EmitParams
        {
            position = transform.position + randomOffset,
            velocity = Vector3.up * 0.2f + Random.insideUnitSphere * 0.1f,
            startLifetime = Random.Range(0.5f, 1.5f),
            startSize = Random.Range(0.1f, 0.3f),
            startColor = acidColor
        }, 1);
    }

    private void CreateAcidDrip()
    {
        if (acidDripPrefab != null)
        {
            Vector3 spawnPosition = transform.position + Random.insideUnitSphere * 0.4f;
            GameObject drip = Instantiate(acidDripPrefab, spawnPosition, Quaternion.identity);

            // 아래쪽 방향으로 조금씩 떨어지도록 설정
            Rigidbody dripRb = drip.GetComponent<Rigidbody>();
            if (dripRb != null)
            {
                dripRb.velocity = new Vector3(
                    Random.Range(-0.1f, 0.1f),
                    -Random.Range(0.5f, 1.0f),
                    Random.Range(-0.1f, 0.1f)
                );
            }

            // 일정 시간 후 자동 제거
            Destroy(drip, 3f);
        }
    }
}