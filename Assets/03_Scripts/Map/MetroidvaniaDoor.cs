using UnityEngine;

[AddComponentMenu("Metroidvania/Door")]
public class MetroidvaniaDoor : MonoBehaviour
{
    public enum DoorType
    {
        Normal,
        OneWay,
        Locked,
        AbilityGate
    }

    public GameObject targetRoom;
    public DoorType doorType = DoorType.Normal;
    public string requiredKeyId; // 열쇠가 필요한 문의 경우
    public string requiredAbilityId; // 특정 능력이 필요한 관문의 경우

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어 감지
        if (collision.CompareTag("Player"))
        {
            // 플레이어 컴포넌트 (예시)
            MetroidvaniaPlayer player = collision.GetComponent<MetroidvaniaPlayer>();

            if (player != null)
            {
                // 도어 타입에 따른 처리
                switch (doorType)
                {
                    case DoorType.Normal:
                        // 일반 도어는 항상 통과 가능
                        TransitionToRoom(player);
                        break;

                    case DoorType.OneWay:
                        // 일방통행은 특정 방향에서만 통과 가능 (구현 예시)
                        if (IsApproachingFromValidDirection(player.transform.position))
                        {
                            TransitionToRoom(player);
                        }
                        break;

                    case DoorType.Locked:
                        // 열쇠가 필요한 문
                        if (player.HasKey(requiredKeyId))
                        {
                            TransitionToRoom(player);
                        }
                        else
                        {
                            player.ShowMessage("This door is locked!");
                        }
                        break;

                    case DoorType.AbilityGate:
                        // 특정 능력이 필요한 관문
                        if (player.HasAbility(requiredAbilityId))
                        {
                            TransitionToRoom(player);
                        }
                        else
                        {
                            player.ShowMessage("You need a special ability to pass!");
                        }
                        break;
                }
            }
        }
    }

    private bool IsApproachingFromValidDirection(Vector3 playerPosition)
    {
        // 플레이어가 유효한 방향에서 접근하는지 체크 (예시)
        Vector3 doorToPlayer = playerPosition - transform.position;
        return Vector3.Dot(transform.up, doorToPlayer) > 0;
    }

    private void TransitionToRoom(MetroidvaniaPlayer player)
    {
        // 다른 방으로 이동
        if (targetRoom != null)
        {
            // 타겟 방의 입구 위치 찾기
            RoomBehavior targetRoomBehavior = targetRoom.GetComponent<RoomBehavior>();
            Transform entryPoint = targetRoom.transform.Find("EntryPoint");

            if (entryPoint != null)
            {
                // 플레이어 위치 이동
                player.transform.position = entryPoint.position;

                // 방 전환 이펙트 (필요시)
                player.PlayRoomTransitionEffect();
            }
            else
            {
                // 입구 없으면 방 중앙으로
                player.transform.position = targetRoom.transform.position;
            }
        }
    }
}