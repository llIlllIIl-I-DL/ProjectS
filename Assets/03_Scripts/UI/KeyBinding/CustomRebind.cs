using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

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
            Debug.LogWarning("InputManager.Instance 또는 playerInput이 null입니다.");
            return;
        }

        var inputActions = InputManager.Instance.playerInput.asset;
        foreach (var map in inputActions.actionMaps)
        {
            foreach (var action in map.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    string key = $"{action.name}_binding_{i}";
                    string savedPath = PlayerPrefs.GetString(key, "");
                    if (!string.IsNullOrEmpty(savedPath))
                    {
                        action.ApplyBindingOverride(i, savedPath);
                    }
                }
            }
        }
    }

    // 현재 사용 중인 모든 바인딩 경로를 가져오는 메서드
    public HashSet<string> GetAllCurrentBindingPaths()
    {
        var usedPaths = new HashSet<string>();
        
        if (InputManager.Instance == null || InputManager.Instance.playerInput == null)
            return usedPaths;

        var inputActions = InputManager.Instance.playerInput.asset;
        foreach (var map in inputActions.actionMaps)
        {
            foreach (var action in map.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    if (action.bindings[i].isComposite) continue;
                    
                    string path = action.bindings[i].effectivePath;
                    if (!string.IsNullOrEmpty(path))
                    {
                        usedPaths.Add(path);
                    }
                }
            }
        }
        
        return usedPaths;
    }

    // 특정 액션의 현재 바인딩 경로를 가져오는 메서드
    public HashSet<string> GetActionBindingPaths(string actionName, int excludeBindingIndex = -1)
    {
        var paths = new HashSet<string>();
        
        if (InputManager.Instance == null || InputManager.Instance.playerInput == null)
            return paths;

        var action = InputManager.Instance.playerInput.asset.FindAction(actionName);
        if (action == null) return paths;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (i == excludeBindingIndex) continue;
            if (action.bindings[i].isComposite) continue;
            
            string path = action.bindings[i].effectivePath;
            if (!string.IsNullOrEmpty(path))
            {
                paths.Add(path);
            }
        }
        
        return paths;
    }

    // 일반 액션 리바인드 (기존 방식)
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

        // 리바인드 전에 Disable
        action.Disable();

        // 현재 사용 중인 모든 바인딩 경로 가져오기
        var usedPaths = GetAllCurrentBindingPaths();
        // 현재 액션의 다른 바인딩은 제외
        var currentActionPaths = GetActionBindingPaths(actionName, bindingIndex);
        usedPaths.ExceptWith(currentActionPaths);

        var rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse");

        // 사용 중인 키 제외
        foreach (var path in usedPaths)
        {
            rebindOperation.WithControlsExcluding(path);
        }

        rebindOperation.OnComplete(operation =>
            {
                operation.Dispose();
                action.Enable();

                Debug.Log($"{actionName} 리바인드 완료: {action.bindings[bindingIndex].effectivePath}");
                
                // 저장
                string key = $"{actionName}_binding_{bindingIndex}";
                PlayerPrefs.SetString(key, action.bindings[bindingIndex].effectivePath);
                PlayerPrefs.Save();
            })
            .Start();
    }

    // Vector2 Move 액션의 특정 방향 리바인드
    public void StartMoveRebind(string actionName, string direction)
    {
        if (InputManager.Instance == null || InputManager.Instance.playerInput == null)
        {
            Debug.Log("InputManager.Instance 또는 playerInput이 null입니다.");
            return;
        }

        var action = InputManager.Instance.playerInput.asset.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError("액션을 찾을 수 없습니다: " + actionName);
            return;
        }

        // 2D Vector Composite에서 특정 방향의 바인딩 인덱스 찾기
        int bindingIndex = GetMoveBindingIndex(action, direction);
        if (bindingIndex == -1)
        {
            Debug.LogError($"{direction} 방향의 바인딩을 찾을 수 없습니다.");
            return;
        }

        // 리바인드 전에 Disable
        action.Disable();

        // 현재 사용 중인 모든 바인딩 경로 가져오기
        var usedPaths = GetAllCurrentBindingPaths();
        // 현재 액션의 다른 바인딩은 제외
        var currentActionPaths = GetActionBindingPaths(actionName, bindingIndex);
        usedPaths.ExceptWith(currentActionPaths);

        var rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse");

        // 사용 중인 키 제외
        foreach (var path in usedPaths)
        {
            rebindOperation.WithControlsExcluding(path);
        }

        rebindOperation.OnComplete(operation =>
            {
                operation.Dispose();
                action.Enable();

                Debug.Log($"{actionName} {direction} 리바인드 완료: {action.bindings[bindingIndex].effectivePath}");
                
                // 저장
                string key = $"{actionName}_{direction}";
                PlayerPrefs.SetString(key, action.bindings[bindingIndex].effectivePath);
                PlayerPrefs.Save();
            })
            .Start();
    }

    // 2D Vector Composite에서 특정 방향의 바인딩 인덱스를 찾는 헬퍼 메서드
    private int GetMoveBindingIndex(InputAction action, string direction)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (action.bindings[i].isComposite) continue;
            
            string bindingName = action.bindings[i].name;
            if (string.IsNullOrEmpty(bindingName)) continue;

            // 방향 매칭 (대소문자 구분 없이)
            if (bindingName.ToLower() == direction.ToLower())
            {
                return i;
            }
        }
        return -1;
    }

    // Move 액션의 모든 방향에 대한 바인딩 정보 가져오기
    public Dictionary<string, string> GetMoveBindings(string actionName)
    {
        var result = new Dictionary<string, string>();
        
        if (InputManager.Instance == null || InputManager.Instance.playerInput == null)
            return result;

        var action = InputManager.Instance.playerInput.asset.FindAction(actionName);
        if (action == null) return result;

        string[] directions = { "up", "down", "left", "right" };
        
        foreach (string direction in directions)
        {
            int bindingIndex = GetMoveBindingIndex(action, direction);
            if (bindingIndex != -1)
            {
                var binding = action.bindings[bindingIndex];
                result[direction] = binding.effectivePath;
            }
        }

        return result;
    }

    // Move 액션의 바인딩을 UI 텍스트로 표시하기 위한 헬퍼
    public string GetMoveBindingDisplayString(string actionName, string direction)
    {
        if (InputManager.Instance == null || InputManager.Instance.playerInput == null)
            return "없음";

        var action = InputManager.Instance.playerInput.asset.FindAction(actionName);
        if (action == null) return "없음";

        int bindingIndex = GetMoveBindingIndex(action, direction);
        if (bindingIndex == -1) return "없음";

        return action.GetBindingDisplayString(bindingIndex);
    }

    // 특정 Move 방향의 바인딩을 기본값으로 리셋
    public void ResetMoveBinding(string actionName, string direction)
    {
        if (InputManager.Instance == null || InputManager.Instance.playerInput == null)
            return;

        var action = InputManager.Instance.playerInput.asset.FindAction(actionName);
        if (action == null) return;

        int bindingIndex = GetMoveBindingIndex(action, direction);
        if (bindingIndex == -1) return;

        action.RemoveBindingOverride(bindingIndex);
        
        // PlayerPrefs에서도 제거
        string key = $"{actionName}_{direction}";
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    // 모든 바인딩을 기본값으로 리셋
    public void ResetAllBindings()
    {
        if (InputManager.Instance == null || InputManager.Instance.playerInput == null)
            return;

        var inputActions = InputManager.Instance.playerInput.asset;
        
        foreach (var map in inputActions.actionMaps)
        {
            map.RemoveAllBindingOverrides();
        }

        // PlayerPrefs 모두 삭제
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}