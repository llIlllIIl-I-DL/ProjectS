using UnityEngine;

public class BossRoomEnterTrigger : MonoBehaviour
{
    [Header("보스 참조")]
    [SerializeField] private BossStateMachine bossSM;
    
    [Header("설정")]
    [SerializeField] private bool activateOnEnter = true;
    [SerializeField] private BoxCollider2D triggerArea;
    [SerializeField] private bool showBossHealthOnEnter = true;
    
    private void Awake()
    {
        // 컴포넌트가 없으면 자동으로 가져오기
        if (triggerArea == null)
            triggerArea = GetComponent<BoxCollider2D>();
            
        // 트리거 설정 확인
        if (triggerArea != null && !triggerArea.isTrigger)
        {
            triggerArea.isTrigger = true;
            Debug.Log("[BossRoomEnterTrigger] 콜라이더를 트리거로 설정했습니다.");
        }
        
        // 보스 참조 확인
        if (bossSM == null)
        {
            bossSM = FindObjectOfType<BossStateMachine>();
            if (bossSM == null)
                Debug.LogError("[BossRoomEnterTrigger] 보스 스테이트 머신을 찾을 수 없습니다.");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!activateOnEnter) return;
        
        if (other.CompareTag(GameConstants.Tags.PLAYER))
        {
            Debug.Log("[BossRoomEnterTrigger] 플레이어가 보스룸에 입장했습니다!");
            
            // 보스에게 플레이어 감지 알림
            if (bossSM != null)
            {
                bossSM.DetectPlayer(other.transform);
                
                // 보스룸 활성화 시 카메라 또는 UI 이벤트를 트리거할 수도 있음
                // BossRoomActivated?.Invoke();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(GameConstants.Tags.PLAYER))
        {
            Debug.Log("[BossRoomTrigger] 플레이어가 보스룸을 벗어났습니다!");

            // 보스 비활성화 처리
            if (bossSM != null)
            {
                bossSM.ResetBoss(); // 리셋 호출
            }
        }
    }

    private void OnDrawGizmos()
    {
        // 트리거 영역 시각화
        if (triggerArea != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // 주황색 반투명
            
            // 콜라이더 크기와 위치 기반으로 Gizmo 그리기
            Vector3 center = transform.position + new Vector3(triggerArea.offset.x, triggerArea.offset.y, 0);
            Vector3 size = new Vector3(triggerArea.size.x, triggerArea.size.y, 0.1f);
            
            Gizmos.DrawCube(center, size);
            
            // 테두리
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}