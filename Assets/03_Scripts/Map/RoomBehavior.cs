using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Metroidvania/Room Behavior")]
public class RoomBehavior : MonoBehaviour
{
    public RoomModule moduleData;
    public string instanceId;

    private Dictionary<int, GameObject> connectionObjects = new Dictionary<int, GameObject>();

    private Dictionary<ConnectionPoint.ConnectionType, Action<GameObject, GameObject>> setups;
    
    private void Awake()
    {
        // 방 초기화
        InitializeRoom();
        
        setups = new Dictionary<ConnectionPoint.ConnectionType, Action<GameObject, GameObject>>
        {
            { ConnectionPoint.ConnectionType.Normal, SetupNormalDoor },
            { ConnectionPoint.ConnectionType.OneWay, SetupOneWayDoor },
            { ConnectionPoint.ConnectionType.LockedDoor, SetupLockedDoor },
            { ConnectionPoint.ConnectionType.AbilityGate, SetupAbilityGate }
        };
    }

    private void InitializeRoom()
    {
        if (moduleData == null)
            return;

        // 연결점 관련 오브젝트 찾기 (예: 도어, 게이트 등)
        for (int i = 0; i < moduleData.connectionPoints.Length; i++)
        {
            string pointName = "ConnectionPoint_" + i;
            Transform pointTransform = transform.Find(pointName);

            if (pointTransform != null)
            {
                connectionObjects[i] = pointTransform.gameObject;
            }
        }

        // 방 종류에 따른 특수 동작 설정
        if (moduleData.isSpecialRoom)
        {
            // 보스룸, 아이템룸 등의 특수 설정
        }
    }

    public void SetupConnection(int connectionIndex, GameObject targetRoom)
    {
        if (connectionObjects.TryGetValue(connectionIndex, out GameObject connObject))
        {
            // 연결 오브젝트(도어 등) 설정
            ConnectionPoint connPoint = moduleData.connectionPoints[connectionIndex];

            // 델리게이트와 enum 이용시
            // if (setups.TryGetValue(connPoint.type, out var setupAction))
            // {
            //     setupAction.Invoke(connObject, targetRoom);
            // }
            
            
            // 연결 타입에 따른 처리
            switch (connPoint.type)
            {
                case ConnectionPoint.ConnectionType.Normal:
                    SetupNormalDoor(connObject, targetRoom);
                    break;

                case ConnectionPoint.ConnectionType.OneWay:
                    SetupOneWayDoor(connObject, targetRoom);
                    break;

                case ConnectionPoint.ConnectionType.LockedDoor:
                    SetupLockedDoor(connObject, targetRoom);
                    break;

                case ConnectionPoint.ConnectionType.AbilityGate:
                    SetupAbilityGate(connObject, targetRoom);
                    break;
            }
        }
    }

    private void SetupNormalDoor(GameObject doorObject, GameObject targetRoom)
    {
        // 일반 도어 설정
        // 기능이 전반적으로 유사하다 -> 모듈화할 수 있다.
        MetroidvaniaDoor door = doorObject.GetComponent<MetroidvaniaDoor>();
        if (door == null)
        {
            door = doorObject.AddComponent<MetroidvaniaDoor>();
        }

        door.targetRoom = targetRoom;
        door.doorType = MetroidvaniaDoor.DoorType.Normal;
    }

    private void SetupOneWayDoor(GameObject doorObject, GameObject targetRoom)
    {
        // 일방통행 도어 설정
        MetroidvaniaDoor door = doorObject.GetComponent<MetroidvaniaDoor>();
        if (door == null)
        {
            door = doorObject.AddComponent<MetroidvaniaDoor>();
        }

        door.targetRoom = targetRoom;
        door.doorType = MetroidvaniaDoor.DoorType.OneWay;
    }

    private void SetupLockedDoor(GameObject doorObject, GameObject targetRoom)
    {
        // 잠긴 도어 설정
        MetroidvaniaDoor door = doorObject.GetComponent<MetroidvaniaDoor>();
        if (door == null)
        {
            door = doorObject.AddComponent<MetroidvaniaDoor>();
        }

        door.targetRoom = targetRoom;
        door.doorType = MetroidvaniaDoor.DoorType.Locked;
        door.requiredKeyId = "default_key"; // 키 ID 설정
    }

    private void SetupAbilityGate(GameObject doorObject, GameObject targetRoom)
    {
        // 능력 관문 설정
        MetroidvaniaDoor door = doorObject.GetComponent<MetroidvaniaDoor>();
        if (door == null)
        {
            door = doorObject.AddComponent<MetroidvaniaDoor>();
        }

        door.targetRoom = targetRoom;
        door.doorType = MetroidvaniaDoor.DoorType.AbilityGate;
        door.requiredAbilityId = "default_ability"; // 필요 능력 ID 설정
    }
}