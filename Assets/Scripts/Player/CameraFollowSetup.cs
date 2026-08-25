using UnityEngine;
using Unity.Cinemachine;

public class CameraFollowSetup : MonoBehaviour
{
private void Start()
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
