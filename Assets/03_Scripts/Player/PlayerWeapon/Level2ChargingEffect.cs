using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level2ChargingEffect : MonoBehaviour
{
    [Header("기본 이펙트 설정")]
    [SerializeField] private float rotationSpeed = 120f;        // 회전 속도(초당 각도)
    [SerializeField] private float pulseSpeed = 3f;             // 맥동 속도
    [SerializeField] private float pulseMinScale = 0.9f;        // 최소 스케일
    [SerializeField] private float pulseMaxScale = 1.5f;        // 최대 스케일
    [SerializeField] private Color effectColor = Color.red;     // 이펙트 색상
    [SerializeField] private float alphaValue = 0.8f;           // 투명도

    [Header("외부 링 설정")]
    [SerializeField] private bool useOuterRing = true;          // 외부 링 사용 여부
    [SerializeField] private float outerRingSize = 1.5f;        // 외부 링 크기
    [SerializeField] private float outerRingRotationSpeed = -90f; // 외부 링 회전 속도
    [SerializeField] private Color outerRingColor = new Color(1f, 0.5f, 0f, 0.6f); // 주황색, 반투명

    [Header("파티클 설정")]
    [SerializeField] private bool useParticles = true;          // 파티클 사용 여부
    [SerializeField] private int particleCount = 12;            // 파티클 개수
    [SerializeField] private float particleSpeed = 1.5f;        // 파티클 속도
    [SerializeField] private float particleSize = 0.15f;        // 파티클 크기

    [Header("에너지 볼 설정")]
    [SerializeField] private bool useEnergyBalls = true;        // 에너지 볼 사용 여부
    [SerializeField] private int energyBallCount = 4;           // 에너지 볼 개수
    [SerializeField] private float energyBallSize = 0.2f;       // 에너지 볼 크기
    [SerializeField] private float energyBallOrbitSpeed = 2.0f; // 에너지 볼 궤도 속도
    [SerializeField] private float energyBallOrbitRadius = 0.8f; // 에너지 볼 궤도 반경

    private SpriteRenderer spriteRenderer;
    private GameObject outerRingObj;
    private List<GameObject> particles = new List<GameObject>();
    private List<GameObject> energyBalls = new List<GameObject>();
    private Transform playerTransform;
    private float initialScale;
    private Sprite circleSprite = null;
    private Sprite ringSprite = null;
    
    // 최소/최대 Z위치 제한 (카메라와의 거리 제한)
    private const float MIN_Z_POSITION = -10f;
    private const float MAX_Z_POSITION = 10f;

    private void Awake()
    {
        // 스프라이트 미리 생성
        circleSprite = CreateCircleSprite();
        ringSprite = CreateRingSprite();
    }

    private void Start()
    {
        // 이펙트 오브젝트가 항상 정렬 문제없이 표시되도록 Z값 설정
        Vector3 safePosition = transform.position;
        safePosition.z = 0f; // Z값을 0으로 설정
        transform.position = safePosition;
        
        // 스프라이트 렌더러 설정
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = circleSprite;
        }
        
        // 스프라이트 렌더러 정렬 모드 설정
        spriteRenderer.sortingLayerName = "Effects"; // 이펙트 레이어에 배치 (필요에 따라 조정)
        spriteRenderer.sortingOrder = 10; // 높은 순서로 설정하여 앞에 표시
        
        // 정렬 모드를 SpriteSortMode.Immediate로 설정
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transparencySortMode = TransparencySortMode.Orthographic;
        }

        // 색상 및 투명도 설정
        spriteRenderer.color = new Color(effectColor.r, effectColor.g, effectColor.b, alphaValue);

        // 초기 스케일 저장
        initialScale = transform.localScale.x;

        // 외부 링 생성
        if (useOuterRing && ringSprite != null)
        {
            CreateOuterRing();
        }

        // 파티클 생성
        if (useParticles && circleSprite != null)
        {
            CreateParticles();
        }

        // 에너지 볼 생성
        if (useEnergyBalls && circleSprite != null)
        {
            CreateEnergyBalls();
        }

        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        // 회전 애니메이션
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        // 맥동 애니메이션 (사인파를 이용한 스케일 변화)
        float pulse = Mathf.Lerp(pulseMinScale, pulseMaxScale, (Mathf.Sin(Time.time * pulseSpeed) + 1) / 2);
        // 무한값 체크
        if (float.IsFinite(pulse))
        {
            pulse = Mathf.Clamp(pulse, 0.1f, 2.0f); // 값을 안전한 범위로 제한
            transform.localScale = new Vector3(initialScale * pulse, initialScale * pulse, initialScale);
        }

        // 외부 링 업데이트
        if (useOuterRing && outerRingObj != null)
        {
            UpdateOuterRing();
        }

        // 파티클 업데이트
        if (useParticles)
        {
            UpdateParticles();
        }

        // 에너지 볼 업데이트
        if (useEnergyBalls)
        {
            UpdateEnergyBalls();
        }

        // 플레이어를 따라가도록 설정
        if (playerTransform != null)
        {
            // 플레이어 위치가 유효한지 확인
            if (float.IsFinite(playerTransform.position.x) && float.IsFinite(playerTransform.position.y))
            {
                // Z 값은 0으로 고정 (무한대 값 방지)
                Vector3 newPosition = new Vector3(
                    playerTransform.position.x,
                    playerTransform.position.y,
                    0f
                );
                transform.position = newPosition;
            }
        }
        
        // 매 프레임마다 Z 위치를 검사하고 고정
        EnsureSafeZPosition(transform);
    }
    
    // 안전한 Z 위치 유지
    private void EnsureSafeZPosition(Transform targetTransform)
    {
        if (targetTransform == null) return;
        
        Vector3 position = targetTransform.position;
        if (!float.IsFinite(position.z) || position.z < MIN_Z_POSITION || position.z > MAX_Z_POSITION)
        {
            position.z = 0f;
            targetTransform.position = position;
        }
        
        // 자식 오브젝트에도 적용
        for (int i = 0; i < targetTransform.childCount; i++)
        {
            Transform child = targetTransform.GetChild(i);
            if (child != null)
            {
                Vector3 localPos = child.localPosition;
                if (!float.IsFinite(localPos.z) || localPos.z < MIN_Z_POSITION || localPos.z > MAX_Z_POSITION)
                {
                    localPos.z = 0f;
                    child.localPosition = localPos;
                }
            }
        }
    }

    private void CreateOuterRing()
    {
        outerRingObj = new GameObject("OuterRing");
        outerRingObj.transform.parent = transform;
        outerRingObj.transform.localPosition = Vector3.zero;
        outerRingObj.transform.localScale = new Vector3(outerRingSize, outerRingSize, 1f);

        // 스프라이트 렌더러 추가
        SpriteRenderer ringRenderer = outerRingObj.AddComponent<SpriteRenderer>();
        ringRenderer.sprite = ringSprite;
        ringRenderer.color = outerRingColor;
        ringRenderer.sortingOrder = -1; // 메인 이펙트 뒤에 표시
        
        // SpriteMaskInteraction 설정
        ringRenderer.maskInteraction = SpriteMaskInteraction.None;
    }

    private void UpdateOuterRing()
    {
        if (outerRingObj == null) return;

        // 역방향 회전
        outerRingObj.transform.Rotate(0, 0, outerRingRotationSpeed * Time.deltaTime);

        // 맥동 애니메이션 (주 효과와 다른 위상)
        float phase = Mathf.PI / 2; // 90도 위상차
        float pulse = Mathf.Lerp(pulseMinScale, pulseMaxScale, (Mathf.Sin(Time.time * pulseSpeed + phase) + 1) / 2);
        
        // 무한값 체크
        if (float.IsFinite(pulse))
        {
            pulse = Mathf.Clamp(pulse, 0.1f, 2.0f); // 값을 안전한 범위로 제한
            outerRingObj.transform.localScale = new Vector3(outerRingSize * pulse, outerRingSize * pulse, 1f);
        }
        
        // Z 위치 검사
        Vector3 localPos = outerRingObj.transform.localPosition;
        if (!float.IsFinite(localPos.z) || localPos.z != 0f)
        {
            localPos.z = 0f;
            outerRingObj.transform.localPosition = localPos;
        }
    }

    private void CreateParticles()
    {
        // 기존 파티클 삭제
        foreach (var particle in particles)
        {
            if (particle != null)
                Destroy(particle);
        }
        particles.Clear();

        // 새 파티클 생성
        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = new GameObject("Particle_" + i);
            particle.transform.parent = transform;

            // 스프라이트 렌더러 추가
            SpriteRenderer particleRenderer = particle.AddComponent<SpriteRenderer>();
            particleRenderer.sprite = circleSprite;
            particleRenderer.sortingOrder = 9; // 메인 이펙트보다 뒤에 표시
            particleRenderer.maskInteraction = SpriteMaskInteraction.None;

            // 파티클마다 약간 다른 색상 사용
            float hue = effectColor.r * 0.299f + effectColor.g * 0.587f + effectColor.b * 0.114f;
            Color particleColor = Color.HSVToRGB(
                Random.Range(-0.1f, 0.1f) + hue,
                Random.Range(0.8f, 1.0f),
                Random.Range(0.8f, 1.0f)
            );
            particleRenderer.color = new Color(particleColor.r, particleColor.g, particleColor.b, alphaValue * 0.7f);

            // 크기 설정
            float size = particleSize * Random.Range(0.8f, 1.2f);
            particle.transform.localScale = new Vector3(size, size, size);

            // 초기 위치 설정 (원형으로 배치)
            float angle = i * (360f / particleCount);
            float rad = angle * Mathf.Deg2Rad;
            float distance = Random.Range(0.4f, 0.6f);
            particle.transform.localPosition = new Vector3(Mathf.Cos(rad) * distance, Mathf.Sin(rad) * distance, 0f);

            // 리스트에 추가
            particles.Add(particle);
        }
    }

    private void UpdateParticles()
    {
        if (particles.Count == 0) return;

        for (int i = 0; i < particles.Count; i++)
        {
            GameObject particle = particles[i];
            if (particle == null) continue;

            try
            {
                // 회전 애니메이션 (RotateAround 대신 직접 계산)
                float angle = rotationSpeed * ((i % 2 == 0) ? 1 : -1) * Time.deltaTime * particleSpeed;
                
                if (float.IsFinite(angle))
                {
                    // 현재 위치 가져오기(로컬 좌표계)
                    Vector3 currentLocalPos = particle.transform.localPosition;
                    
                    // 회전 행렬 생성
                    float rad = angle * Mathf.Deg2Rad;
                    float cos = Mathf.Cos(rad);
                    float sin = Mathf.Sin(rad);
                    
                    // 새 위치 계산
                    float newX = currentLocalPos.x * cos - currentLocalPos.y * sin;
                    float newY = currentLocalPos.x * sin + currentLocalPos.y * cos;
                    
                    // 무한값 체크 후 적용
                    if (float.IsFinite(newX) && float.IsFinite(newY))
                    {
                        particle.transform.localPosition = new Vector3(newX, newY, 0f);
                    }
                }

                // 맥동 애니메이션 (파티클마다 다른 위상)
                float phase = (float)i / particleCount * Mathf.PI;
                float pulse = Mathf.Lerp(pulseMinScale, pulseMaxScale, (Mathf.Sin(Time.time * pulseSpeed + phase) + 1) / 2);

                // 무한값 체크
                if (float.IsFinite(pulse))
                {
                    pulse = Mathf.Clamp(pulse, 0.1f, 2.0f); // 값을 안전한 범위로 제한
                    
                    // 원래 크기 보존
                    Vector3 originalScale = particle.transform.localScale;
                    float avgOriginalScale = (originalScale.x + originalScale.y) / 2;
                    
                    if (float.IsFinite(avgOriginalScale))
                    {
                        particle.transform.localScale = new Vector3(
                            avgOriginalScale * pulse,
                            avgOriginalScale * pulse,
                            avgOriginalScale
                        );
                    }
                }

                // 색상 깜박임 효과
                SpriteRenderer particleRenderer = particle.GetComponent<SpriteRenderer>();
                if (particleRenderer != null)
                {
                    Color color = particleRenderer.color;
                    float alpha = Mathf.Lerp(0.3f, 0.8f, (Mathf.Sin(Time.time * 3f + phase) + 1) / 2);
                    
                    if (float.IsFinite(alpha))
                    {
                        alpha = Mathf.Clamp01(alpha); // 알파값 0-1 사이로 제한
                        particleRenderer.color = new Color(color.r, color.g, color.b, alpha);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"파티클 업데이트 중 오류: {e.Message}");
                
                // 오류 발생 시 파티클의 위치와 스케일을 안전한 값으로 리셋
                if (particle != null)
                {
                    particle.transform.localPosition = Vector3.zero;
                    particle.transform.localScale = Vector3.one * particleSize;
                }
            }
        }
    }

    private void CreateEnergyBalls()
    {
        // 기존 에너지 볼 삭제
        foreach (var ball in energyBalls)
        {
            if (ball != null)
                Destroy(ball);
        }
        energyBalls.Clear();

        // 새 에너지 볼 생성
        for (int i = 0; i < energyBallCount; i++)
        {
            GameObject ball = new GameObject("EnergyBall_" + i);
            ball.transform.parent = transform;

            // 스프라이트 렌더러 추가
            SpriteRenderer ballRenderer = ball.AddComponent<SpriteRenderer>();
            ballRenderer.sprite = circleSprite;
            ballRenderer.sortingOrder = 11; // 메인 이펙트보다 앞에 표시
            ballRenderer.maskInteraction = SpriteMaskInteraction.None;

            // 에너지 볼 색상 설정 (주 효과보다 더 밝게)
            Color ballColor = new Color(
                Mathf.Min(effectColor.r * 1.5f, 1f),
                Mathf.Min(effectColor.g * 1.5f, 1f),
                Mathf.Min(effectColor.b * 1.5f, 1f),
                alphaValue
            );
            ballRenderer.color = ballColor;

            // 초기 크기 설정
            ball.transform.localScale = new Vector3(energyBallSize, energyBallSize, energyBallSize);

            // 초기 위치 설정 (원형으로 배치)
            float angle = i * (360f / energyBallCount);
            float rad = angle * Mathf.Deg2Rad;
            ball.transform.localPosition = new Vector3(
                Mathf.Cos(rad) * energyBallOrbitRadius,
                Mathf.Sin(rad) * energyBallOrbitRadius,
                0f // Z값을 0으로 고정
            );

            // 리스트에 추가
            energyBalls.Add(ball);
        }
    }

    private void UpdateEnergyBalls()
    {
        if (energyBalls.Count == 0) return;

        for (int i = 0; i < energyBalls.Count; i++)
        {
            GameObject ball = energyBalls[i];
            if (ball == null) continue;

            try
            {
                // 궤도 회전 (직접 계산)
                float currentAngle = Time.time * energyBallOrbitSpeed * 360f + (i * (360f / energyBallCount));
                float rad = currentAngle * Mathf.Deg2Rad;
                
                // 무한값 체크
                float cosValue = Mathf.Cos(rad);
                float sinValue = Mathf.Sin(rad);
                
                if (float.IsFinite(cosValue) && float.IsFinite(sinValue))
                {
                    float newX = cosValue * energyBallOrbitRadius;
                    float newY = sinValue * energyBallOrbitRadius;
                    
                    if (float.IsFinite(newX) && float.IsFinite(newY))
                    {
                        ball.transform.localPosition = new Vector3(newX, newY, 0f);
                    }
                }

                // 크기 맥동 (빠른 맥동)
                float fastPulse = Mathf.Lerp(0.8f, 1.2f, (Mathf.Sin(Time.time * 8f + i) + 1) / 2);
                
                if (float.IsFinite(fastPulse))
                {
                    fastPulse = Mathf.Clamp(fastPulse, 0.5f, 1.5f); // 값을 안전한 범위로 제한
                    ball.transform.localScale = new Vector3(
                        energyBallSize * fastPulse,
                        energyBallSize * fastPulse,
                        energyBallSize
                    );
                }

                // 색상 맥동
                SpriteRenderer ballRenderer = ball.GetComponent<SpriteRenderer>();
                if (ballRenderer != null)
                {
                    // 기본 색상에서 흰색으로 맥동
                    Color baseColor = new Color(
                        Mathf.Min(effectColor.r * 1.5f, 1f),
                        Mathf.Min(effectColor.g * 1.5f, 1f),
                        Mathf.Min(effectColor.b * 1.5f, 1f)
                    );

                    float colorPulse = (Mathf.Sin(Time.time * 5f + i * 0.5f) + 1) / 2;
                    
                    if (float.IsFinite(colorPulse))
                    {
                        colorPulse = Mathf.Clamp01(colorPulse); // 0-1 사이로 제한
                        Color pulseColor = Color.Lerp(baseColor, Color.white, colorPulse);
                        ballRenderer.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, alphaValue);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"에너지 볼 업데이트 중 오류: {e.Message}");
                
                // 오류 발생 시 에너지 볼의 위치와 스케일을 안전한 값으로 리셋
                if (ball != null)
                {
                    ball.transform.localPosition = new Vector3(energyBallOrbitRadius, 0f, 0f);
                    ball.transform.localScale = Vector3.one * energyBallSize;
                }
            }
        }
    }

    // 원형 스프라이트 생성
    private Sprite CreateCircleSprite()
    {
        try
        {
            // 텍스처 생성
            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] colors = new Color[32 * 32];

            // 원 그리기
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    float distX = x - 16;
                    float distY = y - 16;
                    float dist = Mathf.Sqrt(distX * distX + distY * distY);

                    if (dist <= 15)
                    {
                        // 원 내부 - 흰색 (가장자리에서 페이드 아웃)
                        float fade = 1.0f - Mathf.Max(0, (dist - 10) / 5.0f);
                        colors[y * 32 + x] = new Color(1, 1, 1, fade);
                    }
                    else
                    {
                        // 원 외부 - 투명
                        colors[y * 32 + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(colors);
            texture.Apply();

            // 스프라이트 생성 (피벗 설정 주의)
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
        catch (System.Exception e)
        {
            Debug.LogError("원형 스프라이트 생성 중 오류 발생: " + e.Message);
            return null;
        }
    }

    // 링 스프라이트 생성
    private Sprite CreateRingSprite()
    {
        try
        {
            // 텍스처 생성
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color[] colors = new Color[64 * 64];

            // 링 그리기
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    float distX = x - 32;
                    float distY = y - 32;
                    float dist = Mathf.Sqrt(distX * distX + distY * distY);

                    if (dist >= 25 && dist <= 32)
                    {
                        // 링 테두리 - 흰색 (내부와 외부 모두에서 페이드)
                        float edge = Mathf.Abs(dist - 28.5f) / 3.5f;
                        float fade = 1.0f - edge;
                        colors[y * 64 + x] = new Color(1, 1, 1, fade);
                    }
                    else
                    {
                        // 링 외부와 내부 - 투명
                        colors[y * 64 + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(colors);
            texture.Apply();

            // 스프라이트 생성 (피벗 설정 주의)
            return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
        catch (System.Exception e)
        {
            Debug.LogError("링 스프라이트 생성 중 오류 발생: " + e.Message);
            return null;
        }
    }
    
    private void OnDestroy()
    {
        // 생성된 파티클과 에너지 볼 정리
        foreach (var particle in particles)
        {
            if (particle != null)
                Destroy(particle);
        }
        particles.Clear();
        
        foreach (var ball in energyBalls)
        {
            if (ball != null)
                Destroy(ball);
        }
        energyBalls.Clear();
        
        if (outerRingObj != null)
            Destroy(outerRingObj);
    }
}