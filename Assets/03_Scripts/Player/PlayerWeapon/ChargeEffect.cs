using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargingEffect : MonoBehaviour
{
    [Header("이펙트 설정")]
    [SerializeField] private float rotationSpeed = 90f;         // 회전 속도(초당 각도)
    [SerializeField] private float pulseSpeed = 2f;             // 맥동 속도
    [SerializeField] private float pulseMinScale = 0.8f;        // 최소 스케일
    [SerializeField] private float pulseMaxScale = 1.2f;        // 최대 스케일
    [SerializeField] private Color effectColor = Color.cyan;    // 이펙트 색상
    [SerializeField] private float alphaValue = 0.7f;           // 투명도

    [Header("파티클 설정")]
    [SerializeField] private bool useParticles = true;          // 파티클 사용 여부
    [SerializeField] private int particleCount = 8;             // 파티클 개수
    [SerializeField] private float particleSpeed = 1f;          // 파티클 속도
    [SerializeField] private float particleSize = 0.1f;         // 파티클 크기

    private SpriteRenderer spriteRenderer;
    private List<GameObject> particles = new List<GameObject>();
    private Transform playerTransform;
    private float initialScale;
    private Sprite circleSprite = null;

    private void Awake()
    {
        // 스프라이트 미리 생성
        circleSprite = CreateCircleSprite();
    }

    private void Start()
    {
        // 스프라이트 렌더러 설정
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = circleSprite;
        }

        // 색상 및 투명도 설정
        spriteRenderer.color = new Color(effectColor.r, effectColor.g, effectColor.b, alphaValue);

        // 초기 스케일 저장
        initialScale = transform.localScale.x;

        // 파티클 생성
        if (useParticles)
        {
            CreateParticles();
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

        // 파티클 업데이트
        if (useParticles)
        {
            UpdateParticles();
        }

        // 플레이어를 따라가도록 설정
        if (playerTransform != null)
        {
            // 플레이어 위치가 유효한지 확인
            if (float.IsFinite(playerTransform.position.x) && float.IsFinite(playerTransform.position.y))
            {
                transform.position = playerTransform.position;
            }
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
            particleRenderer.color = new Color(effectColor.r, effectColor.g, effectColor.b, alphaValue * 0.7f);

            // 크기 설정
            particle.transform.localScale = new Vector3(particleSize, particleSize, particleSize);

            // 초기 위치 설정 (원형으로 배치)
            float angle = i * (360f / particleCount);
            float rad = angle * Mathf.Deg2Rad;
            float distance = 0.5f;
            particle.transform.localPosition = new Vector3(Mathf.Cos(rad) * distance, Mathf.Sin(rad) * distance, 0);

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

            // 회전 애니메이션 (RotateAround 대신 직접 계산)
            float angle = ((i % 2 == 0) ? 1 : -1) * rotationSpeed * Time.deltaTime * particleSpeed;
            
            if (float.IsFinite(angle)) // 각도가 유효한지 확인
            {
                // 현재 위치 가져오기
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
                    particle.transform.localPosition = new Vector3(newX, newY, currentLocalPos.z);
                }
            }

            // 맥동 애니메이션 (파티클마다 다른 위상)
            float phase = (float)i / particleCount * Mathf.PI;
            float pulse = Mathf.Lerp(pulseMinScale, pulseMaxScale, (Mathf.Sin(Time.time * pulseSpeed + phase) + 1) / 2);
            
            // 무한값 체크
            if (float.IsFinite(pulse))
            {
                pulse = Mathf.Clamp(pulse, 0.1f, 2.0f); // 값을 안전한 범위로 제한
                particle.transform.localScale = new Vector3(particleSize * pulse, particleSize * pulse, particleSize);
            }
        }
    }

    // 원형 스프라이트 생성 (스프라이트가 없는 경우 사용)
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

                    if (dist <= 16)
                    {
                        // 원 내부 - 흰색
                        colors[y * 32 + x] = Color.white;
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

            // 스프라이트 생성
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }
        catch (System.Exception e)
        {
            Debug.LogError("원형 스프라이트 생성 중 오류 발생: " + e.Message);
            return null;
        }
    }

    private void OnDestroy()
    {
        // 생성된 파티클 정리
        foreach (var particle in particles)
        {
            if (particle != null)
                Destroy(particle);
        }
        particles.Clear();
    }
}