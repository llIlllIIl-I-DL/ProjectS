using UnityEngine;
using Cinemachine;

public class MountainFollower : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;
    public Vector3 offset;

    void LateUpdate()
    {
        if (virtualCamera != null)
        {
            // 카메라의 위치를 기준으로 산의 위치를 조정  
            Vector3 cameraPosition = virtualCamera.State.FinalPosition;
            transform.position = new Vector3(
                cameraPosition.x + offset.x,
                cameraPosition.y + offset.y,
                transform.position.z
            );
        }
    }
}
