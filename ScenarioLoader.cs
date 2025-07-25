
using UnityEngine;
using System.Collections.Generic;
using System;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Loads scenario data from ScriptableObject and applies it to scene components
    /// This component bridges the gap between persistent ScriptableObject data and scene-specific GameObjects
    /// Furthermore, it is responsible for returning references to characters, waypoints, and cameras to the message system
    /// </summary>
    public class ScenarioLoader : MonoBehaviour
    {
        [Header("Scenario Configuration")]
        [SerializeField] private SO_ScenarioData scenarioData;

        [Header("Debug Settings")]
        [SerializeField] private bool enableDetailedLogging = true;

        // Cached references to found GameObjects
        private Dictionary<string, GameObject> foundCharacters = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> foundWaypoints = new Dictionary<string, GameObject>();
        private Dictionary<string, Camera> foundCameras = new Dictionary<string, Camera>();



        [System.Serializable]
        public class StudySetupResponse
        {
            public NPCData[] npcs;
            public string[] positions;
        }

        [System.Serializable]
        public class NPCData
        {
            public string name;
            public string displayName;
            public AudioFileData[] audioFiles;
        }

        [System.Serializable]
        public class AudioFileData
        {
            public string clipName;
            public string label;
        }


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

            if (scenarioData.cameraTags != null)
            {
                foreach (string cameraTag in scenarioData.cameraTags)
                {
                    GameObject cameraObj = GameObject.FindGameObjectWithTag(cameraTag);
                    if (cameraObj != null)
                    {
                        Camera camera = cameraObj.GetComponent<Camera>();
                        if (camera != null)
                        {
                            foundCameras[cameraTag.ToLower()] = camera;
                            if (enableDetailedLogging)
                                Debug.Log($"[ScenarioLoader] Found camera: {cameraTag}");
                        }
                        else
                        {
                            Debug.LogWarning($"[ScenarioLoader] GameObject {cameraTag} found but has no Camera component");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[ScenarioLoader] Camera not found: {cameraTag}");
                    }
                }
            }
        }

        /// <summary>
        /// Build JSON data for study setup response
        /// </summary>
        internal string BuildStudySetupData()
        {
            Debug.Log("[MessageHandler] Building study setup data");

            var npcDataList = new List<NPCData>();
            // Get all NPCs from scenario data
            if (scenarioData.characterNames != null)
            {
                Debug.Log($"[MessageHandler] Found {scenarioData.characterNames.Length} characters in scenario data");
                foreach (string characterName in scenarioData.characterNames)
                {
                    Debug.Log($"[MessageHandler] Processing character: {characterName}");
                    GameObject npcObject = GetCharacter(characterName);
                    if (npcObject != null)
                    {
                        Debug.Log($"[MessageHandler] Found NPC object for: {characterName}");

                        // Get audio data from NPC controller
                        var (clipNames, labels) = scenarioData.GetAudioDataForNPC(characterName);
                        //Get audio data and labels from scenario data

                        Debug.Log($"[MessageHandler] {characterName} has {clipNames.Length} audio clips");
                        // Build audio files array
                        var audioFiles = new List<AudioFileData>();
                        for (int i = 0; i < clipNames.Length; i++)
                        {
                            audioFiles.Add(new AudioFileData
                            {
                                clipName = clipNames[i],
                                label = i < labels.Length ? labels[i] : clipNames[i]
                            });
                        }

                        // Create NPC data object
                        var npcData = new NPCData
                        {
                            name = characterName.ToLower(),
                            displayName = CapitalizeFirstLetter(characterName),
                            audioFiles = audioFiles.ToArray()
                        };

                        npcDataList.Add(npcData);

                        if (enableDetailedLogging)
                            Debug.Log($"[MessageHandler] Added NPC data for: {characterName} with {clipNames.Length} audio clips");

                    }
                    else
                    {
                        Debug.LogWarning($"[MessageHandler] NPC {characterName} not found in scene!");
                    }
                }
            }

            // Get available positions
            string[] positions = scenarioData.availablePositions ?? new string[0];

            // Create the complete response object
            var responseData = new StudySetupResponse
            {
                npcs = npcDataList.ToArray(),
                positions = scenarioData.availablePositions ?? new string[0]
            };

            // Convert to JSON string
            return JsonUtility.ToJson(responseData);
        }



        /// <summary>
        /// Apply scenario data to EventTriggerSystem
        /// </summary>
        private void ApplyToEventTriggerSystem()
        {
            // Apply character references
            GetCharacterReferences();

            // Apply waypoint references
            GetWaypointReferences();

            if (enableDetailedLogging)
                Debug.Log("[ScenarioLoader] Applied scenario data to EventTriggerSystem");
        }

        /// <summary>
        /// Apply found character references to EventTriggerSystem
        /// Dynamic mapping based on characterNames array order
        /// </summary>
        private List<GameObject> GetCharacterReferences()
        {
            if (scenarioData.characterNames == null || scenarioData.characterNames.Length == 0)
            {
                Debug.LogWarning("[ScenarioLoader] No character names configured in scenario data!");
                return null;
            }

            List<GameObject> characters = new List<GameObject>();
            for (int i = 0; i < scenarioData.characterNames.Length && i < 3; i++)
            {
                string characterName = scenarioData.characterNames[i].ToLower();
                if (foundCharacters.ContainsKey(characterName))
                {
                    characters.Add(foundCharacters[characterName]);
                    if (enableDetailedLogging)
                        Debug.Log($"[ScenarioLoader] Mapped character: {scenarioData.characterNames[i]}");
                }
                else
                {
                    Debug.LogWarning($"[ScenarioLoader] Character '{scenarioData.characterNames[i]}' not found in scene!");
                }

            }
            return characters;
        }

        /// <summary>
        /// Apply found waypoint references to EventTriggerSystem
        /// Dynamic mapping based on availablePositions array
        /// </summary>
        private List<Transform> GetWaypointReferences()
        {
            if (scenarioData.availablePositions == null || scenarioData.availablePositions.Length == 0)
            {
                Debug.LogWarning("[ScenarioLoader] No waypoint positions configured in scenario data!");
                return null;
            }

            List<Transform> waypoints = new List<Transform>();
            foreach (string positionName in scenarioData.availablePositions)
            {
                string key = positionName.ToLower();
                if (foundWaypoints.ContainsKey(key))
                {
                    waypoints.Add(foundWaypoints[key].transform);
                    if (enableDetailedLogging)
                        Debug.Log($"[ScenarioLoader] Mapped waypoint: {positionName}");
                }
                else
                {
                    Debug.LogWarning($"[ScenarioLoader] Waypoint '{positionName}' not found in scene!");
                }
            }
            return waypoints;
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

        /// <summary>
        /// Helper method to capitalize first letter of a string
        /// </summary>
        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }
    }
}
