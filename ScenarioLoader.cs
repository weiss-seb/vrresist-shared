using UnityEngine;
using System.Collections.Generic;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Loads scenario data from ScriptableObject and applies it to scene components
    /// This component bridges the gap between persistent ScriptableObject data and scene-specific GameObjects
    /// </summary>
    public class ScenarioLoader : MonoBehaviour
    {
        [Header("Scenario Configuration")]
        [SerializeField] private SO_ScenarioData scenarioData;

        [Header("Target Components")]
        [SerializeField] private EventTriggerSystem eventTriggerSystem;

        [Header("Debug Settings")]
        [SerializeField] private bool enableDetailedLogging = true;

        // Cached references to found GameObjects
        private Dictionary<string, GameObject> foundCharacters = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> foundWaypoints = new Dictionary<string, GameObject>();
        private Dictionary<string, Camera> foundCameras = new Dictionary<string, Camera>();

        void Start()
        {
            if (scenarioData == null)
            {
                Debug.LogError("[ScenarioLoader] No scenario data assigned!");
                return;
            }

            LoadScenarioData();
        }

        public SO_ScenarioData GetScenarioData()
        {
            return scenarioData;
        }

        /// <summary>
        /// Load scenario data and apply it to scene components
        /// </summary>
        public void LoadScenarioData()
        {
            if (scenarioData == null)
            {
                Debug.LogError("[ScenarioLoader] No scenario data to load!");
                return;
            }

            if (enableDetailedLogging)
                Debug.Log($"[ScenarioLoader] Loading scenario: {scenarioData.scenarioName}");

            // Find and cache all GameObjects
            FindCharacters();
            FindWaypoints();
            FindCameras();

            // Apply scenario data to EventTriggerSystem
            ApplyToEventTriggerSystem();

            // Set NPC starting positions
            SetNPCStartingPositions();

            if (enableDetailedLogging)
                Debug.Log($"[ScenarioLoader] Successfully loaded scenario: {scenarioData.scenarioName}");
        }

        /// <summary>
        /// Find character GameObjects in the scene
        /// </summary>
        private void FindCharacters()
        {
            foundCharacters.Clear();

            // Try finding by names first
            if (scenarioData.characterNames != null)
            {
                foreach (string characterName in scenarioData.characterNames)
                {
                    GameObject character = GameObject.Find(characterName);
                    if (character != null)
                    {
                        foundCharacters[characterName.ToLower()] = character;
                        if (enableDetailedLogging)
                            Debug.Log($"[ScenarioLoader] Found character by name: {characterName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ScenarioLoader] Character not found by name: {characterName}");
                    }
                }
            }

            // Try finding by tags as fallback
            if (scenarioData.characterTags != null)
            {
                for (int i = 0; i < scenarioData.characterTags.Length; i++)
                {
                    string tag = scenarioData.characterTags[i];
                    GameObject character = GameObject.FindGameObjectWithTag(tag);
                    if (character != null)
                    {
                        string key = (i < scenarioData.characterNames.Length) ?
                            scenarioData.characterNames[i].ToLower() : tag.ToLower();

                        if (!foundCharacters.ContainsKey(key))
                        {
                            foundCharacters[key] = character;
                            if (enableDetailedLogging)
                                Debug.Log($"[ScenarioLoader] Found character by tag: {tag} -> {key}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Find waypoint GameObjects in the scene
        /// </summary>
        private void FindWaypoints()
        {
            foundWaypoints.Clear();

            if (scenarioData.availablePositions != null)
            {
                foreach (string positionName in scenarioData.availablePositions)
                {
                    GameObject waypoint = GameObject.Find(positionName);
                    if (waypoint != null)
                    {
                        foundWaypoints[positionName.ToLower()] = waypoint;
                        if (enableDetailedLogging)
                            Debug.Log($"[ScenarioLoader] Found waypoint: {positionName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ScenarioLoader] Waypoint not found: {positionName}");
                    }
                }
            }
        }

        /// <summary>
        /// Find camera objects in the scene
        /// </summary>
        private void FindCameras()
        {
            foundCameras.Clear();

            if (scenarioData.cameraNames != null)
            {
                foreach (string cameraName in scenarioData.cameraNames)
                {
                    GameObject cameraObj = GameObject.Find(cameraName);
                    if (cameraObj != null)
                    {
                        Camera camera = cameraObj.GetComponent<Camera>();
                        if (camera != null)
                        {
                            foundCameras[cameraName.ToLower()] = camera;
                            if (enableDetailedLogging)
                                Debug.Log($"[ScenarioLoader] Found camera: {cameraName}");
                        }
                        else
                        {
                            Debug.LogWarning($"[ScenarioLoader] GameObject {cameraName} found but has no Camera component");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[ScenarioLoader] Camera not found: {cameraName}");
                    }
                }
            }
        }

        /// <summary>
        /// Apply scenario data to EventTriggerSystem
        /// </summary>
        private void ApplyToEventTriggerSystem()
        {
            if (eventTriggerSystem == null)
            {
                eventTriggerSystem = FindObjectOfType<EventTriggerSystem>();
                if (eventTriggerSystem == null)
                {
                    Debug.LogWarning("[ScenarioLoader] EventTriggerSystem not found in scene!");
                    return;
                }
            }

            // Apply character references
            ApplyCharacterReferences();

            // Apply waypoint references
            ApplyWaypointReferences();

            if (enableDetailedLogging)
                Debug.Log("[ScenarioLoader] Applied scenario data to EventTriggerSystem");
        }

        /// <summary>
        /// Apply found character references to EventTriggerSystem
        /// Dynamic mapping based on characterNames array order
        /// </summary>
        private void ApplyCharacterReferences()
        {
            if (scenarioData.characterNames == null || scenarioData.characterNames.Length == 0)
            {
                Debug.LogWarning("[ScenarioLoader] No character names configured in scenario data!");
                return;
            }

            // Map characters dynamically based on array order
            // First character -> patient, Second -> colleague, Third -> head_doctor
            for (int i = 0; i < scenarioData.characterNames.Length && i < 3; i++)
            {
                string characterName = scenarioData.characterNames[i].ToLower();
                if (foundCharacters.ContainsKey(characterName))
                {
                    switch (i)
                    {
                        case 0:
                            eventTriggerSystem.patient = foundCharacters[characterName];
                            if (enableDetailedLogging)
                                Debug.Log($"[ScenarioLoader] Mapped {scenarioData.characterNames[i]} to patient");
                            break;
                        case 1:
                            eventTriggerSystem.colleague = foundCharacters[characterName];
                            if (enableDetailedLogging)
                                Debug.Log($"[ScenarioLoader] Mapped {scenarioData.characterNames[i]} to colleague");
                            break;
                        case 2:
                            eventTriggerSystem.head_doctor = foundCharacters[characterName];
                            if (enableDetailedLogging)
                                Debug.Log($"[ScenarioLoader] Mapped {scenarioData.characterNames[i]} to head_doctor");
                            break;
                    }
                }
                else
                {
                    Debug.LogWarning($"[ScenarioLoader] Character '{scenarioData.characterNames[i]}' not found in scene!");
                }
            }

            // Warn if more than 3 characters are configured (EventTriggerSystem only supports 3)
            if (scenarioData.characterNames.Length > 3)
            {
                Debug.LogWarning($"[ScenarioLoader] Scenario has {scenarioData.characterNames.Length} characters, but EventTriggerSystem only supports 3. Extra characters will be ignored.");
            }
        }

        /// <summary>
        /// Apply found waypoint references to EventTriggerSystem
        /// Dynamic mapping based on availablePositions array
        /// </summary>
        private void ApplyWaypointReferences()
        {
            if (scenarioData.availablePositions == null || scenarioData.availablePositions.Length == 0)
            {
                Debug.LogWarning("[ScenarioLoader] No waypoint positions configured in scenario data!");
                return;
            }

            // Map waypoints dynamically based on common naming patterns
            foreach (string positionName in scenarioData.availablePositions)
            {
                string key = positionName.ToLower();
                if (foundWaypoints.ContainsKey(key))
                {
                    GameObject waypoint = foundWaypoints[key];

                    if (enableDetailedLogging)
                        Debug.Log($"[ScenarioLoader] Processed waypoint: {positionName}");
                }
            }
        }

        /// <summary>
        /// Set NPC starting positions based on scenario data
        /// </summary>
        private void SetNPCStartingPositions()
        {
            // Set Chefarzt position
            if (eventTriggerSystem.head_doctor != null)
            {
                eventTriggerSystem.head_doctor.transform.position = scenarioData.chefarztStartPosition.position;
                if (enableDetailedLogging)
                    Debug.Log($"[ScenarioLoader] Set Chefarzt position to {scenarioData.chefarztStartPosition}");
            }

            // Set Kollege position
            if (eventTriggerSystem.colleague != null)
            {
                eventTriggerSystem.colleague.transform.position = scenarioData.kollegeStartPosition.position;
                if (enableDetailedLogging)
                    Debug.Log($"[ScenarioLoader] Set Kollege position to {scenarioData.kollegeStartPosition.position}");
            }

            // Set Patient position
            if (eventTriggerSystem.patient != null)
            {
                eventTriggerSystem.patient.transform.position = scenarioData.patientStartPosition.position;
                if (enableDetailedLogging)
                    Debug.Log($"[ScenarioLoader] Set Patient position to {scenarioData.patientStartPosition.position}");
            }

            // Set participant (main camera) position
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.position = scenarioData.participantStartPosition.transform.position;
                if (enableDetailedLogging)
                    Debug.Log($"[ScenarioLoader] Set participant position to {scenarioData.participantStartPosition}");
            }
        }

        /// <summary>
        /// Get a character GameObject by name (for external access)
        /// </summary>
        public GameObject GetCharacter(string characterName)
        {
            string key = characterName.ToLower();
            return foundCharacters.ContainsKey(key) ? foundCharacters[key] : null;
        }

        /// <summary>
        /// Get a waypoint GameObject by name (for external access)
        /// </summary>
        public GameObject GetWaypoint(string waypointName)
        {
            string key = waypointName.ToLower();
            return foundWaypoints.ContainsKey(key) ? foundWaypoints[key] : null;
        }

        /// <summary>
        /// Get a camera by name (for external access)
        /// </summary>
        public Camera GetCamera(string cameraName)
        {
            string key = cameraName.ToLower();
            return foundCameras.ContainsKey(key) ? foundCameras[key] : null;
        }

        /// <summary>
        /// Reload scenario data (useful for runtime scenario switching)
        /// </summary>
        public void ReloadScenario(SO_ScenarioData newScenarioData)
        {
            scenarioData = newScenarioData;
            LoadScenarioData();
        }

        /// <summary>
        /// Validate that all required objects were found
        /// </summary>
        [ContextMenu("Validate Scenario Loading")]
        public void ValidateScenarioLoading()
        {
            if (scenarioData == null)
            {
                Debug.LogError("[ScenarioLoader] No scenario data assigned!");
                return;
            }

            Debug.Log($"[ScenarioLoader] Validation for scenario: {scenarioData.scenarioName}");

            // Validate characters
            Debug.Log($"Characters found: {foundCharacters.Count}");
            foreach (var kvp in foundCharacters)
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value.name}");
            }

            // Validate waypoints
            Debug.Log($"Waypoints found: {foundWaypoints.Count}");
            foreach (var kvp in foundWaypoints)
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value.name}");
            }

            // Validate cameras
            Debug.Log($"Cameras found: {foundCameras.Count}");
            foreach (var kvp in foundCameras)
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value.name}");
            }
        }
    }
}
