using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class CameraControl : MonoBehaviour
{
    [Header("Camera Configuration")]
    [SerializeField]
    [Tooltip("Main camera used for tablet streaming")]
    Camera camera;

    [SerializeField]
    [Tooltip("VR user's camera")]
    Camera VRCamera;

    [SerializeField]
    [Tooltip("Available camera positions for switching")]
    List<Transform> camPositions = new List<Transform>();

    [Header("Available Cameras")]
    [SerializeField]
    [Tooltip("All cameras available for this scenario (for SO_ScenarioData integration)")]
    public Camera[] availableCameras;

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
        currentCameraId += cameraIndex;

        // set position of camera according to cameraIndex
        if (currentCameraId < 0)
        {
            currentCameraId = camPositions.Count - 1;
        }
        else if (currentCameraId >= camPositions.Count)
        {
            currentCameraId = 0;
        }
        try
        {
            camera.transform.SetPositionAndRotation(camPositions[currentCameraId].transform.position, camPositions[currentCameraId].rotation);
        }
        catch
        {
            Debug.Log("[CameraControl] Out of bounds");
        }

        Debug.Log("[Camera] Switched to camera: " + currentCameraId);
    }

    #region MessageHandler Integration Methods

    /// <summary>
    /// Get available cameras for scenario data integration
    /// </summary>
    /// <returns>Array of available cameras</returns>
    public Camera[] GetAvailableCameras()
    {
        if (availableCameras != null && availableCameras.Length > 0)
        {
            return availableCameras;
        }

        // Fallback: create array from main camera and VR camera
        List<Camera> fallbackCameras = new List<Camera>();
        if (camera != null) fallbackCameras.Add(camera);
        if (VRCamera != null) fallbackCameras.Add(VRCamera);

        return fallbackCameras.ToArray();
    }

    /// <summary>
    /// Get camera names for tablet UI
    /// </summary>
    /// <returns>Array of camera names</returns>
    public string[] GetCameraNames()
    {
        Camera[] cameras = GetAvailableCameras();
        string[] names = new string[cameras.Length];

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null)
                names[i] = cameras[i].name;
            else
                names[i] = $"Camera {i}";
        }

        return names;
    }

    /// <summary>
    /// Get current camera index
    /// </summary>
    /// <returns>Current camera index</returns>
    public int GetCurrentCameraIndex()
    {
        return currentCameraId;
    }

    /// <summary>
    /// Validate camera configuration
    /// </summary>
    /// <returns>True if configuration is valid</returns>
    public bool ValidateCameraConfiguration()
    {
        if (camera == null)
        {
            Debug.LogError("[CameraControl] Main camera is not assigned!");
            return false;
        }

        if (VRCamera == null)
        {
            Debug.LogWarning("[CameraControl] VR camera is not assigned!");
        }

        if (camPositions.Count == 0)
        {
            Debug.LogWarning("[CameraControl] No camera positions defined!");
        }

        // Check for null positions
        for (int i = 0; i < camPositions.Count; i++)
        {
            if (camPositions[i] == null)
            {
                Debug.LogError($"[CameraControl] Camera position at index {i} is null!");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Initialize camera streaming preparation (for future tablet streaming)
    /// </summary>
    public void PrepareForStreaming()
    {
        // TODO: Implement camera streaming setup for tablet
        // This will be used when implementing camera feed to tablet
        Debug.Log("[CameraControl] Camera streaming preparation - TODO: Implement streaming setup");
    }

    #endregion
}
