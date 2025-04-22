using UnityEngine;
using System.Collections;

/// <summary>
/// 레이저 오브젝트 - 플레이어와 상호작용 시 레이저 발사
/// </summary>
public class ObjectLazer : BaseObject
{
    // 레이저가 지금 너무 안이쁘게 나감
    // 메터리얼이랑 쉐이더를 바꿔야할 듯 
    
    [Header("레이저 설정")]
    [SerializeField] private float laserRange; // 레이저 최대 사거리
    [SerializeField] private float laserDuration ; // 레이저 지속 시간
    [SerializeField] private float cooldownTime; // 재사용 대기시간
    [SerializeField] private LayerMask hitLayers; // 레이저가 충돌할 레이어
    [SerializeField] private int maxReflections; // 최대 반사 횟수
    
    [Header("시각 효과")]
    [SerializeField] private LineRenderer laserLine; // 레이저 라인 렌더러
    [SerializeField] private float laserWidth; // 레이저 두께
    [SerializeField] private GameObject impactEffect; // 충돌 효과
    [SerializeField] private Transform firePoint; // 레이저 발사 위치
    
    [Header("소리 효과")]
    [SerializeField] private AudioClip chargeSound; // 충전 소리
    [SerializeField] private AudioClip fireSound; // 발사 소리
    
    
    private bool isOnCooldown = false;
    private AudioSource audioSource;
    
    protected override void Start()
    {
        base.Start();
        
        // LineRenderer 없으면 추가
        if (laserLine == null)
        {
            laserLine = gameObject.AddComponent<LineRenderer>();
        }
        
        // 선 설정
        laserLine.startWidth = laserWidth;
        laserLine.endWidth = laserWidth;
        
        // 레이저 포인트 개수 설정 (최대 반사 횟수 + 1) * 2
        laserLine.positionCount = (maxReflections + 1) * 2;
        
        // 발사 지점이 없으면 현재 위치로 설정
        if (firePoint == null)
            firePoint = transform;
            
        laserLine.enabled = false;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    protected override void OnInteract(GameObject interactor)
    {
        if (isOnCooldown)
        {
            Debug.Log("레이저 쿨다운 중...");
            return;
        }
        
        StartCoroutine(FireLaser());
    }

    public void OnLazer()
    {
        if (isOnCooldown)
        {
            Debug.Log("레이저 쿨다운 중...");
            return;
        }
        
        StartCoroutine(FireLaser());
    }
    
    public void OffLazer()
    {
        if (laserLine != null)
        {
            laserLine.enabled = false;
        }
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private IEnumerator FireLaser()
    {
        isOnCooldown = true;
        SetInteractable(false);
        
        // 충전 소리 재생
        if (chargeSound != null && audioSource != null)
        {
            audioSource.clip = chargeSound;
            audioSource.Play();
        }
        
        // 잠시 충전 시간
        yield return new WaitForSeconds(0.5f);
        
        // 레이저 발사 소리
        if (fireSound != null && audioSource != null)
        {
            audioSource.clip = fireSound;
            audioSource.Play();
        }
        
        // 레이저 라인 활성화
        laserLine.enabled = true;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < laserDuration)
        {
            // 반사를 포함한 레이저 처리
            ProcessLaserWithReflections();
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 레이저 비활성화
        laserLine.enabled = false;
        
        // 쿨다운 대기
        yield return new WaitForSeconds(cooldownTime);
        
        isOnCooldown = false;
        SetInteractable(true);
    }

    // 반사를 고려한 레이저 처리 메서드
    private void ProcessLaserWithReflections()
    {
        // 시작점과 방향 설정
        Vector2 startPos = firePoint.position;
        Vector2 direction = firePoint.right; // 발사 방향
        
        // 디버깅용
        Debug.DrawRay(startPos, direction * 5, Color.yellow, 0.5f);
        
        // 레이저 포인트 초기화
        int pointCount = 0;
        laserLine.positionCount = (maxReflections + 1) * 2; // 포인트 개수 설정
        
        // 첫 번째 포인트 설정 (레이저 발사 지점)
        laserLine.SetPosition(pointCount++, startPos);
        
        // 재귀적으로 레이저 반사 처리
        CastLaserRecursive(startPos, direction, 0, ref pointCount);
        
        // 사용하지 않는 점은 마지막 위치와 같게 설정
        for (int i = pointCount; i < laserLine.positionCount; i++)
        {
            laserLine.SetPosition(i, laserLine.GetPosition(pointCount - 1));
        }
    }

    // 재귀적 레이저 처리
    private void CastLaserRecursive(Vector2 startPos, Vector2 direction, int reflectionCount, ref int pointIndex)
    {
        // 최대 반사 횟수 초과 검사
        if (reflectionCount > maxReflections)
            return;
        
        // 레이저 발사 및 충돌 확인
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, laserRange, hitLayers);
        
        if (hit.collider != null)
        {
            // 충돌 지점 설정
            laserLine.SetPosition(pointIndex++, hit.point);
            
            // 충돌 효과 생성
            if (impactEffect != null)
            {
                GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.identity);
                Destroy(impact, 0.2f);
            }
            
            // 디버깅용 로그
            Debug.Log($"레이저가 {hit.collider.name}과 충돌, 위치: {hit.point}");
            
            // 반사경 체크
            ObjectMirror mirror = hit.collider.GetComponent<ObjectMirror>();
            if (mirror != null && reflectionCount < maxReflections)
            {
                // 반사 방향 계산
                Vector2 reflectedDir = mirror.ReflectLaser(direction);
                
                // 디버깅: 법선 및 반사 벡터 시각화
                Debug.DrawRay(hit.point, hit.normal, Color.blue, 0.5f);
                Debug.DrawRay(hit.point, reflectedDir * 5, Color.green, 0.5f);
                
                Debug.Log($"반사경 회전: {mirror.transform.eulerAngles.z}, 법선: {mirror.transform.up}, 반사 방향: {reflectedDir}");
                
                // 다음 반사점 시작 위치 (약간 오프셋)
                Vector2 nextStartPos = hit.point + reflectedDir * 0.05f;
                
                // 다음 레이저 선분의 시작점
                laserLine.SetPosition(pointIndex++, nextStartPos);
                
                // 다음 반사 레이저 계산 (재귀)
                CastLaserRecursive(nextStartPos, reflectedDir, reflectionCount + 1, ref pointIndex);
            }
            else
            {
                // 반사경이 아닌 일반 충돌체 처리 (데미지 등)
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // 데미지 처리 로직
                }
            }
        }
        else
        {
            // 충돌 없음 - 최대 사거리까지 그리기
            Vector2 endPos = startPos + direction * laserRange;
            laserLine.SetPosition(pointIndex++, endPos);
        }
    }
    
    #if UNITY_EDITOR
    // 편집기에서 레이저 방향 시각화
    private void OnDrawGizmos()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(firePoint.position, firePoint.right * laserRange);
        }
    }
    #endif
}