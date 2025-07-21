using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Manages scene loading for different scenarios
    /// Each scenario has its own Unity scene with unique location and setup
    /// </summary>
    public class ScenarioSceneManager : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] ScenarioSceneData[] scenarioScenes = new ScenarioSceneData[5];

        [Header("Loading UI")]
        [SerializeField] GameObject loadingScreen;
        [SerializeField] TMP_Text loadingText;
        [SerializeField] TMP_Text scenarioInfoText;

        [Header("Network")]
        [SerializeField] TCPServer tcpServer;

        [Header("Debug")]
        [SerializeField] bool enableDetailedLogging = true;

        private int currentScenarioId = -1;
        private bool isLoading = false;

        public UnityEvent<EventMessage> OnScenarioLoaded;


        [System.Serializable]
        public class ScenarioMapping
        {
            public string scenarioId;
            public string scenarioTitle;
            public string sceneName;
        }

        [SerializeField]
        private List<ScenarioMapping> scenarioMappings = new List<ScenarioMapping>();

        void Start()
        {
            InitializeSceneData();
            ShowLoadingScreen("Warten auf Szenario-Auswahl...");

            DontDestroyOnLoad(this.gameObject);
        }

        /// <summary>
        /// Initialize default scene data if not configured
        /// </summary>
        void InitializeSceneData()
        {
            if (scenarioScenes == null || scenarioScenes.Length == 0)
            {
                scenarioScenes = new ScenarioSceneData[5];
            }

            for (int i = 0; i < scenarioScenes.Length; i++)
            {
                if (scenarioScenes[i] == null)
                {
                    scenarioScenes[i] = CreateDefaultSceneData(i);
                }
            }

            if (enableDetailedLogging)
                Debug.Log($"[ScenarioSceneManager] Initialized {scenarioScenes.Length} scenario scenes");
        }

        /// <summary>
        /// Create default scene data for a scenario
        /// </summary>
        ScenarioSceneData CreateDefaultSceneData(int scenarioId)
        {
            var sceneData = new ScenarioSceneData();
            sceneData.scenarioId = scenarioId;
            sceneData.scenarioName = $"Scenario {scenarioId + 1}";
            sceneData.sceneName = $"Scenario{scenarioId + 1}Scene";
            sceneData.loadingMessage = $"Lade Scenario {scenarioId + 1}...";
            sceneData.scenarioInfoText = $"Sie befinden sich in Scenario {scenarioId + 1}.\nBitte folgen Sie den Anweisungen des Studienleiters.";
            return sceneData;
        }

        /// <summary>
        /// Load scenario scene by ID (called from MessageHandler)
        /// </summary>
        public void LoadScenarioScene(int scenarioId, string scenarioInfoText = "")
        {
            if (isLoading)
            {
                Debug.LogWarning("[ScenarioSceneManager] Already loading a scene, ignoring request");
                return;
            }

            if (scenarioId < 0 || scenarioId >= scenarioScenes.Length)
            {
                Debug.LogError($"[ScenarioSceneManager] Invalid scenario ID: {scenarioId}");
                return;
            }

            var sceneData = scenarioScenes[scenarioId - 1]; //Numbering of Scenario IDs begins at 1
            if (sceneData == null)
            {
                Debug.LogError($"[ScenarioSceneManager] Scene data for scenario {scenarioId} is null");
                return;
            }

            // Use provided info text or fall back to configured text
            string infoText = !string.IsNullOrEmpty(scenarioInfoText) ? scenarioInfoText : sceneData.scenarioInfoText;

            StartCoroutine(LoadSceneCoroutine(sceneData, infoText));
        }

        /// <summary>
        /// Coroutine to handle scene loading with loading screen
        /// </summary>
        IEnumerator LoadSceneCoroutine(ScenarioSceneData sceneData, string infoText)
        {
            isLoading = true;
            currentScenarioId = sceneData.scenarioId;

            if (enableDetailedLogging)
                Debug.Log($"[ScenarioSceneManager] Loading scenario {sceneData.scenarioId}: {sceneData.scenarioName}");

            // Show loading screen
            ShowLoadingScreen(sceneData.loadingMessage);

            // Send loading status to tablet
            SendStatusToTablet("SCENE_LOADING", sceneData.scenarioName);

            // Wait a frame to ensure UI updates
            yield return null;

            // Check if scene exists in build settings
            if (!IsSceneInBuildSettings(sceneData.sceneName))
            {
                Debug.LogError($"[ScenarioSceneManager] Scene '{sceneData.sceneName}' not found in build settings!");
                ShowLoadingScreen($"Fehler: Szene '{sceneData.sceneName}' nicht gefunden!");
                SendStatusToTablet("SCENE_ERROR", $"Scene {sceneData.sceneName} not found");
                isLoading = false;
                yield break;
            }

            // Load the scene asynchronously and additively
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneData.sceneName, LoadSceneMode.Additive);
            asyncLoad.allowSceneActivation = false;

            //remove old scene if it exists, but not the first one
            if (SceneManager.sceneCount > 2)
            {
                Scene oldScene = SceneManager.GetSceneAt(1); // Assuming the first scene is always the main scene
                if (oldScene.isLoaded)
                {
                    Debug.Log($"[ScenarioSceneManager] Unloading old scene: {oldScene.name}");
                    yield return SceneManager.UnloadSceneAsync(oldScene);
                }
            }

            // Update loading progress
            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                UpdateLoadingProgress(sceneData.loadingMessage, progress);

                // Scene is ready to activate
                if (asyncLoad.progress >= 0.9f)
                {
                    // Wait a moment for smooth transition
                    yield return new WaitForSeconds(0.5f);

                    // Activate the scene
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }

            // Scene loaded successfully
            if (enableDetailedLogging)
                Debug.Log($"[ScenarioSceneManager] Successfully loaded scenario scene: {sceneData.sceneName}");

            // Hide loading screen and show scenario info
            HideLoadingScreen();
            DisplayScenarioInfo(infoText);

            // Send success status to tablet
            SendStatusToTablet("SCENE_LOADED", sceneData.scenarioName);

            isLoading = false;
        }

        /// <summary>
        /// Check if scene exists in build settings
        /// </summary>
        bool IsSceneInBuildSettings(string sceneName)
        {
            Debug.Log($"[ScenarioSceneManager] Amount of scenes in build settings: {SceneManager.sceneCountInBuildSettings}");
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                Debug.Log($"[ScenarioSceneManager] Checking scene : {scenePath}");
                string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneNameFromPath == sceneName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Show loading screen with message
        /// </summary>
        void ShowLoadingScreen(string message)
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(true);
            }

            if (loadingText != null)
            {
                loadingText.text = message;
            }

            // Hide scenario info during loading
            if (scenarioInfoText != null)
            {
                scenarioInfoText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Update loading progress
        /// </summary>
        void UpdateLoadingProgress(string baseMessage, float progress)
        {
            if (loadingText != null)
            {
                int percentage = Mathf.RoundToInt(progress * 100);
                loadingText.text = $"{baseMessage}\n{percentage}%";
            }
        }

        /// <summary>
        /// Hide loading screen
        /// </summary>
        void HideLoadingScreen()
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(false);
            }
        }

        /// <summary>
        /// Display scenario information text
        /// </summary>
        void DisplayScenarioInfo(string infoText)
        {
            if (scenarioInfoText != null && !string.IsNullOrEmpty(infoText))
            {
                scenarioInfoText.text = infoText;
                scenarioInfoText.gameObject.SetActive(true);

                if (enableDetailedLogging)
                    Debug.Log($"[ScenarioSceneManager] Displaying scenario info: {infoText}");
            }
        }

        /// <summary>
        /// Send status updates to tablet
        /// </summary>
        void SendStatusToTablet(string statusType, string message)
        {
            if (tcpServer != null)
            {
                var statusMessage = new EventMessage(statusType, new string[] { message });
                string json = JsonUtility.ToJson(statusMessage);
                tcpServer.SendMessageToClient(json);

                if (enableDetailedLogging)
                    Debug.Log($"[ScenarioSceneManager] Sent status to tablet: {statusType} - {message}");
            }
        }

        /// <summary>
        /// Get current scenario ID
        /// </summary>
        public int GetCurrentScenarioId()
        {
            return currentScenarioId;
        }

        /// <summary>
        /// Get scenario scene data by ID
        /// </summary>
        public ScenarioSceneData GetScenarioSceneData(int scenarioId)
        {
            if (scenarioId >= 0 && scenarioId < scenarioScenes.Length)
            {
                return scenarioScenes[scenarioId];
            }
            return null;
        }

        /// <summary>
        /// Validate all scenario scenes (check if scenes exist in build settings)
        /// </summary>
        [ContextMenu("Validate Scenario Scenes")]
        public void ValidateScenarioScenes()
        {
            if (scenarioScenes == null)
            {
                Debug.LogWarning("[ScenarioSceneManager] No scenario scenes configured!");
                return;
            }

            Debug.Log("[ScenarioSceneManager] Validating scenario scenes:");

            for (int i = 0; i < scenarioScenes.Length; i++)
            {
                var sceneData = scenarioScenes[i];
                if (sceneData == null)
                {
                    Debug.LogWarning($"  Scenario {i}: NULL");
                    continue;
                }

                bool sceneExists = IsSceneInBuildSettings(sceneData.sceneName);
                string status = sceneExists ? " EXISTS" : " MISSING";

                Debug.Log($"  Scenario {i}: {sceneData.scenarioName} -> {sceneData.sceneName} [{status}]");
            }
        }

        public string[] GetAllSceneInfo()
        {
            var sceneInfoList = new List<string>();
            foreach (var mapping in scenarioMappings)
            {
                sceneInfoList.Add(mapping.scenarioId);
                sceneInfoList.Add(mapping.scenarioTitle);
            }
            return sceneInfoList.ToArray();
        }


    }
}

/// <summary>
/// Data structure for scenario scene configuration
/// </summary>
[System.Serializable]
public class ScenarioSceneData
{
    [Header("Scenario Information")]
    public int scenarioId;
    public string scenarioName;

    [Header("Scene Configuration")]
    [Tooltip("Name of the Unity scene to load for this scenario")]
    public string sceneName;

    [Header("Loading Configuration")]
    [Tooltip("Message displayed during scene loading")]
    public string loadingMessage;

    [Header("Scenario Info")]
    [TextArea(3, 6)]
    [Tooltip("Information text displayed to user after scene loads")]
    public string scenarioInfoText;
}

