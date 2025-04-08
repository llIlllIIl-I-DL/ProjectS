using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerSettings settings;

    // 필수 컴포넌트들
    private PlayerInputHandler inputHandler;
    private PlayerMovement movement;
    private CollisionDetector collisionDetector;
    private PlayerStateManager stateManager;
    private PlayerAnimator playerAnimator;

    private void Awake()
    {
        // 필요한 컴포넌트 추가
        EnsureComponents();
    }

    private void EnsureComponents()
    {
        // 컴포넌트 가져오기 또는 추가
        inputHandler = GetComponent<PlayerInputHandler>();
        if (inputHandler == null)
            inputHandler = gameObject.AddComponent<PlayerInputHandler>();

        movement = GetComponent<PlayerMovement>();
        if (movement == null)
            movement = gameObject.AddComponent<PlayerMovement>();

        collisionDetector = GetComponent<CollisionDetector>();
        if (collisionDetector == null)
            collisionDetector = gameObject.AddComponent<CollisionDetector>();

        stateManager = GetComponent<PlayerStateManager>();
        if (stateManager == null)
            stateManager = gameObject.AddComponent<PlayerStateManager>();

        playerAnimator = GetComponent<PlayerAnimator>();
        if (playerAnimator == null)
            playerAnimator = gameObject.AddComponent<PlayerAnimator>();
    }
}