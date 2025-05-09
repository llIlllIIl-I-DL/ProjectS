using UnityEngine;

/// <summary>
/// 맵 청크에 대한 정보를 저장하는 컴포넌트
/// 각 청크 씬에 포함되어 해당 청크의 경계, 크기, 위치 정보 등을 저장
/// </summary>
public class ChunkInfo : MonoBehaviour
{
    [Header("청크 식별 정보")]
    public Vector2Int chunkId;
    public string chunkName => $"Chunk_{chunkId.x}_{chunkId.y}";
    
    [Header("청크 크기 정보")]
    public float chunkSize = 100f;
    public Vector2 boundMin;
    public Vector2 boundMax;
    
    [Header("상태 정보")]
    public int moduleCount;
    public bool isInitialized;
    
    [Header("JSON 파일 정보")]
    public string jsonFilePath;
    
    private void OnDrawGizmos()
    {
        // 에디터에서 청크 경계 시각화
        Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.3f);
        Gizmos.DrawCube(
            new Vector3((boundMin.x + boundMax.x) * 0.5f, (boundMin.y + boundMax.y) * 0.5f, 0f),
            new Vector3(boundMax.x - boundMin.x, boundMax.y - boundMin.y, 1f)
        );
        
        // 청크 ID 표시
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(
            new Vector3((boundMin.x + boundMax.x) * 0.5f, (boundMin.y + boundMax.y) * 0.5f, 0f),
            new Vector3(boundMax.x - boundMin.x, boundMax.y - boundMin.y, 1f)
        );
    }
    
    // 다른 청크와의 인접 여부 확인
    public bool IsAdjacentTo(ChunkInfo other)
    {
        // 상하좌우로 인접한 청크인지 확인
        return Mathf.Abs(chunkId.x - other.chunkId.x) + Mathf.Abs(chunkId.y - other.chunkId.y) == 1;
    }
    
    // 주어진 위치가 이 청크 내에 있는지 확인
    public bool ContainsPosition(Vector3 position)
    {
        return position.x >= boundMin.x && position.x < boundMax.x &&
               position.y >= boundMin.y && position.y < boundMax.y;
    }
    
    // 플레이어 위치로부터의 거리 확인
    public float GetDistanceFromPosition(Vector3 position)
    {
        // 청크 중심 좌표
        Vector3 center = new Vector3(
            (boundMin.x + boundMax.x) * 0.5f,
            (boundMin.y + boundMax.y) * 0.5f,
            0f
        );
        
        return Vector3.Distance(position, center);
    }
} 