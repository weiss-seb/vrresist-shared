using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.UI;
using TextureSendReceive;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Manages scene loading for different scenarios
    /// Each scenario has its own Unity scene with unique location and setup
    /// </summary>
    public class ScenarioSceneManager : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] ScenarioLoader scenarioLoader;

        [Header("Loading UI")]
        [SerializeField] GameObject loadingScreen;
        [SerializeField] TMP_Text loadingScreenTitle;
        [SerializeField] Slider loadingProgressSlider;
        [SerializeField] TMP_Text progressText;
        [SerializeField] TMP_Text scenarioInfoText;

        [Header("Network")]
        [SerializeField] TCPServer tcpServer;

        [Header("Debug")]
        [SerializeField] bool enableDetailedLogging = true;

        [Header("UI Timing")]
        [SerializeField] float scenarioInfoDisplaySeconds = 2f;

        private int currentScenarioId = -1;
        private bool isLoading = false;

        [SerializeField]
        private GameObject XRUser;


        [System.Serializable]
        public class ScenarioMapping
        {
            public string scenarioId;
            public string scenarioTitle;
            public string sceneName;
            public string loadingMessage;
        }

        [SerializeField]
        private List<ScenarioMapping> scenarioMappings = new List<ScenarioMapping>();

        private static ScenarioSceneManager _instance;
        public static ScenarioSceneManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ScenarioSceneManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ScenarioSceneManager");
                        _instance = go.AddComponent<ScenarioSceneManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Singleton enforcement
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);

            }
            else if (_instance != this)
            {
                Debug.LogWarning("[TCPServer] Duplicate TCPServer instance found. Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        void Start()
        {
            ShowLoadingScreen();
            DontDestroyOnLoad(this.gameObject);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log("[ScenarioSceneManager] OnSceneLoaded called");

            scenarioLoader = FindObjectOfType<ScenarioLoader>();
            tcpServer = FindObjectOfType<TCPServer>();
            GameObject.Find("MessageHandler").GetComponent<MessageHandler>().scenarioSceneManager = this;

            if (XRUser == null)
            {
                XRUser = GameObject.FindWithTag("XRUser");
            }

            this.gameObject.transform.position = XRUser.transform.position;

            ShowLoadingScreen();

            if (enableDetailedLogging)
                Debug.Log($"[ScenarioSceneManager] Scene loaded: {scene.name} in mode {mode}");

            // Notify listeners that a new scenario has been loaded
            var statusMessage = new EventMessage("SCENE_LOADED", new string[] { scene.name });
            string json = JsonUtility.ToJson(statusMessage);
            tcpServer.SendMessageToClient(json);

            isLoading = false;
        }

        /// <summary>
        /// Load scenario scene by ID (called from MessageHandler)
        /// </summary>
        public void LoadScenarioScene(int scenarioId)
        {
            if (isLoading)
            {
                Debug.LogWarning("[ScenarioSceneManager] Already loading a scene, ignoring request");
                return;
            }

            // find scnenario mapping
            var mapping = scenarioMappings.Find(m => m.scenarioId == scenarioId.ToString());

            var statusmuessage = new EventMessage("SCENE_REQUESTED", new string[] { mapping.sceneName });
            string json = JsonUtility.ToJson(statusmuessage);
            tcpServer.SendMessageToClient(json);

            StartCoroutine(LoadSceneCoroutine(mapping));

        }

        internal void LoadWaitingRoomScene()
        {
            //find mapping for waiting room
            var mapping = scenarioMappings.Find(m => m.scenarioId == "0");

            StartCoroutine(LoadSceneCoroutine(mapping));
        }

        internal void LoadQuestionnaireScene()
        {
            Debug.Log("[ScenarioSceneManager] Loading Questionnaire Scene");

            //Save current scenario id
            PlayerPrefs.SetInt("LastScenarioId", currentScenarioId);
            PlayerPrefs.Save();


            //get mapping for questionnaoire id 888
            var questionnaireScene = scenarioMappings.Find(m => m.scenarioId == "888");

            StartCoroutine(LoadSceneCoroutine(questionnaireScene));
        }

        public string GetScenarioHelpText(int scenarioId)
        {
            //find mapping by id
            var mapping = scenarioMappings.Find(m => m.scenarioId == scenarioId.ToString());

            return mapping?.loadingMessage ?? "Keine Informationen für dieses Szenario verfügbar.";
        }

        /// <summary>
        /// Coroutine to handle scene loading with loading screen
        /// </summary>
        IEnumerator LoadSceneCoroutine(ScenarioMapping mapping)
        {
            isLoading = true;

            // Stop texture transmission before loading new scene
            var textureSender = FindObjectOfType<_TextureSender>();
            if (textureSender != null)
            {
                Debug.Log("[ScenarioSceneManager] Stopping texture transmission before scene load");
                textureSender.StopTransmission();
            }

            if (enableDetailedLogging)
                Debug.Log($"[ScenarioSceneManager] Loading scenario {mapping.scenarioId}: {mapping.scenarioTitle}");

            // Show loading screen
            ShowLoadingScreen();

            // Send loading status to tablet
            SendStatusToTablet("SCENE_LOADING", mapping.sceneName);

            // Wait a frame to ensure UI updates
            yield return null;

            // Check if scene exists in build settings
            if (!IsSceneInBuildSettings(mapping.sceneName))
            {
                Debug.LogError($"[ScenarioSceneManager] Scene '{mapping.sceneName}' not found in build settings!");
                ShowLoadingScreen();
                SendStatusToTablet("SCENE_ERROR", $"Scene {mapping.sceneName} not found");
                isLoading = false;
                yield break;
            }

            // Load the scene asynchronously as single scene

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mapping.sceneName, LoadSceneMode.Single);
            asyncLoad.allowSceneActivation = true;
            // Update loading progress
            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                UpdateLoadingProgress(progress);

                // Scene is ready to activate
                if (asyncLoad.progress >= 0.9f)
                {
                    // Wait a moment for smooth transition
                    yield return new WaitForSeconds(0.1f);

                    // Activate the scene
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }

            // Scene loaded successfully
            if (enableDetailedLogging)
                Debug.Log($"[ScenarioSceneManager] Successfully loaded scenario scene: {mapping.sceneName}");

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
        void ShowLoadingScreen()
        {

            Debug.Log("[ScenarioSceneManager] Showing loading screen");
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(true);
            }

            if (scenarioInfoText != null)
            {
                Debug.Log("[ScenarioSceneManager] Setting scenario info text" + scenarioLoader.GetLoadingScreenText());
                scenarioInfoText.text = scenarioLoader.GetLoadingScreenText();
            }
            if (loadingScreenTitle != null)
            {
                loadingScreenTitle.text = scenarioLoader.GetLoadingScreenTitle();
            }

            StartCoroutine(FadeInOut(0.5f, true));
        }

        public void CloseLoadingScreen()
        {
            if (loadingScreen != null)
            {
                //fade effect
                StartCoroutine(FadeInOut(0.5f, false));


                //teleport user to start position
                if (XRUser != null)
                {
                    XRUser.transform.position = scenarioLoader.GetStartPosition();
                }
            }
        }

        IEnumerator FadeInOut(float duration, bool fadein)
        {
            Debug.Log("[ScenarioSceneManager] Starting fade " + (fadein ? "in" : "out"));
            CanvasGroup canvasGroup = loadingScreen.GetComponent<CanvasGroup>();
            float startAlpha = canvasGroup.alpha;
            float targetAlpha = fadein ? 1 : 0;
            float time = 0;

            while (time < duration)
            {
                time += Time.deltaTime;
                if (fadein)
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
                else
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
                yield return null;
            }


            loadingScreen.SetActive(fadein);
        }


        /// <summary>
        /// Update loading progress
        /// </summary>
        void UpdateLoadingProgress(float progress)
        {
            if (progressText != null)
            {
                int percentage = Mathf.RoundToInt(progress * 100);
                progressText.text = $"{percentage}%";
            }

            //update progress slider
            if (loadingProgressSlider != null)
            {
                loadingProgressSlider.value = progress;
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

        //Todo possibly move from mapping to scenarioscenes
        public string[] GetAllSceneInfo()
        {
            var sceneInfoList = new List<string>();
            foreach (var mapping in scenarioMappings)
            {
                sceneInfoList.Add(mapping.scenarioId);
                sceneInfoList.Add(mapping.scenarioTitle);
                sceneInfoList.Add(mapping.loadingMessage);
            }
            return sceneInfoList.ToArray();
        }

        public string[] GetSceneListInfo()
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
    [Tooltip("Information texts displayed to user during the scenario")]
    public string[] scenarioInfoTexts;
}
