using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CustomRebind : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        
        // InputManager가 없으면 생성
        if (InputManager.Instance == null)
        {
            GameObject inputManagerObj = new GameObject("InputManager");
            inputManagerObj.AddComponent<InputManager>();
        }
        
        ApplyAllSavedBindings();
    }

    void Start()
    {
        FindObjectOfType<CustomRebind>()?.ApplyAllSavedBindings();
    }

    // 모든 액션의 저장된 바인딩을 PlayerPrefs에서 불러와 적용
    public void ApplyAllSavedBindings()
    {
        if (InputManager.Instance == null || InputManager.Instance.playerInput == null)
        {
            Debug.LogError("InputManager.Instance 또는 playerInput이 null입니다.");
            return;
        }

        var inputActions = InputManager.Instance.playerInput.asset;
        foreach (var map in inputActions.actionMaps)
        {
            foreach (var action in map.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    string key = action.name + "_rebind";
                    string savedPath = PlayerPrefs.GetString(key, "");
                    if (!string.IsNullOrEmpty(savedPath))
                    {
                        action.ApplyBindingOverride(i, savedPath);
                    }
                }
            }
        }
    }

    // 액션 이름과 바인딩 인덱스를 지정해서 리바인드 시작
    public void StartRebind(string actionName, int bindingIndex)
    {
        if (InputManager.Instance == null || InputManager.Instance.playerInput == null)
        {
            Debug.LogError("InputManager.Instance 또는 playerInput이 null입니다.");
            return;
        }

        var inputActions = InputManager.Instance.playerInput.asset;
        var action = inputActions.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError("액션을 찾을 수 없습니다: " + actionName);
            return;
        }

        // 1. 리바인드 전에 Disable
        action.Disable();

        action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse") // 마우스 제외 등 옵션
            .OnComplete(operation =>
            {
                operation.Dispose();

                // 2. 리바인드 끝나면 Enable
                action.Enable();

                Debug.Log($"{actionName} 리바인드 완료: {action.bindings[bindingIndex].effectivePath}");
                // 저장
                PlayerPrefs.SetString(actionName + "_rebind", action.bindings[bindingIndex].effectivePath);
                PlayerPrefs.Save();
            })
            .Start();
    }

    // 저장된 바인딩 불러오기 (개별)
    public void LoadRebind(string actionName, int bindingIndex)
    {
        if (InputManager.Instance == null || InputManager.Instance.playerInput == null)
        {
            Debug.LogError("InputManager.Instance 또는 playerInput이 null입니다.");
            return;
        }

        var inputActions = InputManager.Instance.playerInput.asset;
        var action = inputActions.FindAction(actionName);
        if (action == null) return;

        string savedPath = PlayerPrefs.GetString(actionName + "_rebind", "");
        if (!string.IsNullOrEmpty(savedPath))
        {
            action.ApplyBindingOverride(bindingIndex, savedPath);
        }
    }
}
