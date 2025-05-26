using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
}