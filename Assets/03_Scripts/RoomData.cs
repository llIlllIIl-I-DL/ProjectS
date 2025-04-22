using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomData
{
    public List<PlacedModuleData> placedModules = new List<PlacedModuleData>();

    [System.Serializable]
    public class PlacedModuleData
    {
        public string moduleGUID;
        public Vector2 position;
        public int rotationStep;
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