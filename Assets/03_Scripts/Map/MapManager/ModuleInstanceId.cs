// 모듈 인스턴스 ID (안정적인 키 생성을 위한 구조체)
using System;
using UnityEngine;

[System.Serializable]
public struct ModuleInstanceId : IEquatable<ModuleInstanceId>
{
    public string moduleGUID;
    public float posX;
    public float posY;

    public ModuleInstanceId(string guid, Vector2 position)
    {
        moduleGUID = guid;
        posX = position.x;
        posY = position.y;
    }

    public override string ToString()
    {
        return $"{moduleGUID}_{posX:F2}_{posY:F2}";
    }

    public string GetModuleGuid()
    {
        return moduleGUID;
    }

    public bool Equals(ModuleInstanceId other)
    {
        return moduleGUID == other.moduleGUID &&
               Math.Abs(posX - other.posX) < 0.001f &&
               Math.Abs(posY - other.posY) < 0.001f;
    }

    public override bool Equals(object obj)
    {
        return obj is ModuleInstanceId other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + (moduleGUID != null ? moduleGUID.GetHashCode() : 0);
            hash = hash * 23 + posX.GetHashCode();
            hash = hash * 23 + posY.GetHashCode();
            return hash;
        }
    }
}



// 모듈 인스턴스화 인터페이스
public interface IModuleInstantiator
{
    GameObject InstantiateModule(RoomData.PlacedModuleData moduleData);
    RoomModule GetModuleByGUID(string guid);
    void ClearCache();
}