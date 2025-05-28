using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SettingUI : MonoBehaviour
{
    [Header("해상도")]
    public TMP_Dropdown resolutionDropdown;
    [Header("밝기")]
    public Slider gammaSlider;
    [Header("전체화면")]
    public Toggle fullscreenToggle;
    [Header("BGM")]
    public Slider bgmSlider;
    [Header("효과음")]
    public Slider seSlider;
    [Header("키설정")]
    public Button keySettingButton;
    public GameObject keySettingPanel;

    void Start()
    {
        // 해상도 설정
        resolutionDropdown.onValueChanged.AddListener(SetResolution);

        // 밝기 설정
        gammaSlider.onValueChanged.AddListener(SetGamma);

        // 전체화면 설정
        fullscreenToggle.onValueChanged.AddListener(SetFullScreen);

        // BGM/SE는 사운드매니저 연동 전까지 비활성화
        bgmSlider.interactable = false;
        seSlider.interactable = false;

        // 키설정 버튼
        keySettingButton.onClick.AddListener(OpenKeySetting);

        // 씬이 바뀌어도 UI를 동기화
        UpdateUI();
    }

    void SetResolution(int index)
    {
        // 해상도 변경 로직
        // 예시: Screen.SetResolution(width, height, fullscreenToggle.isOn);
    }

    void SetGamma(float value)
    {
        // 밝기 변경 로직 (포스트 프로세싱 등과 연동)
    }

    void SetFullScreen(bool isFull)
    {
        Screen.fullScreen = isFull;
    }

    void OpenKeySetting()
    {
        keySettingPanel.SetActive(true);
    }

    // 사운드 매니저 연동용 메서드(추후 구현)
    public void SetBGMVolume(float value)
    {
        // SoundManager.Instance.SetBGMVolume(value);
    }

    public void SetSEVolume(float value)
    {
        // SoundManager.Instance.SetSEVolume(value);
    }

    public void UpdateUI()
    {
        // 전체화면 상태 동기화
        fullscreenToggle.isOn = Screen.fullScreen;
        // 해상도 드롭다운 동기화 (예시, 실제 구현은 프로젝트에 맞게 수정)
        // 현재 해상도와 일치하는 인덱스를 찾아서 설정
        for (int i = 0; i < resolutionDropdown.options.Count; i++)
        {
            var option = resolutionDropdown.options[i].text;
            var current = Screen.currentResolution;
            string currentRes = $"{current.width} x {current.height}";
            if (option.Contains(currentRes))
            {
                resolutionDropdown.value = i;
                break;
            }
        }
        // 밝기 슬라이더 동기화 (예시, 실제 밝기 값은 별도 저장 필요)
        // gammaSlider.value = ...;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateUI();
    }
}