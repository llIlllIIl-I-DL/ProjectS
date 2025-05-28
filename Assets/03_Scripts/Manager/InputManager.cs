using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
    public InputActionAsset inputActions;
    public PlayerInput playerInput { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        // PlayerInput 생성
        playerInput = new PlayerInput();
        // inputActions가 없으면 자동으로 생성
        if (inputActions == null)
        {
            inputActions = playerInput.asset;
        }
    }
} 