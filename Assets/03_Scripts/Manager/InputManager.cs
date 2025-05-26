using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public InputActionAsset inputActions;
    public PlayerInput playerInput { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // PlayerInput 생성
            playerInput = new PlayerInput();
            
            // inputActions가 없으면 자동으로 생성
            if (inputActions == null)
            {
                inputActions = playerInput.asset;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
} 