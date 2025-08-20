using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class CameraControl : MonoBehaviour
{
    [SerializeField] Camera camera;
    [SerializeField] Camera VRCamera;
    [SerializeField] List<Transform> camPositions = new List<Transform>();

    //[SerializeField] WebSocketClient webSocket;

    int currentCameraId = 0;

    void Start()
    {

    }

    private void Update()
    {
        if (currentCameraId == camPositions.Count)
        {
            camera.transform.SetPositionAndRotation(
                VRCamera.transform.position,
                VRCamera.transform.rotation
            );
        }

    }

    public void SetCameraToIndex(int cameraIndex)
    {

        Debug.Log("[CameraControl] Current Camera Index: " + cameraIndex);
        currentCameraId = cameraIndex;

        // set position of camera according to cameraIndex
        try
        {
            camera.transform.SetPositionAndRotation(camPositions[cameraIndex].transform.position, camPositions[cameraIndex].rotation);
        }
        catch
        {
            Debug.Log("[CameraControl] Out of bounds");
        }

        Debug.Log("[Camera] Switched to camera: " + currentCameraId);
    }
}
