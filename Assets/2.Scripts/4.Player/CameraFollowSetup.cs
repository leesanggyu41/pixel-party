using UnityEngine;
using Unity.Cinemachine;
using Fusion;

public class CameraFollowSetup : NetworkBehaviour
{
    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {


            CinemachineCamera vcam = FindFirstObjectByType<CinemachineCamera>();

            if (vcam != null)
            {
                vcam.Follow = transform;
            }
            else
            {
                Debug.LogWarning("씬에 CinemachineCamera가 없습니다.");
            }
        }
    }
}
