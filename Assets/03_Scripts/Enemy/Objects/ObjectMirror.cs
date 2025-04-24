using UnityEngine;
using System.Collections;

/// <summary>
/// 반사경 오브젝트 - 레이저 반사 및 회전 기능
/// </summary>
public class ObjectMirror : BaseObject
{
    #region Variables

    [Header("반사경 설정")]
    [SerializeField] private float rotationStep;     // 회전 단위 (각도)
    [SerializeField] private float initialAngle;     // 초기 각도
    [SerializeField] private bool canBeAttacked = true; // 공격으로도 회전 가능한지
    
    [Header("시각 효과")]
    [SerializeField] private SpriteRenderer mirrorRenderer;
    [SerializeField] private GameObject reflectEffect; // 반사 효과 프리팹
    [SerializeField] private float effectDuration;     // 효과 지속 시간

    #endregion

    #region Unity Lifecycle
    
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
    
    #endregion

    #region Interaction
    
    /// <summary>
    /// 상호작용 시 호출 - 각도 변경
    /// </summary>
    protected override void OnInteract(GameObject interactor)
    {
        RotateMirror();
        PlayInteractSound();
    }
    
    /// <summary>
    /// 공격 받았을 때 처리 (외부에서 호출)
    /// </summary>
    public void OnAttacked()
    {
        if (!canBeAttacked) return;
        
        RotateMirror();
        
        // 시각 효과
        StartCoroutine(FlashEffect());
    }
    
    #endregion

    #region Mirror Rotation
    
    /// <summary>
    /// 반사경 회전 처리
    /// </summary>
    private void RotateMirror()
    {
        transform.Rotate(0, 0, rotationStep);
        
        // rotationStep 단위로 각도 맞추기
        SnapToRotationStep();
    }
    
    /// <summary>
    /// 회전 단위에 맞춰 각도 조정하는 메서드
    /// </summary>
    private void SnapToRotationStep()
    {
        float z = transform.eulerAngles.z;
        z = Mathf.Round(z / rotationStep) * rotationStep;
        transform.rotation = Quaternion.Euler(0, 0, z);
    }
    
    #endregion

    #region Laser Reflection
    
    /// <summary>
    /// 레이저 반사 방향 계산 메서드
    /// </summary>
    /// <param name="incomingDirection">입사 레이저 방향</param>
    /// <returns>반사된 레이저 방향</returns>
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
        ShowReflectionEffect();
        
        return reflectedDirection;
    }
    
    /// <summary>
    /// 반사 효과 생성
    /// </summary>
    private void ShowReflectionEffect()
    {
        if (reflectEffect != null)
        {
            GameObject effect = Instantiate(reflectEffect, transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
    }
    
    #endregion

    #region Visual Effects
    
    /// <summary>
    /// 반사경 깜박임 효과
    /// </summary>
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
    
    #endregion

    #region Editor
    
    #if UNITY_EDITOR
    // 편집기에서 법선 벡터 시각화
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.up * 1f);
    }
    #endif
    
    #endregion
}