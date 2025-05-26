using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class KeyRebindUI : MonoBehaviour
{
    public InputActionReference actionReference; // 인스펙터에서 연결
    public int bindingIndex = 0; // 바인딩 인덱스
    public TMP_Text actionNameText;
    public TMP_Text keyText;
    public Button rebindButton;

    private void Start()
    {
        actionNameText.text = actionReference.action.name;
        UpdateKeyText();

        rebindButton.onClick.AddListener(() => StartRebind());
    }

    void UpdateKeyText()
    {
        keyText.text = InputControlPath.ToHumanReadableString(
            actionReference.action.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }

    public void StartRebind()
    {
        keyText.text = "입력 대기중...";
        actionReference.action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse") // 마우스 제외
            .OnComplete(operation =>
            {
                operation.Dispose();
                UpdateKeyText();
                // 저장
                PlayerPrefs.SetString(actionReference.action.name + "_rebind", actionReference.action.bindings[bindingIndex].effectivePath);
                PlayerPrefs.Save();
            })
            .Start();
    }
}