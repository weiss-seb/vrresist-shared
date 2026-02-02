using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace OVGU.VAR.VRResist
{
    public class CameraControl : MonoBehaviour
    {
        private Camera externalCamera;
        private Camera firstPoV;
        List<CameraPosition> camPositions = new List<CameraPosition>();

        int currentCameraId = 0;


        void Awake()
        {

        }
        void Start()
        {
            firstPoV = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
            externalCamera = transform.GetChild(0).GetComponent<Camera>();
            ValidateCameraConfiguration();
        }

        private void Update()
        {
            if (currentCameraId == camPositions.Count)
            {
                externalCamera.transform.SetPositionAndRotation(
                    firstPoV.transform.position,
                    firstPoV.transform.rotation
                );
            }

        }

        public void SetCameraPositions(List<CameraPosition> positionsFromSO)
        {
            firstPoV = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();

            // camPositions.Clear();
            // camPositions.AddRange(positionsFromSO);
            // camPositions.Add(new CameraPosition
            // {
            //     position = firstPoV.transform.position,
            //     rotation = firstPoV.transform.eulerAngles
            // });
            // Debug.Log("[CameraControl] Loaded " + camPositions.Count + " camera positions from scenario data.");
        }

        public void SetCameraToIndex(int cameraIndex)
        {

            Debug.Log("[CameraControl] Current Camera Index: " + cameraIndex);
            currentCameraId += cameraIndex;

            // set position of camera according to cameraIndex
            if (currentCameraId < 0)
            {
                currentCameraId = camPositions.Count;
            }
            else if (currentCameraId > camPositions.Count)
            {
                currentCameraId = 0;
            }


            externalCamera.transform.position = camPositions[currentCameraId].position;
            externalCamera.transform.eulerAngles = camPositions[currentCameraId].rotation;


            Debug.Log("[Camera] Switched to camera: " + currentCameraId);
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
            if (externalCamera == null)
            {
                Debug.LogError("[CameraControl] Main camera is not assigned!");
                return false;
            }

            if (firstPoV == null)
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
                if (camPositions[i].Equals(default(CameraPosition)))
                {
                    Debug.LogError($"[CameraControl] Camera position at index {i} is null!");
                    return false;
                }
            }

            return true;
        }
    }
}
