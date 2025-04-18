using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteamPressureEffect : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private Transform attachPoint;         // 이펙트가 붙을 위치 (null이면 자동으로 플레이어 찾음)
    [SerializeField] private float baseScale = 1.0f;        // 기본 스케일
    [Range(0f, 1f)]
    [SerializeField] private float currentPressure = 0f;    // 현재 압력 (0-1 사이)

    [Header("압력 게이지 설정")]
    [SerializeField] private bool showPressureGauge = true;                // 압력 게이지 표시 여부
    [SerializeField] private float gaugeSize = 0.5f;                       // 게이지 크기
    [SerializeField] private Color lowPressureColor = new Color(0.2f, 0.8f, 0.2f, 1.0f);  // 낮은 압력 색상 (녹색)
    [SerializeField] private Color mediumPressureColor = new Color(0.9f, 0.9f, 0.2f, 1.0f); // 중간 압력 색상 (노란색)
    [SerializeField] private Color highPressureColor = new Color(0.9f, 0.2f, 0.2f, 1.0f);  // 높은 압력 색상 (빨간색)

    [Header("증기 설정")]
    [SerializeField] private bool emitSteam = true;             // 증기 방출 여부
    [SerializeField] private int maxSteamParticles = 20;        // 최대 증기 파티클 수
    [SerializeField] private float steamParticleSize = 0.15f;   // 증기 파티클 크기
    [SerializeField] private float steamSpeed = 1.5f;           // 증기 속도
    [SerializeField] private float steamLifetime = 1.0f;        // 증기 수명
    [SerializeField] private Color steamColor = new Color(0.9f, 0.9f, 0.9f, 0.7f); // 증기 색상 (반투명 흰색)

    [Header("기어 설정")]
    [SerializeField] private bool showGears = true;             // 기어 표시 여부
    [SerializeField] private int gearCount = 3;                 // 기어 개수
    [SerializeField] private float gearMinSize = 0.15f;         // 최소 기어 크기
    [SerializeField] private float gearMaxSize = 0.3f;          // 최대 기어 크기
    [SerializeField] private Color gearColor = new Color(0.7f, 0.5f, 0.2f, 1.0f); // 기어 색상 (황동색)

    [Header("파이프 설정")]
    [SerializeField] private bool showPipes = true;             // 파이프 표시 여부
    [SerializeField] private int pipeSegments = 4;              // 파이프 세그먼트 수
    [SerializeField] private float pipeThickness = 0.08f;       // 파이프 두께
    [SerializeField] private Color pipeColor = new Color(0.6f, 0.4f, 0.2f, 1.0f); // 파이프 색상 (구리색)

    [Header("사운드 설정")]
    [SerializeField] private bool playSounds = true;            // 사운드 재생 여부
    [SerializeField] private AudioClip pressureBuildSound;      // 압력 증가 사운드
    [SerializeField] private AudioClip pressureReleaseSound;    // 압력 방출 사운드
    [SerializeField] private AudioClip steamHissSound;          // 증기 분출 사운드
    [SerializeField] private float minPitchVariation = 0.8f;    // 최소 피치 변화
    [SerializeField] private float maxPitchVariation = 1.2f;    // 최대 피치 변화

    // 내부 변수
    private GameObject pressureGaugeObj;
    private List<GameObject> steamParticles = new List<GameObject>();
    private List<GameObject> gearObjs = new List<GameObject>();
    private List<GameObject> pipeObjs = new List<GameObject>();
    private AudioSource audioSource;
    private float lastPressureLevel = 0f;
    private float lastSteamEmitTime = 0f;
    private float steamEmitInterval = 0.1f;
    private bool isReleasing = false;

    // 회전 방향을 저장하기 위한 간단한 컴포넌트
    private class GearRotation : MonoBehaviour
    {
        public bool isClockwise = true;
    }

    private void Start()
    {
        // 오디오 소스 설정
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && playSounds)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D 사운드
            audioSource.volume = 0.6f;
            audioSource.pitch = 1.0f;
        }

        // 연결 지점 설정
        if (attachPoint == null)
        {
            // 이제 부모 객체(플레이어)의 Transform을 사용
            attachPoint = transform.parent;
            
            // 부모가 없는 경우를 대비하여 안전 장치 추가
            if (attachPoint == null)
            {
                attachPoint = transform;
                Debug.LogWarning("SteamPressureEffect: 부모 객체가 없어 자신의 위치로 설정됩니다.");
            }
        }

        // 초기 구성 요소 생성
        if (showPressureGauge) CreatePressureGauge();
        if (showGears) CreateGears();
        if (showPipes) CreatePipes();

        // 바로 업데이트하여 시각적으로 설정
        UpdateVisuals();
    }

    private void Update()
    {
        // 플레이어의 자식으로 생성되므로 위치 업데이트 코드 제거
        // 플레이어 따라가는 코드 필요 없음
        
        // 증기 파티클 생성 (압력에 따라 빈도 조절)
        if (emitSteam && Time.time > lastSteamEmitTime + steamEmitInterval / Mathf.Max(0.1f, currentPressure))
        {
            EmitSteamParticle();
            lastSteamEmitTime = Time.time;
        }

        // 압력 레벨 변화 감지 및 처리
        float pressureLevel = GetPressureLevel();
        if (pressureLevel != lastPressureLevel)
        {
            // 압력 상승 시 사운드 재생
            if (pressureLevel > lastPressureLevel && playSounds && pressureBuildSound != null && !isReleasing)
            {
                PlayPressureSound(pressureBuildSound, 0.2f, pressureLevel);
            }

            lastPressureLevel = pressureLevel;
        }

        // 시각적 요소 업데이트
        UpdateVisuals();

        // 증기 파티클 업데이트
        UpdateSteamParticles();

        // 기어 회전 업데이트
        UpdateGears();
    }

    // 압력 레벨 반환 (0: 낮음, 1: 중간, 2: 높음)
    private int GetPressureLevel()
    {
        if (currentPressure < 0.33f) return 0;
        else if (currentPressure < 0.66f) return 1;
        else return 2;
    }

    // 압력 게이지 생성
    private void CreatePressureGauge()
    {
        if (pressureGaugeObj != null)
        {
            Destroy(pressureGaugeObj);
        }

        pressureGaugeObj = new GameObject("PressureGauge");
        pressureGaugeObj.transform.parent = transform;
        pressureGaugeObj.transform.localPosition = new Vector3(0, 0.7f * baseScale, 0);
        pressureGaugeObj.transform.localScale = new Vector3(gaugeSize, gaugeSize, gaugeSize);

        // 게이지 배경 스프라이트 렌더러 추가
        GameObject gaugeBackground = new GameObject("GaugeBackground");
        gaugeBackground.transform.parent = pressureGaugeObj.transform;
        gaugeBackground.transform.localPosition = Vector3.zero;

        SpriteRenderer bgRenderer = gaugeBackground.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = CreateCircleSprite(32, Color.black, Color.gray);
        bgRenderer.sortingOrder = 1;

        // 게이지 표시기 생성
        GameObject gaugeIndicator = new GameObject("GaugeIndicator");
        gaugeIndicator.transform.parent = pressureGaugeObj.transform;
        gaugeIndicator.transform.localPosition = Vector3.zero;

        SpriteRenderer indicatorRenderer = gaugeIndicator.AddComponent<SpriteRenderer>();
        indicatorRenderer.sprite = CreateGaugeIndicatorSprite();
        indicatorRenderer.sortingOrder = 2;

        // 게이지 바늘 생성
        GameObject gaugeNeedle = new GameObject("GaugeNeedle");
        gaugeNeedle.transform.parent = pressureGaugeObj.transform;
        gaugeNeedle.transform.localPosition = Vector3.zero;

        SpriteRenderer needleRenderer = gaugeNeedle.AddComponent<SpriteRenderer>();
        needleRenderer.sprite = CreateNeedleSprite();
        needleRenderer.sortingOrder = 3;
    }

    // 기어 생성
    private void CreateGears()
    {
        // 기존 기어 삭제
        foreach (var gear in gearObjs)
        {
            Destroy(gear);
        }
        gearObjs.Clear();

        // 새 기어 생성
        for (int i = 0; i < gearCount; i++)
        {
            GameObject gear = new GameObject("Gear_" + i);
            gear.transform.parent = transform;

            // 기어 위치 및 크기 설정
            float angle = i * (360f / gearCount);
            float rad = angle * Mathf.Deg2Rad;
            float distance = 0.5f * baseScale;
            float size = Random.Range(gearMinSize, gearMaxSize) * baseScale;

            gear.transform.localPosition = new Vector3(Mathf.Cos(rad) * distance, Mathf.Sin(rad) * distance, 0);
            gear.transform.localScale = new Vector3(size, size, size);

            // 기어 스프라이트 렌더러 추가
            SpriteRenderer gearRenderer = gear.AddComponent<SpriteRenderer>();
            gearRenderer.sprite = CreateGearSprite(Random.Range(6, 12)); // 6-12개의 톱니
            gearRenderer.color = gearColor;
            gearRenderer.sortingOrder = 1;

            // 회전 방향 컴포넌트 추가
            GearRotation rotation = gear.AddComponent<GearRotation>();
            rotation.isClockwise = (i % 2 == 0);

            gearObjs.Add(gear);
        }
    }

    // 파이프 생성
    private void CreatePipes()
    {
        // 기존 파이프 삭제
        foreach (var pipe in pipeObjs)
        {
            Destroy(pipe);
        }
        pipeObjs.Clear();

        // 새 파이프 생성
        for (int i = 0; i < pipeSegments; i++)
        {
            GameObject pipe = new GameObject("Pipe_" + i);
            pipe.transform.parent = transform;

            // 파이프 위치 및 회전 설정
            float angle = i * (360f / pipeSegments) + (360f / pipeSegments / 2); // 기어 사이에 배치
            float rad = angle * Mathf.Deg2Rad;
            float distance = 0.4f * baseScale;

            pipe.transform.localPosition = new Vector3(Mathf.Cos(rad) * distance, Mathf.Sin(rad) * distance, 0);
            pipe.transform.localRotation = Quaternion.Euler(0, 0, angle - 90);

            // 파이프 스프라이트 렌더러 추가
            SpriteRenderer pipeRenderer = pipe.AddComponent<SpriteRenderer>();
            pipeRenderer.sprite = CreatePipeSprite();
            pipeRenderer.color = pipeColor;
            pipeRenderer.sortingOrder = 0;

            pipe.transform.localScale = new Vector3(pipeThickness * baseScale, 0.3f * baseScale, 1);

            pipeObjs.Add(pipe);
        }
    }

    // 증기 파티클 방출
    private void EmitSteamParticle()
    {
        if (steamParticles.Count >= maxSteamParticles * currentPressure)
        {
            // 오래된 파티클 제거
            if (steamParticles.Count > 0)
            {
                Destroy(steamParticles[0]);
                steamParticles.RemoveAt(0);
            }
        }

        // 랜덤 방출 위치 선택
        int emitPoint = Random.Range(0, pipeObjs.Count);
        if (pipeObjs.Count > emitPoint)
        {
            // 파티클 생성
            GameObject particle = new GameObject("SteamParticle");
            particle.transform.parent = transform;

            // 파이프 끝에서 파티클 위치 계산
            Vector3 pipeEndPos = pipeObjs[emitPoint].transform.position +
                             (pipeObjs[emitPoint].transform.up * 0.15f * baseScale);
            particle.transform.position = pipeEndPos;

            // 스프라이트 렌더러 추가
            SpriteRenderer particleRenderer = particle.AddComponent<SpriteRenderer>();
            particleRenderer.sprite = CreateCloudSprite();
            particleRenderer.color = steamColor;
            particleRenderer.sortingOrder = 4;

            // 초기 크기 및 속도 설정
            float size = steamParticleSize * baseScale * (0.5f + currentPressure * 0.5f);
            particle.transform.localScale = new Vector3(size * 0.5f, size * 0.5f, size * 0.5f);

            // 파티클 속성 설정 (코루틴으로 확산 효과)
            StartCoroutine(AnimateSteamParticle(particle, pipeObjs[emitPoint].transform.up,
                                               steamSpeed * (0.5f + currentPressure * 1.5f)));

            // 리스트에 추가
            steamParticles.Add(particle);

            // 압력이 높을 때 증기 소리 재생
            if (playSounds && steamHissSound != null && currentPressure > 0.5f && Random.value > 0.7f)
            {
                audioSource.pitch = Random.Range(minPitchVariation, maxPitchVariation);
                audioSource.PlayOneShot(steamHissSound, currentPressure * 0.3f);
            }
        }
    }

    // 시각적 요소 업데이트
    private void UpdateVisuals()
    {
        // 압력 게이지 업데이트
        if (showPressureGauge && pressureGaugeObj != null)
        {
            // 게이지 바늘 회전
            Transform needle = pressureGaugeObj.transform.Find("GaugeNeedle");
            if (needle != null)
            {
                float rotationAngle = -90f + currentPressure * 180f; // -90도에서 +90도까지
                needle.localRotation = Quaternion.Euler(0, 0, rotationAngle);
            }

            // 압력 레벨에 따른 색상 변경
            Transform indicator = pressureGaugeObj.transform.Find("GaugeIndicator");
            if (indicator != null)
            {
                SpriteRenderer indicatorRenderer = indicator.GetComponent<SpriteRenderer>();
                if (indicatorRenderer != null)
                {
                    Color pressureColor;
                    if (currentPressure < 0.33f)
                        pressureColor = lowPressureColor;
                    else if (currentPressure < 0.66f)
                        pressureColor = mediumPressureColor;
                    else
                        pressureColor = highPressureColor;

                    indicatorRenderer.color = pressureColor;
                }
            }
        }

        // 파이프 색상 업데이트 (압력에 따라 더 어두운/밝은 색)
        if (showPipes)
        {
            foreach (var pipe in pipeObjs)
            {
                SpriteRenderer pipeRenderer = pipe.GetComponent<SpriteRenderer>();
                if (pipeRenderer != null)
                {
                    // 압력이 높을수록 더 밝은 색상
                    float colorIntensity = 0.8f + (currentPressure * 0.4f);
                    pipeRenderer.color = new Color(
                        pipeColor.r * colorIntensity,
                        pipeColor.g * colorIntensity,
                        pipeColor.b * colorIntensity,
                        pipeColor.a
                    );
                }
            }
        }
    }

    // 증기 파티클 업데이트
    private void UpdateSteamParticles()
    {
        // 특별한 업데이트 로직은 코루틴에서 처리
    }

    // 기어 업데이트 (회전)
    private void UpdateGears()
    {
        if (!showGears) return;

        float pressureBasedSpeed = 30f + (currentPressure * 90f); // 압력에 따른 회전 속도

        foreach (var gear in gearObjs)
        {
            GearRotation rotation = gear.GetComponent<GearRotation>();
            if (rotation != null)
            {
                if (rotation.isClockwise)
                {
                    gear.transform.Rotate(0, 0, -pressureBasedSpeed * Time.deltaTime);
                }
                else
                {
                    gear.transform.Rotate(0, 0, pressureBasedSpeed * Time.deltaTime);
                }
            }
        }
    }

    // 증기 파티클 애니메이션 코루틴
    private IEnumerator AnimateSteamParticle(GameObject particle, Vector3 direction, float speed)
    {
        float lifetime = 0f;
        float maxLifetime = steamLifetime * (0.7f + Random.value * 0.6f); // 약간의 변화
        Vector3 initialScale = particle.transform.localScale;
        Vector3 targetScale = initialScale * 2f; // 최종 크기는 초기 크기의 2배
        Vector3 velocity = direction * speed + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.2f, 0.8f), 0);
        Color initialColor = particle.GetComponent<SpriteRenderer>().color;

        while (lifetime < maxLifetime && particle != null)
        {
            // 시간 업데이트
            lifetime += Time.deltaTime;
            float normalizedTime = lifetime / maxLifetime;

            // 이동
            if (particle != null)
                particle.transform.position += velocity * Time.deltaTime;

            // 느려지는 속도
            velocity *= (1f - Time.deltaTime * 0.5f);

            // 크기 변화 (점점 커짐)
            if (particle != null)
                particle.transform.localScale = Vector3.Lerp(initialScale, targetScale, normalizedTime);

            // 투명도 변화 (점점 사라짐)
            if (particle != null)
            {
                SpriteRenderer renderer = particle.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    Color color = initialColor;
                    color.a = initialColor.a * (1f - normalizedTime);
                    renderer.color = color;
                }
            }

            yield return null;
        }

        // 파티클 제거
        if (particle != null)
        {
            steamParticles.Remove(particle);
            Destroy(particle);
        }
    }

    // 압력 사운드 재생
    private void PlayPressureSound(AudioClip clip, float volume, float pitchMultiplier)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.pitch = Random.Range(minPitchVariation, maxPitchVariation) * (0.8f + pitchMultiplier * 0.4f);
            audioSource.PlayOneShot(clip, volume);
        }
    }

    // 압력 설정 (외부에서 호출할 메서드)
    public void SetPressure(float pressure)
    {
        currentPressure = Mathf.Clamp01(pressure);
    }

    // 압력 증가 (외부에서 호출할 메서드)
    public void IncreasePressure(float amount)
    {
        isReleasing = false;
        currentPressure = Mathf.Clamp01(currentPressure + amount);
    }

    // 압력 방출 (외부에서 호출할 메서드)
    public void ReleasePressure()
    {
        // 게임 오브젝트가 비활성화된 상태에서는 코루틴을 시작할 수 없음
        // 대신 현재 상태만 변경하고 코루틴은 호출하지 않음
        isReleasing = true;
        currentPressure = 0f;
        
        // 게임 오브젝트가 활성화된 상태에서만 코루틴 시작
        if (gameObject.activeSelf)
        {
            // 압력 방출 사운드 재생
            if (playSounds && pressureReleaseSound != null && currentPressure > 0.3f)
            {
                PlayPressureSound(pressureReleaseSound, currentPressure, 1.0f);
            }

            // 압력에 따라 더 많은 증기 파티클 방출
            StartCoroutine(BurstSteamParticles());
        }
        
        // 압력 초기화는 이미 위에서 함
    }

    // 압력 방출 시 증기 폭발 효과
    private IEnumerator BurstSteamParticles()
    {
        float burstAmount = 20f; // 기본 증기 파티클 수

        for (int i = 0; i < burstAmount; i++)
        {
            EmitSteamParticle();
            yield return new WaitForSeconds(0.05f);
        }

        isReleasing = false;
    }

    // 원형 스프라이트 생성
    private Sprite CreateCircleSprite(int resolution = 32, Color centerColor = default, Color edgeColor = default)
    {
        if (centerColor == default) centerColor = Color.white;
        if (edgeColor == default) edgeColor = centerColor;

        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] colors = new Color[resolution * resolution];

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float distX = x - resolution / 2;
                float distY = y - resolution / 2;
                float dist = Mathf.Sqrt(distX * distX + distY * distY) / (resolution / 2);

                if (dist <= 1.0f)
                {
                    colors[y * resolution + x] = Color.Lerp(centerColor, edgeColor, dist);
                }
                else
                {
                    colors[y * resolution + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    // 게이지 바늘 스프라이트 생성
    private Sprite CreateNeedleSprite()
    {
        int resolution = 32;
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] colors = new Color[resolution * resolution];

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                colors[y * resolution + x] = Color.clear;
            }
        }

        // 바늘 그리기 (직선)
        for (int y = 0; y < resolution; y++)
        {
            int x = resolution / 2;
            if (y >= resolution / 2 - 2 && y <= resolution - 5)
            {
                colors[y * resolution + x] = Color.black;
                if (x + 1 < resolution) colors[y * resolution + x + 1] = Color.black;
            }
        }

        // 바늘 중심 원 그리기
        for (int x = resolution / 2 - 3; x <= resolution / 2 + 3; x++)
        {
            for (int y = resolution / 2 - 3; y <= resolution / 2 + 3; y++)
            {
                if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                {
                    float distX = x - resolution / 2;
                    float distY = y - resolution / 2;
                    float dist = Mathf.Sqrt(distX * distX + distY * distY);

                    if (dist <= 3.0f)
                    {
                        colors[y * resolution + x] = Color.red;
                    }
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    // 게이지 인디케이터 스프라이트 생성
    private Sprite CreateGaugeIndicatorSprite()
    {
        int resolution = 64;
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] colors = new Color[resolution * resolution];

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                colors[y * resolution + x] = Color.clear;
            }
        }

        // 반원 그리기 (하단 반원)
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution / 2; y++)
            {
                float distX = x - resolution / 2;
                float distY = y - resolution / 2;
                float dist = Mathf.Sqrt(distX * distX + distY * distY);

                // 외부 원
                if (dist >= resolution * 0.3f && dist <= resolution * 0.35f)
                {
                    colors[y * resolution + x] = new Color(0.7f, 0.7f, 0.7f);
                }
                // 내부 색상 그라데이션 (녹색 -> 노란색 -> 빨간색)
                else if (dist < resolution * 0.3f)
                {
                    // 각도 계산 (-180도 ~ 0도 사이)
                    float angle = Mathf.Atan2(distY, distX) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360;

                    // 색상 결정
                    Color sectionColor;
                    if (angle > 240 && angle <= 300) // 왼쪽 (녹색)
                    {
                        sectionColor = lowPressureColor;
                    }
                    else if (angle > 300 || angle <= 0) // 중간 (노란색)
                    {
                        sectionColor = mediumPressureColor;
                    }
                    else if (angle > 0 && angle <= 60) // 오른쪽 (빨간색)
                    {
                        sectionColor = highPressureColor;
                    }
                    else
                    {
                        sectionColor = Color.clear;
                    }

                    // 거리가 멀수록 더 투명하게
                    float alpha = 1.0f - (dist / (resolution * 0.3f));
                    if (alpha > 0)
                    {
                        colors[y * resolution + x] = new Color(sectionColor.r, sectionColor.g, sectionColor.b, alpha * 0.7f);
                    }
                }
            }
        }

        // 눈금 표시
        for (int i = -3; i <= 3; i++)
        {
            float angle = -90 + i * 30; // -90도 ~ +90도 사이 눈금
            float rad = angle * Mathf.Deg2Rad;
            int startRadius = (int)(resolution * 0.25f);
            int endRadius = (int)(resolution * 0.35f);

            int centerX = resolution / 2;
            int centerY = resolution / 2;

            for (int r = startRadius; r <= endRadius; r++)
            {
                int x = centerX + (int)(Mathf.Cos(rad) * r);
                int y = centerY + (int)(Mathf.Sin(rad) * r);

                if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                {
                    colors[y * resolution + x] = Color.white;
                    // 두께를 위해 주변 픽셀도 설정
                    if (x + 1 < resolution) colors[y * resolution + x + 1] = Color.white;
                    if (y + 1 < resolution) colors[(y + 1) * resolution + x] = Color.white;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    // 기어 스프라이트 생성
    private Sprite CreateGearSprite(int teethCount = 8)
    {
        int resolution = 64;
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] colors = new Color[resolution * resolution];

        // 배경 초기화
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                colors[y * resolution + x] = Color.clear;
            }
        }

        float outerRadius = resolution * 0.4f;
        float innerRadius = resolution * 0.3f;
        float teethLength = resolution * 0.1f;

        // 기어 그리기
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float distX = x - resolution / 2;
                float distY = y - resolution / 2;
                float dist = Mathf.Sqrt(distX * distX + distY * distY);

                // 각도 계산
                float angle = Mathf.Atan2(distY, distX) * Mathf.Rad2Deg;
                if (angle < 0) angle += 360;

                // 톱니 여부 확인
                float toothAngle = 360f / teethCount;
                float angularDistance = angle % toothAngle;
                bool isToothArea = angularDistance < toothAngle * 0.5f;

                // 내부 원
                if (dist <= innerRadius)
                {
                    colors[y * resolution + x] = gearColor;
                }
                // 톱니 부분
                else if (dist <= outerRadius || (isToothArea && dist <= outerRadius + teethLength))
                {
                    // 중심에서 가장자리로 갈수록 더 어두운 색상
                    float brightness = 1.0f - (dist - innerRadius) / (outerRadius + teethLength - innerRadius) * 0.3f;
                    colors[y * resolution + x] = new Color(
                        gearColor.r * brightness,
                        gearColor.g * brightness,
                        gearColor.b * brightness,
                        gearColor.a
                    );
                }
            }
        }

        // 중심 구멍
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float distX = x - resolution / 2;
                float distY = y - resolution / 2;
                float dist = Mathf.Sqrt(distX * distX + distY * distY);

                if (dist <= resolution * 0.1f)
                {
                    colors[y * resolution + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    // 파이프 스프라이트 생성
    private Sprite CreatePipeSprite()
    {
        int width = 32;
        int height = 128;
        Texture2D texture = new Texture2D(width, height);
        Color[] colors = new Color[width * height];

        // 배경 초기화
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                colors[y * width + x] = Color.clear;
            }
        }

        // 파이프 그리기 (위쪽에서 아래쪽으로)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 파이프 경계 결정
                bool isLeftEdge = x < width * 0.2f;
                bool isRightEdge = x > width * 0.8f;
                bool isTopSection = y > height * 0.8f;

                if (isLeftEdge || isRightEdge)
                {
                    // 파이프 가장자리 (더 어두운 색상)
                    colors[y * width + x] = new Color(
                        pipeColor.r * 0.7f,
                        pipeColor.g * 0.7f,
                        pipeColor.b * 0.7f,
                        pipeColor.a
                    );
                }
                else if (isTopSection)
                {
                    // 파이프 상단 부분 (밸브/출구)
                    if (y > height * 0.9f)
                    {
                        // 파이프 출구
                        colors[y * width + x] = new Color(
                            pipeColor.r * 0.6f,
                            pipeColor.g * 0.6f,
                            pipeColor.b * 0.6f,
                            pipeColor.a
                        );
                    }
                    else if (y > height * 0.85f)
                    {
                        // 밸브 연결부
                        colors[y * width + x] = new Color(
                            pipeColor.r * 1.1f,
                            pipeColor.g * 1.1f,
                            pipeColor.b * 1.1f,
                            pipeColor.a
                        );
                    }
                    else
                    {
                        // 일반 파이프
                        colors[y * width + x] = pipeColor;
                    }
                }
                else
                {
                    // 일반 파이프
                    colors[y * width + x] = pipeColor;
                }

                // 파이프 하이라이트 추가
                bool isHighlightArea = x > width * 0.3f && x < width * 0.4f;
                if (isHighlightArea)
                {
                    colors[y * width + x] = new Color(
                        pipeColor.r * 1.2f,
                        pipeColor.g * 1.2f,
                        pipeColor.b * 1.2f,
                        pipeColor.a
                    );
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0));
    }

    // 구름(증기) 스프라이트 생성
    private Sprite CreateCloudSprite()
    {
        int resolution = 32;
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] colors = new Color[resolution * resolution];

        // 배경 초기화
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                colors[y * resolution + x] = Color.clear;
            }
        }

        // 여러 개의 원을 그려 구름 모양 만들기
        List<Vector2> cloudCircles = new List<Vector2>()
        {
            new Vector2(resolution * 0.5f, resolution * 0.5f),  // 중앙
            new Vector2(resolution * 0.3f, resolution * 0.5f),  // 왼쪽
            new Vector2(resolution * 0.7f, resolution * 0.5f),  // 오른쪽
            new Vector2(resolution * 0.4f, resolution * 0.65f), // 왼쪽 위
            new Vector2(resolution * 0.6f, resolution * 0.65f)  // 오른쪽 위
        };

        float[] cloudRadii = { resolution * 0.25f, resolution * 0.2f, resolution * 0.2f, resolution * 0.15f, resolution * 0.15f };

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float maxDensity = 0f;

                // 각 구름 원에서의 밀도 계산
                for (int i = 0; i < cloudCircles.Count; i++)
                {
                    float distX = x - cloudCircles[i].x;
                    float distY = y - cloudCircles[i].y;
                    float dist = Mathf.Sqrt(distX * distX + distY * distY);

                    if (dist <= cloudRadii[i])
                    {
                        float density = 1.0f - (dist / cloudRadii[i]);
                        maxDensity = Mathf.Max(maxDensity, density);
                    }
                }

                // 최종 색상 설정
                if (maxDensity > 0)
                {
                    colors[y * resolution + x] = new Color(1, 1, 1, maxDensity * 0.8f);
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    // 현재 압력 반환 메서드 (외부에서 접근 가능)
    public float GetCurrentPressure()
    {
        return currentPressure;
    }

    // 외부에서 직접 증기 파티클을 생성할 수 있는 메서드
    public void EmitSteamParticleExternal()
    {
        if (gameObject.activeSelf)
        {
            EmitSteamParticle();
        }
    }
}