using UnityEngine;
using System.Collections;

/// <summary>
/// 반사경 오브젝트 - 레이저 반사 및 회전 기능
/// </summary>
public class ObjectMirror : BaseObject
{
    #region Variables

    [Header("반사경 설정")]
    [SerializeField] private float rotationStep = 45f;   // 회전 단위 (각도)
    [SerializeField] private float initialAngle = 0f;    // 초기 각도
    [SerializeField] private bool canBeAttacked = true;  // 공격으로도 회전 가능한지
    
    [Header("시각 효과")]
    [SerializeField] private SpriteRenderer mirrorRenderer;
    [SerializeField] private GameObject reflectEffect;   // 반사 효과 프리팹
    [SerializeField] private float effectDuration = 0.2f; // 효과 지속 시간
    
    [Header("디버깅")]
    [SerializeField] private bool showDebugRays = true; // 디버그 레이 표시 여부
    [SerializeField] private float debugRayLength = 2f;  // 디버그 레이 길이

    // 법선 벡터 캐싱
    private Vector2 cachedNormal;

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
        
        // 법선 벡터 초기화
        UpdateNormalVector();
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
        
        // 법선 벡터 업데이트
        UpdateNormalVector();
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
    
    /// <summary>
    /// 현재 각도에 따른 법선 벡터 업데이트
    /// </summary>
    private void UpdateNormalVector()
    {
        // 현재 각도에 따른 법선 벡터 계산
        float currentAngle = transform.eulerAngles.z;
        
        // 삼각형 모양을 고려하여 법선 벡터 계산
        // 0도 = 오른쪽을 가리키는 삼각형 = 위쪽 법선
        float angleInRadians = (currentAngle + 90f) * Mathf.Deg2Rad;
        cachedNormal = new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians)).normalized;
        
        // 디버깅
        if (showDebugRays)
        {
            Debug.DrawRay(transform.position, cachedNormal * debugRayLength, Color.blue, 0.5f);
        }
    }
    
    #endregion

    #region Laser Reflection
    
    /// <summary>
    /// 레이저 반사 방향 계산 메서드
    /// </summary>
    /// <param name="incomingDirection">입사 레이저 방향</param>
    /// <param name="hitPoint">레이저 충돌 지점</param>
    /// <returns>반사된 레이저 방향</returns>
    public Vector2 ReflectLaser(Vector2 incomingDirection, Vector2 hitPoint)
    {
        // 입력 방향 정규화
        incomingDirection = incomingDirection.normalized;
        
        // 충돌 지점에서 법선 벡터 계산
        Vector2 mirrorToHit = (hitPoint - (Vector2)transform.position).normalized;
        Vector2 normal = Vector2.Perpendicular(mirrorToHit).normalized;
        
        // 반사 방향 계산
        Vector2 reflectedDirection = Vector2.Reflect(incomingDirection, normal);
        
        // 디버깅 및 효과
        if (showDebugRays)
        {
            // 입사 레이저
            Debug.DrawRay(hitPoint, incomingDirection.normalized * debugRayLength, Color.yellow, 1f);
            // 법선 벡터
            Debug.DrawRay(hitPoint, normal * debugRayLength, Color.blue, 1f);
            // 반사 레이저
            Debug.DrawRay(hitPoint, reflectedDirection * debugRayLength, Color.green, 1f);
        }
        
        // 반사 이펙트 생성
        ShowReflectionEffect(hitPoint);
        
        // 현재 각도 로그
        Debug.Log($"반사경 각도: {transform.eulerAngles.z}, 법선 벡터: {cachedNormal}, 입사: {incomingDirection}, 반사: {reflectedDirection}");
        
        return reflectedDirection;
    }
    
    /// <summary>
    /// 간단한 반사 계산 (중앙 충돌 가정, ObjectLazer에서 사용)
    /// </summary>
    public Vector2 ReflectLaser(Vector2 incomingDirection)
    {
        return ReflectLaser(incomingDirection, transform.position);
    }
    
    /// <summary>
    /// 반사 효과 생성
    /// </summary>
    private void ShowReflectionEffect(Vector2 position)
    {
        if (reflectEffect != null)
        {
            GameObject effect = Instantiate(reflectEffect, position, Quaternion.identity);
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
    private void OnDrawGizmos()
    {
        // 에디터에서 법선 벡터 방향 표시
        Gizmos.color = Color.blue;
        
        float angle = transform.eulerAngles.z;
        float angleInRadians = (angle + 90f) * Mathf.Deg2Rad;
        Vector2 normal = new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians)).normalized;
        
        Gizmos.DrawRay(transform.position, normal * debugRayLength);
        
        // 반사경 외곽선
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 1f, 0.1f));
    }
    #endif
    
    #endregion
}