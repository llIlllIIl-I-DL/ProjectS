using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class KeyRebindUI : MonoBehaviour
{
    public enum PlayerAction
    {
        Move,
        Dash,
        Jump,
        Attack,
        Prev,
        Next,
        SpecialAttack,
        Interaction,
        Inventory,
        PauseMenu,
        Map
    }

    [SerializeField] private PlayerAction actionType; // enum으로 변경
    public int bindingIndex = 0;
    public TMP_Text actionNameText;
    public TMP_Text keyText;
    public Button rebindButton;
    public Button resetButton;

    private string ActionName => actionType.ToString();

    private InputAction GetAction()
    {
        if (InputManager.Instance == null || InputManager.Instance.playerInput == null)
        {
            Debug.LogError("InputManager.Instance 또는 playerInput이 null입니다.");
            return null;
        }
        return InputManager.Instance.playerInput.asset.FindAction(ActionName);
    }

    private void Awake()
    {
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetToDefault);
    }

    private void Start()
    {
        if (actionNameText != null)
            actionNameText.text = ActionName;
            
        LoadRebind();
        UpdateKeyText();
        
        if (rebindButton != null)
            rebindButton.onClick.AddListener(() => StartRebind());
    }

    void UpdateKeyText()
    {
        keyText.text = InputControlPath.ToHumanReadableString(
            GetAction().bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }

    public void StartRebind()
    {
        keyText.text = "입력 대기중...";
        GetAction().PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse") // 마우스 제외
            .OnComplete(operation =>
            {
                operation.Dispose();
                UpdateKeyText();
                // 저장
                PlayerPrefs.SetString(ActionName + "_rebind", GetAction().bindings[bindingIndex].effectivePath);
                PlayerPrefs.Save();
            })
            .Start();
    }

    private void LoadRebind()
    {
        if (PlayerPrefs.HasKey(ActionName + "_rebind"))
        {
            string rebind = PlayerPrefs.GetString(ActionName + "_rebind");
            GetAction().ApplyBindingOverride(new InputBinding { overridePath = rebind });
        }
    }

    private void ResetToDefault()
    {
        GetAction().RemoveAllBindingOverrides();
        UpdateKeyText();
    }
}