using UnityEngine;
using System.Collections;

public class ObjectMirror : BaseObject
{
    [Header("반사경 설정")]
    [SerializeField] private float rotationStep; // 회전 단위 (각도)
    [SerializeField] private float initialAngle; // 초기 각도
    [SerializeField] private bool canBeAttacked = true; // 공격으로도 회전 가능한지
    
    [Header("시각 효과")]
    [SerializeField] private SpriteRenderer mirrorRenderer;
    [SerializeField] private GameObject reflectEffect; // 반사 효과 프리팹
    [SerializeField] private float effectDuration;
    
    protected override void Start()
    {
        base.Start();
        
        if (mirrorRenderer == null)
            mirrorRenderer = GetComponent<SpriteRenderer>();
            
        // 초기 각도 설정
        transform.rotation = Quaternion.Euler(0, 0, initialAngle);
        
        // 회전 각도를 rotationStep의 배수로 맞추기
        SnapToRotationStep();
    }
    
    // 상호작용 시 호출 - 각도 변경
    protected override void OnInteract(GameObject interactor)
    {
        RotateMirror();
        PlayInteractSound();
    }
    
    // 반사경 회전 처리
    private void RotateMirror()
    {
        transform.Rotate(0, 0, rotationStep);
        
        // 회전 각도 45도 단위로 맞추기 (선택 사항)
        float z = transform.eulerAngles.z;
        z = Mathf.Round(z / rotationStep) * rotationStep;
        transform.rotation = Quaternion.Euler(0, 0, z);
    }
    
    // 공격 받았을 때 처리 (외부에서 호출)
    public void OnAttacked()
    {
        if (!canBeAttacked) return;
        
        RotateMirror();
        
        // 시각 효과
        StartCoroutine(FlashEffect());
    }
    
    private IEnumerator FlashEffect()
    {
        if (mirrorRenderer != null)
        {
            Color originalColor = mirrorRenderer.color;
            mirrorRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            mirrorRenderer.color = originalColor;
        }
    }
    
    // 레이저 반사 방향 계산 메서드
    public Vector2 ReflectLaser(Vector2 incomingDirection)
    {
        // 입력 방향 정규화
        incomingDirection = incomingDirection.normalized;
        
        // 거울의 법선 벡터 (up 방향을 기준으로)
        Vector2 normal = transform.up.normalized;
        
        // 디버깅용 로그
        Debug.Log($"반사경 회전 각도: {transform.eulerAngles.z}, 법선 벡터: {normal}");
        
        // 입사 벡터를 반사
        Vector2 reflectedDirection = Vector2.Reflect(incomingDirection, normal);
        
        // 디버깅용 시각화
        Debug.DrawRay(transform.position, normal * 2, Color.blue, 0.5f);
        Debug.DrawRay(transform.position, incomingDirection * 2, Color.yellow, 0.5f);
        Debug.DrawRay(transform.position, reflectedDirection * 2, Color.green, 0.5f);
        
        // 반사 효과 표시
        if (reflectEffect != null)
        {
            GameObject effect = Instantiate(reflectEffect, transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        return reflectedDirection;
    }
    
    // 회전 단위에 맞춰 각도 조정하는 메서드
    private void SnapToRotationStep()
    {
        float z = transform.eulerAngles.z;
        z = Mathf.Round(z / rotationStep) * rotationStep;
        transform.rotation = Quaternion.Euler(0, 0, z);
    }
}