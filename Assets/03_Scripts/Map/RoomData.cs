using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RoomData
{
    public List<PlacedModuleData> placedModules = new List<PlacedModuleData>();

    [System.Serializable]
    public class PlacedModuleData
    {
        public string moduleGUID; // 모듈 에셋의 GUID
        public Vector2 position;
        public int rotationStep; // 90도 단위 회전 (0, 1, 2, 3)
        public string moduleType; // 모듈 타입 아이템인지, 방인지 구분
        public string itemID; // 아이템 아이디  
        public List<ConnectionData> connections = new List<ConnectionData>();
    }

    [System.Serializable]
    public class ConnectionData
    {
        public int connectionPointIndex;
        public string connectedModuleGUID;
        public int connectedPointIndex;
    }
}