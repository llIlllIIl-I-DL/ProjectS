using System.Collections;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    [Header("스팀 압력 이펙트")]
    [SerializeField] private GameObject steamPressureEffectPrefab;
    [SerializeField] private AudioClip pressureBuildSound;// 압력 증가 사운드
    [SerializeField] private AudioClip pressureReleaseSound;// 압력 방출 사운드
    [SerializeField] private AudioClip steamHissSound;// 스팀 소리

    private SteamPressureEffect pressureEffect;
    private AudioSource audioSource;
    private GameObject player;

    private void Awake()
    {
        // 오디오 소스 컴포넌트 가져오기
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = 0.7f;
            audioSource.pitch = 1.0f;
        }

        // 플레이어 찾기
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("EffectManager: 플레이어를 찾을 수 없습니다.");
        }

        // 스팀 압력 이펙트 생성
        CreatePressureEffect();
    }

    private void CreatePressureEffect()
    {
        if (steamPressureEffectPrefab != null && player != null)
        {
            // 이펙트를 플레이어의 자식으로 생성
            GameObject effectObj = Instantiate(steamPressureEffectPrefab, player.transform.position, Quaternion.identity);
            effectObj.transform.SetParent(player.transform);
            effectObj.SetActive(false); // 초기에는 비활성화 상태로 설정

            pressureEffect = effectObj.GetComponent<SteamPressureEffect>();

            // 압력 이펙트에 사운드 설정
            if (pressureEffect != null)
            {
                var effectAudio = effectObj.GetComponent<AudioSource>();
                if (effectAudio != null)
                {
                    effectAudio.clip = pressureBuildSound;
                }
            }
        }
        else
        {
            Debug.LogWarning("EffectManager: steamPressureEffectPrefab이 할당되지 않았거나 플레이어가 없어 압력 이펙트가 없습니다.");
        }
    }

    // 차징 압력 이펙트 업데이트 - 즉시 활성화
    public void UpdatePressureEffect(float pressure)
    {
        if (pressureEffect != null)
        {
            // 압력이 0보다 크면 이펙트 활성화, 아니면 비활성화
            bool shouldBeActive = pressure > 0f;
            
            if (shouldBeActive)
            {
                if (!pressureEffect.gameObject.activeSelf)
                {
                    pressureEffect.gameObject.SetActive(true);
                }
                pressureEffect.SetPressure(pressure);
            }
            else
            {
                pressureEffect.gameObject.SetActive(false);
            }
        }
    }

    // 차징 레벨 변경 시 사운드 재생
    public void PlayChargeLevelSound(int level)
    {
        if (audioSource != null && pressureBuildSound != null)
        {
            if (level == 1)
            {
                audioSource.pitch = 1.0f;
                audioSource.PlayOneShot(pressureBuildSound, 0.5f);
            }
            else if (level == 2)
            {
                audioSource.pitch = 1.2f;
                audioSource.PlayOneShot(pressureBuildSound, 0.7f);
            }
        }
    }

    // 차징 중단 및 이펙트 종료 - 즉시 처리
    public void StopPressureEffect()
    {
        if (pressureEffect != null && pressureEffect.gameObject.activeSelf)
        {
            // 압력 게이지를 0으로 설정
            pressureEffect.SetPressure(0f);
            
            // 증기 파티클 방출 효과 즉시 처리
            EmitBurstSteamParticles();
            
            // 압력 방출 사운드 재생
            PlayPressureReleaseSound(pressureEffect.GetCurrentPressure());
            
            // 이펙트 즉시 비활성화
            pressureEffect.gameObject.SetActive(false);
        }
    }

    // 압력 방출 시 증기 폭발 효과 - 즉시 처리
    private void EmitBurstSteamParticles()
    {
        if (pressureEffect != null)
        {
            int burstAmount = 5; // 기본 증기 파티클 수 (즉시 처리이므로 적게 설정)
            float currentPressure = pressureEffect.GetCurrentPressure();
            burstAmount = Mathf.RoundToInt(burstAmount * (1f + currentPressure));

            // 한번에 모든 파티클 방출
            for (int i = 0; i < burstAmount; i++)
            {
                pressureEffect.EmitSteamParticleExternal();
            }
        }
    }

    // 압력 방출 사운드 재생
    private void PlayPressureReleaseSound(float pressure)
    {
        if (audioSource != null && pressureReleaseSound != null && pressure > 0.3f)
        {
            audioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
            audioSource.PlayOneShot(pressureReleaseSound, Mathf.Min(1.0f, pressure));
        }
    }

    // 일반 발사 사운드 재생
    public void PlayFireSound()
    {
        if (audioSource != null && steamHissSound != null)
        {
            audioSource.pitch = 1.2f;
            audioSource.PlayOneShot(steamHissSound, 0.5f);
        }
    }

    // 차징샷 발사 사운드 재생
    public void PlayChargeShotSound(int chargeLevel)
    {
        if (audioSource != null && pressureReleaseSound != null)
        {
            if (chargeLevel == 2)
            {
                audioSource.pitch = 0.8f;
                audioSource.PlayOneShot(pressureReleaseSound, 1.0f);
            }
            else if (chargeLevel == 1)
            {
                audioSource.pitch = 1.0f;
                audioSource.PlayOneShot(pressureReleaseSound, 0.7f);
            }
            else
            {
                PlayFireSound(); // 차징이 안된 경우 일반 발사 사운드
            }
        }
    }
}