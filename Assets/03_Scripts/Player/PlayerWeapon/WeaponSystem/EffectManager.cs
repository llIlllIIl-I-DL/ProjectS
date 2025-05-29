using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EffectManager : MonoBehaviour
{
    [Header("스팀 압력 이펙트")]
    [SerializeField] private GameObject steamPressureEffectPrefab;
    [SerializeField] private string pressureBuildSoundName; // 압력 증가 사운드 이름
    [SerializeField] private string pressureReleaseSoundName; // 압력 방출 사운드 이름
    [SerializeField] private string steamHissSoundName; // 스팀 소리 이름

    private SteamPressureEffect pressureEffect;
    private GameObject player;

    private void Awake()
    {
        // 씬 전환 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // 초기화
        Initialize();
    }

    private void OnDestroy()
    {
        // 씬 전환 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Initialize();
    }

    public void Initialize()
    {
        // 플레이어 찾기
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("EffectManager: 플레이어를 찾을 수 없습니다. 나중에 다시 시도합니다.");
            return;
        }

        // 기존 이펙트 정리
        if (pressureEffect != null)
        {
            Destroy(pressureEffect.gameObject);
            pressureEffect = null;
        }

        // 스팀 압력 이펙트 생성
        CreatePressureEffect();
    }

    private void CreatePressureEffect()
    {
        if (steamPressureEffectPrefab != null && player != null)
        {
            try
            {
                // 이펙트를 플레이어의 자식으로 생성
                GameObject effectObj = Instantiate(steamPressureEffectPrefab, player.transform.position, Quaternion.identity);
                effectObj.transform.SetParent(player.transform);
                effectObj.SetActive(false); // 초기에는 비활성화 상태로 설정

                pressureEffect = effectObj.GetComponent<SteamPressureEffect>();
                if (pressureEffect == null)
                {
                    Debug.LogError("EffectManager: SteamPressureEffect 컴포넌트를 찾을 수 없습니다.");
                    Destroy(effectObj);
                    return;
                }

                Debug.Log("EffectManager: 압력 이펙트가 성공적으로 생성되었습니다.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"EffectManager: 이펙트 생성 중 오류 발생: {e.Message}");
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
        if (pressureEffect != null && pressureEffect.gameObject != null)
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
        else
        {
            Debug.LogWarning("EffectManager: pressureEffect가 null이거나 파괴되었습니다. 재초기화가 필요합니다.");
            Initialize();
        }
    }

    // 차징 레벨 변경 시 사운드 재생
    public void PlayChargeLevelSound(int level)
    {
        if (level == 1)
        {
            AudioManager.Instance.PlaySFX(pressureBuildSoundName, 0.5f);
        }
        else if (level == 2)
        {
            AudioManager.Instance.PlaySFX(pressureBuildSoundName, 0.7f);
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
        if (pressure > 0.3f)
        {
            AudioManager.Instance.PlaySFX(pressureReleaseSoundName, Mathf.Min(1.0f, pressure));
        }
    }

    // 일반 발사 사운드 재생
    public void PlayFireSound()
    {
        AudioManager.Instance.PlaySFX(steamHissSoundName, 0.5f);
    }

    // 차징샷 발사 사운드 재생
    public void PlayChargeShotSound(int chargeLevel)
    {
        if (chargeLevel == 2)
        {
            AudioManager.Instance.PlaySFX(pressureReleaseSoundName, 1.0f);
        }
        else if (chargeLevel == 1)
        {
            AudioManager.Instance.PlaySFX(pressureReleaseSoundName, 0.7f);
        }
        else
        {
            PlayFireSound(); // 차징이 안된 경우 일반 발사 사운드
        }
    }
}