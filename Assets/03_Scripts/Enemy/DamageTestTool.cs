using UnityEngine;
using UnityEngine.InputSystem; // 새 Input System 네임스페이스 추가

public class DamageTestTool : MonoBehaviour
{
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private Key damageKey = Key.F; // KeyCode 대신 Key 사용
    [SerializeField] private float raycastDistance = 5f;
    
    private Camera mainCamera;
    
    private void Start()
    {
        mainCamera = Camera.main;
    }
    
    private void Update()
    {
        // 새 Input System 방식으로 키 입력 감지
        if (Keyboard.current != null && Keyboard.current[damageKey].wasPressedThisFrame)
        {
            TestDamageAtCursor();
        }
    }
    
    private void TestDamageAtCursor()
    {
        // 마우스 위치 가져오기 (새 Input System 방식)
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, raycastDistance);
        
        if (hit.collider != null)
        {
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damageAmount);
                Debug.Log($"데미지 {damageAmount}을(를) {hit.collider.name}에게 적용했습니다.");
            }
            else
            {
                Debug.Log($"{hit.collider.name}은(는) IDamageable 인터페이스를 구현하지 않았습니다.");
            }
        }
        else
        {
            Debug.Log("타겟을 찾지 못했습니다.");
        }
    }
}