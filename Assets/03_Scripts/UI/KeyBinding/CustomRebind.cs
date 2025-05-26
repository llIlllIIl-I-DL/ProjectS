using UnityEngine;
using UnityEngine.InputSystem;

public class CustomRebind : MonoBehaviour
{
    public InputActionAsset inputActions; // 인스펙터에서 연결

    // 액션 이름과 바인딩 인덱스를 지정해서 리바인드 시작
    public void StartRebind(string actionName, int bindingIndex)
    {
        var action = inputActions.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError("액션을 찾을 수 없습니다: " + actionName);
            return;
        }

        action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse") // 마우스 제외 등 옵션
            .OnComplete(operation =>
            {
                operation.Dispose();
                Debug.Log($"{actionName} 리바인드 완료: {action.bindings[bindingIndex].effectivePath}");
                // 저장
                PlayerPrefs.SetString(actionName + "_rebind", action.bindings[bindingIndex].effectivePath);
                PlayerPrefs.Save();
            })
            .Start();
    }

    // 저장된 바인딩 불러오기
    public void LoadRebind(string actionName, int bindingIndex)
    {
        var action = inputActions.FindAction(actionName);
        if (action == null) return;

        string savedPath = PlayerPrefs.GetString(actionName + "_rebind", "");
        if (!string.IsNullOrEmpty(savedPath))
        {
            action.ApplyBindingOverride(bindingIndex, savedPath);
        }
    }
}