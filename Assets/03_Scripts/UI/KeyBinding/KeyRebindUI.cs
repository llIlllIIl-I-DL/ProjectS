using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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

    // Move 액션의 방향을 지정하는 enum 추가
    public enum MoveDirection
    {
        None,  // Move가 아닌 액션들을 위함
        Up,
        Down,
        Left,
        Right
    }

    [SerializeField] private PlayerAction actionType;
    [SerializeField] private MoveDirection moveDirection = MoveDirection.None; // Move 액션일 때만 사용
    public int bindingIndex = 0;
    public TMP_Text actionNameText;
    public TMP_Text keyText;
    public Button rebindButton;
    public Button resetButton;
    [SerializeField] private TMP_Text errorText; // 에러 메시지를 표시할 텍스트 컴포넌트

    private string ActionName => actionType.ToString();
    private bool IsVectorAction => actionType == PlayerAction.Move;

    private InputAction GetAction()
    {
        if (InputManager.Instance == null || InputManager.Instance.playerInput == null)
        {
            Debug.LogError("InputManager.Instance 또는 playerInput이 null입니다.");
            return null;
        }
        return InputManager.Instance.playerInput.asset.FindAction(ActionName);
    }

    // Vector 액션에서 특정 방향의 바인딩 인덱스를 찾는 메서드
    private int GetMoveBindingIndex()
    {
        if (!IsVectorAction || moveDirection == MoveDirection.None)
            return bindingIndex;

        var action = GetAction();
        if (action == null) return -1;

        string targetDirection = moveDirection.ToString().ToLower();

        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (action.bindings[i].isComposite) continue;
            
            string bindingName = action.bindings[i].name;
            if (string.IsNullOrEmpty(bindingName)) continue;

            if (bindingName.ToLower() == targetDirection)
            {
                return i;
            }
        }
        return -1;
    }

    private void Awake()
    {
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetToDefault);
    }

    private void Start()
    {
        SetupUI();
        LoadRebind();
        UpdateKeyText();
        
        if (rebindButton != null)
            rebindButton.onClick.AddListener(() => StartRebind());
            
        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }

    private void SetupUI()
    {
        if (actionNameText != null)
        {
            if (IsVectorAction && moveDirection != MoveDirection.None)
            {
                actionNameText.text = $"{ActionName} ({moveDirection})";
            }
            else
            {
                actionNameText.text = ActionName;
            }
        }
    }

    void UpdateKeyText()
    {
        var action = GetAction();
        if (action == null) return;

        int currentBindingIndex = IsVectorAction ? GetMoveBindingIndex() : bindingIndex;
        
        if (currentBindingIndex == -1)
        {
            keyText.text = "바인딩 없음";
            return;
        }

        keyText.text = InputControlPath.ToHumanReadableString(
            action.bindings[currentBindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }

    public void StartRebind()
    {
        var action = GetAction();
        if (action == null) return;

        int currentBindingIndex = IsVectorAction ? GetMoveBindingIndex() : bindingIndex;
        if (currentBindingIndex == -1)
        {
            ShowError($"바인딩 인덱스를 찾을 수 없습니다: {ActionName} {moveDirection}");
            return;
        }

        keyText.text = "입력 대기중...";
        if (errorText != null)
            errorText.gameObject.SetActive(false);

        action.Disable();

        var customRebind = FindObjectOfType<CustomRebind>();
        if (customRebind == null)
        {
            ShowError("CustomRebind 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        action.PerformInteractiveRebinding(currentBindingIndex)
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnPotentialMatch(operation =>
            {
                HashSet<string> usedPaths = new HashSet<string>();

                if (IsVectorAction)
                {
                    // Move 액션: Move의 다른 방향만 중복 체크
                    var moveAction = GetAction();
                    for (int i = 0; i < moveAction.bindings.Count; i++)
                    {
                        if (i == currentBindingIndex) continue;
                        if (moveAction.bindings[i].isComposite) continue;
                        string path = moveAction.bindings[i].effectivePath;
                        if (!string.IsNullOrEmpty(path))
                            usedPaths.Add(path);
                    }
                }
                else
                {
                    // 일반 액션: Move가 아닌 모든 액션의 바인딩만 중복 체크
                    var inputActions = InputManager.Instance.playerInput.asset;
                    foreach (var map in inputActions.actionMaps)
                    {
                        foreach (var act in map.actions)
                        {
                            if (act.name == "Move") continue; // Move는 제외
                            if (act.name == ActionName) continue; // 자기 자신은 아래에서 제외
                            for (int i = 0; i < act.bindings.Count; i++)
                            {
                                if (act.bindings[i].isComposite) continue;
                                string path = act.bindings[i].effectivePath;
                                if (!string.IsNullOrEmpty(path))
                                    usedPaths.Add(path);
                            }
                        }
                    }
                    // 자기 자신의 다른 바인딩만 제외
                    var currentActionPaths = customRebind.GetActionBindingPaths(ActionName, currentBindingIndex);
                    usedPaths.UnionWith(currentActionPaths);
                }

                string newPath = operation.candidates[0].path;
                if (usedPaths.Contains(newPath))
                {
                    ShowError("이미 사용 중인 키입니다!");
                    operation.Cancel();
                    operation.Dispose();
                    action.Enable();
                    UpdateKeyText();
                }
            })
            .OnComplete(operation =>
            {
                HashSet<string> usedPaths = new HashSet<string>();

                if (IsVectorAction)
                {
                    var moveAction = GetAction();
                    for (int i = 0; i < moveAction.bindings.Count; i++)
                    {
                        if (i == currentBindingIndex) continue;
                        if (moveAction.bindings[i].isComposite) continue;
                        string path = moveAction.bindings[i].effectivePath;
                        if (!string.IsNullOrEmpty(path))
                            usedPaths.Add(path);
                    }
                }
                else
                {
                    var inputActions = InputManager.Instance.playerInput.asset;
                    foreach (var map in inputActions.actionMaps)
                    {
                        foreach (var act in map.actions)
                        {
                            if (act.name == "Move") continue;
                            if (act.name == ActionName) continue;
                            for (int i = 0; i < act.bindings.Count; i++)
                            {
                                if (act.bindings[i].isComposite) continue;
                                string path = act.bindings[i].effectivePath;
                                if (!string.IsNullOrEmpty(path))
                                    usedPaths.Add(path);
                            }
                        }
                    }
                    var currentActionPaths = customRebind.GetActionBindingPaths(ActionName, currentBindingIndex);
                    usedPaths.UnionWith(currentActionPaths);
                }

                string newPath = action.bindings[currentBindingIndex].effectivePath;
                if (usedPaths.Contains(newPath))
                {
                    ShowError("이미 사용 중인 키입니다!");
                    action.RemoveBindingOverride(currentBindingIndex);
                    UpdateKeyText();
                }
                else
                {
                    UpdateKeyText();
                    SaveRebind(currentBindingIndex);
                }
                action.Enable();
                operation.Dispose();
            })
            .OnCancel(operation =>
            {
                operation.Dispose();
                UpdateKeyText();
                action.Enable();
            })
            .Start();
    }

    private void SaveRebind(int currentBindingIndex)
    {
        var action = GetAction();
        if (action == null) return;

        string saveKey;
        if (IsVectorAction && moveDirection != MoveDirection.None)
        {
            saveKey = $"{ActionName}_{moveDirection}_rebind";
        }
        else
        {
            saveKey = $"{ActionName}_rebind";
        }

        PlayerPrefs.SetString(saveKey, action.bindings[currentBindingIndex].effectivePath);
        PlayerPrefs.Save();
    }

    private void LoadRebind()
    {
        string saveKey;
        if (IsVectorAction && moveDirection != MoveDirection.None)
        {
            saveKey = $"{ActionName}_{moveDirection}_rebind";
        }
        else
        {
            saveKey = $"{ActionName}_rebind";
        }

        if (PlayerPrefs.HasKey(saveKey))
        {
            string rebind = PlayerPrefs.GetString(saveKey);
            int currentBindingIndex = IsVectorAction ? GetMoveBindingIndex() : bindingIndex;
            
            if (currentBindingIndex != -1)
            {
                GetAction().ApplyBindingOverride(currentBindingIndex, rebind);
            }
        }
    }

    private void ResetToDefault()
    {
        var action = GetAction();
        if (action == null) return;

        if (IsVectorAction && moveDirection != MoveDirection.None)
        {
            // 특정 방향만 리셋
            int currentBindingIndex = GetMoveBindingIndex();
            if (currentBindingIndex != -1)
            {
                action.RemoveBindingOverride(currentBindingIndex);
            }
            
            string saveKey = $"{ActionName}_{moveDirection}_rebind";
            PlayerPrefs.DeleteKey(saveKey);
        }
        else
        {
            // 전체 액션 리셋
            action.RemoveAllBindingOverrides();
            
            string saveKey = $"{ActionName}_rebind";
            PlayerPrefs.DeleteKey(saveKey);
        }
        
        PlayerPrefs.Save();
        UpdateKeyText();
    }

    private void ShowError(string message)
    {
        Debug.Log(message);
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }
    }

    private void HideError()
    {
        if (errorText != null)
        {
            errorText.gameObject.SetActive(false);
        }
    }
}