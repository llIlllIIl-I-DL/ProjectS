using UnityEngine;
using Cinemachine;

public class CinemachineMountainParallax : CinemachineExtension
{
    public Transform mountainTransform;
    public float parallaxEffect = 0.5f;
    private Vector3 lastCameraPosition;

    protected override void Awake()
    {
        base.Awake();
        lastCameraPosition = Vector3.zero;
    }

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Finalize && mountainTransform != null)
        {
            Vector3 deltaMovement = state.FinalPosition - lastCameraPosition;
            mountainTransform.position += new Vector3(deltaMovement.x * parallaxEffect, deltaMovement.y * parallaxEffect, 0);
            lastCameraPosition = state.FinalPosition;
        }
    }
}