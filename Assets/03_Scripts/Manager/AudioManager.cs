using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;

// ManagerBase를 상속하여 기본 관리 기능 추가
public class AudioManager : Singleton<AudioManager>
{
    // 초기화 상태
    public bool IsInitialized { get; private set; } = false;

    // 오디오 믹서 그룹
    public AudioMixerGroup BGMGroup => bgmMixerGroup;
    public AudioMixerGroup SFXGroup => sfxMixerGroup;
    public AudioMixerGroup UIGroup => uiMixerGroup;
    public AudioMixerGroup VoiceGroup => voiceMixerGroup;

    // 오디오 카테고리 정의
    public enum AudioType
    {
        BGM,
        SFX,
        UI,
        Voice
    }

    [Header("오디오 믹서 설정")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup bgmMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup uiMixerGroup;
    [SerializeField] private AudioMixerGroup voiceMixerGroup;

    [Header("오디오 설정")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.7f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float uiVolume = 1.0f;
    [Range(0f, 1f)]
    [SerializeField] private float voiceVolume = 1.0f;

    private bool[] isMuted = new bool[4]; // BGM, SFX, UI, Voice 순서

    // 오디오 소스 풀
    private AudioSource bgmSource;
    private List<AudioSource> sfxSources = new List<AudioSource>();
    private List<AudioSource> uiSources = new List<AudioSource>();
    private List<AudioSource> voiceSources = new List<AudioSource>();

    // 오디오 딕셔너리 (오디오 클립 캐싱)
    private Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();

    private const int MAX_SFX_SOURCES = 10;
    private const int MAX_UI_SOURCES = 5;
    private const int MAX_VOICE_SOURCES = 3;

    protected override void Awake()
    {
        // 초기화
        InitializeAudioSources();
        IsInitialized = true;
    }

    // 기존 Start는 Initialize로 이동
    private void Start()
    {
        // 이미 Initialize에서 처리했다면 건너뛰기
        if (!IsInitialized)
        {
            UpdateMixerVolumes();
            LoadVolumeSettings();
        }
    }

    // 오디오 소스 초기화 및 풀 생성
    private void InitializeAudioSources()
    {
        // 배경음악용 오디오 소스
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        // SFX 오디오 소스 풀
        CreateAudioSources(MAX_SFX_SOURCES, sfxSources, sfxMixerGroup);

        // UI 오디오 소스 풀
        CreateAudioSources(MAX_UI_SOURCES, uiSources, uiMixerGroup);

        // 음성 오디오 소스 풀
        CreateAudioSources(MAX_VOICE_SOURCES, voiceSources, voiceMixerGroup);
    }

    // 오디오 소스 풀 생성 헬퍼 메서드
    private void CreateAudioSources(int count, List<AudioSource> sourcesList, AudioMixerGroup mixerGroup)
    {
        for (int i = 0; i < count; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = mixerGroup;
            source.playOnAwake = false;
            sourcesList.Add(source);
        }
    }

    // 믹서 볼륨 업데이트
    private void UpdateMixerVolumes()
    {
        SetMixerVolume("MasterVolume", masterVolume);
        SetMixerVolume("BGMVolume", isMuted[0] ? 0 : bgmVolume);
        SetMixerVolume("SFXVolume", isMuted[1] ? 0 : sfxVolume);
        SetMixerVolume("UIVolume", isMuted[2] ? 0 : uiVolume);
        SetMixerVolume("VoiceVolume", isMuted[3] ? 0 : voiceVolume);
    }

    // 믹서 볼륨 설정 헬퍼 메서드
    private void SetMixerVolume(string paramName, float volume)
    {
        // 오디오 믹서는 로그 스케일 사용
        audioMixer.SetFloat(paramName, Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20f);
    }

    #region 공개 API

    // 배경음악 재생
    public void PlayBGM(string clipName, float fadeInDuration = 1.0f)
    {
        AudioClip clip = GetAudioClip(clipName);
        if (clip == null) return;

        StartCoroutine(FadeBGM(clip, fadeInDuration));
    }

    // 효과음 재생
    public AudioSource PlaySFX(string clipName, float volume = 1.0f, bool loop = false)
    {
        return PlayAudio(clipName, AudioType.SFX, volume, loop);
    }

    // UI 사운드 재생
    public AudioSource PlayUISound(string clipName, float volume = 1.0f)
    {
        return PlayAudio(clipName, AudioType.UI, volume);
    }

    // 음성 재생
    public AudioSource PlayVoice(string clipName, float volume = 1.0f)
    {
        return PlayAudio(clipName, AudioType.Voice, volume);
    }

    // 모든 사운드 중지
    public void StopAllSounds()
    {
        bgmSource.Stop();

        StopAllSourcesInList(sfxSources);
        StopAllSourcesInList(uiSources);
        StopAllSourcesInList(voiceSources);
    }

    // 특정 타입의 모든 사운드 중지
    public void StopAllOfType(AudioType audioType)
    {
        switch (audioType)
        {
            case AudioType.BGM:
                bgmSource.Stop();
                break;
            case AudioType.SFX:
                StopAllSourcesInList(sfxSources);
                break;
            case AudioType.UI:
                StopAllSourcesInList(uiSources);
                break;
            case AudioType.Voice:
                StopAllSourcesInList(voiceSources);
                break;
        }
    }

    // 볼륨 설정
    public void SetVolume(AudioType audioType, float volume)
    {
        volume = Mathf.Clamp01(volume);

        switch (audioType)
        {
            case AudioType.BGM:
                bgmVolume = volume;
                SetMixerVolume("BGMVolume", isMuted[0] ? 0 : bgmVolume);
                break;
            case AudioType.SFX:
                sfxVolume = volume;
                SetMixerVolume("SFXVolume", isMuted[1] ? 0 : sfxVolume);
                break;
            case AudioType.UI:
                uiVolume = volume;
                SetMixerVolume("UIVolume", isMuted[2] ? 0 : uiVolume);
                break;
            case AudioType.Voice:
                voiceVolume = volume;
                SetMixerVolume("VoiceVolume", isMuted[3] ? 0 : voiceVolume);
                break;
        }

        SaveVolumeSettings();
    }

    // 마스터 볼륨 설정
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        SetMixerVolume("MasterVolume", masterVolume);
        SaveVolumeSettings();
    }

    // 음소거 설정
    public void SetMute(AudioType audioType, bool mute)
    {
        int index = (int)audioType;
        if (index >= 0 && index < isMuted.Length)
        {
            isMuted[index] = mute;

            // 해당 채널 볼륨 업데이트
            switch (audioType)
            {
                case AudioType.BGM:
                    SetMixerVolume("BGMVolume", mute ? 0 : bgmVolume);
                    break;
                case AudioType.SFX:
                    SetMixerVolume("SFXVolume", mute ? 0 : sfxVolume);
                    break;
                case AudioType.UI:
                    SetMixerVolume("UIVolume", mute ? 0 : uiVolume);
                    break;
                case AudioType.Voice:
                    SetMixerVolume("VoiceVolume", mute ? 0 : voiceVolume);
                    break;
            }
        }
    }

    #endregion

    #region 유틸리티 메서드

    // 지정된 타입에 맞는 오디오 소스 가져오기
    private AudioSource GetAvailableSource(AudioType audioType)
    {
        List<AudioSource> sourcesList;

        switch (audioType)
        {
            case AudioType.SFX:
                sourcesList = sfxSources;
                break;
            case AudioType.UI:
                sourcesList = uiSources;
                break;
            case AudioType.Voice:
                sourcesList = voiceSources;
                break;
            default:
                return null;
        }

        // 비활성 상태인 소스 찾기
        foreach (AudioSource source in sourcesList)
        {
            if (!source.isPlaying)
                return source;
        }

        // 모든 소스가 사용 중이면 첫 번째 소스 재사용
        //Debug.LogWarning($"모든 {audioType} 오디오 소스가 사용 중입니다. 가장 오래된 소스를 재사용합니다.");
        AudioSource oldestSource = sourcesList[0];
        oldestSource.Stop();
        return oldestSource;
    }

    // 오디오 클립 로드 및 캐싱
    private AudioClip GetAudioClip(string clipName)
    {
        // 딕셔너리에 있는지 확인
        if (audioClips.TryGetValue(clipName, out AudioClip clip))
            return clip;

        // 없으면 Resources 폴더에서 로드
        clip = Resources.Load<AudioClip>($"Audio/{clipName}");

        if (clip == null)
        {
            Debug.LogWarning($"오디오 클립을 찾을 수 없습니다: {clipName}");
            return null;
        }

        // 캐싱
        audioClips[clipName] = clip;
        return clip;
    }

    // 오디오 재생 공통 메서드
    private AudioSource PlayAudio(string clipName, AudioType audioType, float volume = 1.0f, bool loop = false)
    {
        AudioClip clip = GetAudioClip(clipName);
        if (clip == null) return null;

        AudioSource source = GetAvailableSource(audioType);
        if (source == null) return null;

        source.clip = clip;
        source.volume = volume;
        source.loop = loop;
        source.Play();

        return source;
    }

    // BGM 페이드 인/아웃 처리
    private IEnumerator FadeBGM(AudioClip newClip, float fadeInDuration)
    {
        // 현재 BGM이 재생 중이면 페이드 아웃
        if (bgmSource.isPlaying)
        {
            float startVolume = bgmSource.volume;
            float fadeOutDuration = 1.0f;

            for (float t = 0; t < fadeOutDuration; t += Time.deltaTime)
            {
                bgmSource.volume = Mathf.Lerp(startVolume, 0, t / fadeOutDuration);
                yield return null;
            }

            bgmSource.Stop();
        }

        // 새 BGM 설정 및 페이드 인
        bgmSource.volume = 0;
        bgmSource.clip = newClip;
        bgmSource.Play();

        for (float t = 0; t < fadeInDuration; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0, bgmVolume, t / fadeInDuration);
            yield return null;
        }

        bgmSource.volume = bgmVolume;
    }

    // 리스트의 모든 오디오 소스 중지
    private void StopAllSourcesInList(List<AudioSource> sourcesList)
    {
        foreach (AudioSource source in sourcesList)
        {
            if (source.isPlaying)
                source.Stop();
        }
    }

    // 볼륨 설정 저장
    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("UIVolume", uiVolume);
        PlayerPrefs.SetFloat("VoiceVolume", voiceVolume);
        PlayerPrefs.Save();
    }

    // 볼륨 설정 로드
    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", masterVolume);
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", bgmVolume);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", sfxVolume);
        uiVolume = PlayerPrefs.GetFloat("UIVolume", uiVolume);
        voiceVolume = PlayerPrefs.GetFloat("VoiceVolume", voiceVolume);

        UpdateMixerVolumes();
    }

    #endregion
}