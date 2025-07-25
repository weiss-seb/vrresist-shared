using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Tablet-specific event control system for VR study
    /// This version works purely through message passing without direct NPC references
    /// Designed to run on the remote tablet UI that connects to the HMD via WebSocket
    /// Now supports multiple scenarios with dynamic UI population
    /// </summary>
    public class TabletEventControl : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] WebSocketClient webSocketClient;
        [Header("Scenario Selection")]


        [SerializeField] TMP_Dropdown scenarioDropdown;

        [Header("Pre-configured UI Sections")]
        [Tooltip("UI Panels - configure these in Unity Editor with pre-made buttons")]
        [SerializeField] GameObject npcActionsPanel;
        [SerializeField] GameObject taskActionsPanel;
        [SerializeField] GameObject cameraActionsPanel;
        [SerializeField] GameObject studyControlPanel;

        [Header("NPC Action Buttons - Assign in Unity Editor")]
        [Tooltip("NPC Movement Buttons")]
        [SerializeField] Button[] chefarztWalkButtons;
        [SerializeField] Button[] kollegeWalkButtons;
        [SerializeField] Button[] patientWalkButtons;

        [Tooltip("NPC Speech Buttons - Configure with predefined audio clip names")]
        [SerializeField] Button[] chefarztTalkButtons;
        [SerializeField] Button[] kollegeTalkButtons;
        [SerializeField] Button[] patientTalkButtons;

        [Header("Task Action Buttons - Assign in Unity Editor")]
        [SerializeField] Button showMathTaskButton;
        [SerializeField] Button showNBackTaskButton;
        [SerializeField] Button hideMathTaskButton;
        [SerializeField] Button hideNBackTaskButton;

        [Header("Camera Control Buttons - Assign in Unity Editor")]
        [SerializeField] Button[] cameraButtons; // Camera 1, 2, 3, 4, etc.

        [Header("Study Control Buttons - Assign in Unity Editor")]
        [SerializeField] Button abortAllButton;
        [SerializeField] Button endStudyButton;
        [SerializeField] Button refreshButton;



        [Header("Dynamic Audio Configuration")]
        [Tooltip("Audio clips and labels are loaded dynamically from ScenarioManager")]
        [SerializeField] string[] chefarztAudioClips = new string[0];
        [SerializeField] string[] kollegeAudioClips = new string[0];
        [SerializeField] string[] patientAudioClips = new string[0];
        [SerializeField] string[] chefarztAudioLabels = new string[0];
        [SerializeField] string[] kollegeAudioLabels = new string[0];
        [SerializeField] string[] patientAudioLabels = new string[0];

        [Header("Dynamic UI Generation")]
        [Tooltip("Button prefab for dynamically created audio buttons")]
        [SerializeField] GameObject buttonPrefab;
        [Tooltip("Parent containers for dynamically created NPC buttons")]
        [SerializeField] Transform chefarztButtonContainer;
        [SerializeField] Transform kollegeButtonContainer;
        [SerializeField] Transform patientButtonContainer;
        [Tooltip("Parent container for dynamically created position buttons")]
        [SerializeField] Transform positionButtonContainer;

        [Header("Debug Settings")]
        [SerializeField] bool enableDetailedLogging = true;

        // Lists to keep track of dynamically created buttons for cleanup
        private List<GameObject> dynamicAudioButtons = new List<GameObject>();
        private List<GameObject> dynamicPositionButtons = new List<GameObject>();

        void Start()
        {
            // Setup scenario dropdown
            //SetupScenarioDropdown();

            // Setup pre-configured UI button listeners
            // SetupPreConfiguredUI();

            // Request initial data from HMD
            // RequestInitialData();
        }

        /// <summary>
        /// Setup scenario dropdown and subscribe to scenario changes
        /// </summary>
        public void SetupScenarioDropdown(EventMessage message)
        {

            //get the scenario list from the event message

            PopulateScenarioDropdown(message.content);

            // Load initial scenario

            if (enableDetailedLogging)
                Debug.Log("[TabletEventControl] Scenario dropdown setup complete");
        }

        /// <summary>
        /// Populate scenario dropdown with available scenarios
        /// </summary>
        void PopulateScenarioDropdown(string[] content)
        {
            if (scenarioDropdown == null) return;

            scenarioDropdown.ClearOptions();

            //Unpack the content, odd entries are scenario ids, even entries are scenario names 
            var scenarioNames = new List<string>();
            for (int i = 0; i < content.Length; i += 2)
            {
                if (i + 1 < content.Length)
                {
                    var scenarioId = content[i];
                    var scenarioName = content[i + 1];
                    scenarioNames.Add(scenarioName);
                }
            }

            var options = new List<TMP_Dropdown.OptionData>();

            foreach (var name in scenarioNames)
            {
                options.Add(new TMP_Dropdown.OptionData(name));
            }

            scenarioDropdown.AddOptions(options);

            if (enableDetailedLogging)
                Debug.Log($"[TabletEventControl] Populated dropdown with {options.Count} scenarios");
        }


        public void StartSelectedScenario()
        {
            //Send a message to HMD to load up the selected scenario
            if (webSocketClient != null)
            {
                var message = new EventMessage("SCENARIO_CHANGE", new string[] { (scenarioDropdown.value + 1).ToString() });
                webSocketClient.SendEventMessage(message);
                Debug.Log($"[TabletEventControl] Starting scenario:");
            }
            else
            {
                Debug.LogWarning("[TabletEventControl] WebSocketClient is null or no scenario selected!");
            }

        }

        /// <summary>
        /// Update UI elements based on current scenario
        /// </summary>
        void UpdateUIForScenario(SO_ScenarioData scenario)
        {
            //todo add back buttons for NPC walk positions

            // Update audio clips and labels for each NPC
            UpdateNPCAudioData("chefarzt", scenario.GetAudioClipsForNPC("chefarzt"), scenario.GetAudioLabelsForNPC("chefarzt"));
            UpdateNPCAudioData("kollege", scenario.GetAudioClipsForNPC("kollege"), scenario.GetAudioLabelsForNPC("kollege"));
            UpdateNPCAudioData("patient", scenario.GetAudioClipsForNPC("patient"), scenario.GetAudioLabelsForNPC("patient"));

            // Refresh button configurations
            RefreshButtonConfigurations();

            if (enableDetailedLogging)
                Debug.Log($"[TabletEventControl] UI updated for scenario: {scenario.scenarioName}");
        }

        /// <summary>
        /// Update NPC audio data for scenario
        /// </summary>
        void UpdateNPCAudioData(string npcName, string[] audioClips, string[] audioLabels)
        {
            if (audioClips == null || audioClips.Length == 0) return;

            switch (npcName.ToLower())
            {
                case "chefarzt":
                    chefarztAudioClips = audioClips;
                    chefarztAudioLabels = audioLabels ?? audioClips;
                    break;
                case "kollege":
                    kollegeAudioClips = audioClips;
                    kollegeAudioLabels = audioLabels ?? audioClips;
                    break;
                case "patient":
                    patientAudioClips = audioClips;
                    patientAudioLabels = audioLabels ?? audioClips;
                    break;
            }
        }

        /// <summary>
        /// Refresh all button configurations after scenario change
        /// </summary>
        void RefreshButtonConfigurations()
        {
            // Clear existing button listeners
            ClearButtonListeners();

            // Re-setup all buttons with new scenario data
            SetupNPCActionButtons();
            SetupTaskActionButtons();
            SetupStudyControlButtons();
        }

        /// <summary>
        /// Clear all existing button listeners
        /// </summary>
        void ClearButtonListeners()
        {
            // Clear walk button listeners
            ClearButtonArrayListeners(chefarztWalkButtons);
            ClearButtonArrayListeners(kollegeWalkButtons);
            ClearButtonArrayListeners(patientWalkButtons);

            // Clear talk button listeners
            ClearButtonArrayListeners(chefarztTalkButtons);
            ClearButtonArrayListeners(kollegeTalkButtons);
            ClearButtonArrayListeners(patientTalkButtons);

            // Clear other button listeners
            ClearButtonArrayListeners(cameraButtons);

            if (showMathTaskButton != null) showMathTaskButton.onClick.RemoveAllListeners();
            if (showNBackTaskButton != null) showNBackTaskButton.onClick.RemoveAllListeners();
            if (hideMathTaskButton != null) hideMathTaskButton.onClick.RemoveAllListeners();
            if (hideNBackTaskButton != null) hideNBackTaskButton.onClick.RemoveAllListeners();
            if (abortAllButton != null) abortAllButton.onClick.RemoveAllListeners();
            if (endStudyButton != null) endStudyButton.onClick.RemoveAllListeners();
            if (refreshButton != null) refreshButton.onClick.RemoveAllListeners();
        }

        /// <summary>
        /// Clear listeners for an array of buttons
        /// </summary>
        void ClearButtonArrayListeners(Button[] buttons)
        {
            if (buttons == null) return;

            foreach (var button in buttons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }
        }

        /// <summary>
        /// Request initial data from HMD (audio clips, available positions, etc.)
        /// </summary>
        public void RequestInitialData()
        {
            if (webSocketClient != null)
            {
                var refreshMessage = new EventMessage("request", new string[] { "refresh" });
                webSocketClient.SendEventMessage(refreshMessage);
                Debug.Log("[TabletEventControl] Requested initial data from HMD");
            }
        }

        /// <summary>
        /// Setup pre-configured UI button listeners
        /// This method assigns actions to buttons that are already created in the Unity Editor
        /// </summary>
        void SetupPreConfiguredUI()
        {
            Debug.Log("[TabletEventControl] Setting up pre-configured UI button listeners...");

            SetupNPCActionButtons();
            SetupTaskActionButtons();
            SetupStudyControlButtons();

            Debug.Log("[TabletEventControl] Pre-configured UI setup complete!");
        }

        /// <summary>
        /// Setup NPC action buttons - assigns listeners to pre-configured buttons
        /// </summary>
        void SetupNPCActionButtons()
        {

            // Setup talk buttons for each NPC
            SetupTalkButtons(chefarztTalkButtons, "chefarzt", chefarztAudioClips, chefarztAudioLabels);
            SetupTalkButtons(kollegeTalkButtons, "kollege", kollegeAudioClips, kollegeAudioLabels);
            SetupTalkButtons(patientTalkButtons, "patient", patientAudioClips, patientAudioLabels);
        }



        /// <summary>
        /// Setup talk buttons for a specific NPC
        /// </summary>
        void SetupTalkButtons(Button[] buttons, string npcName, string[] audioClips, string[] audioLabels)
        {
            if (buttons == null || audioClips == null) return;

            for (int i = 0; i < buttons.Length && i < audioClips.Length; i++)
            {
                if (buttons[i] != null)
                {
                    string audioClip = audioClips[i];
                    buttons[i].onClick.AddListener(() => SendNPCTalkCommand(npcName, audioClip));

                    // Update button text to show friendly name
                    var buttonText = buttons[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (buttonText != null && audioLabels != null && i < audioLabels.Length)
                    {
                        buttonText.text = audioLabels[i];
                    }

                    if (enableDetailedLogging)
                        Debug.Log($"[TabletEventControl] Setup talk button: {npcName} says {audioClip}");
                }
            }
        }

        /// <summary>
        /// Setup task action buttons
        /// </summary>
        void SetupTaskActionButtons()
        {
            if (showMathTaskButton != null)
            {
                showMathTaskButton.onClick.AddListener(() => SendTaskCommand("MATH_TASK", new string[] { "medium", "60" }));
            }

            if (showNBackTaskButton != null)
            {
                showNBackTaskButton.onClick.AddListener(() => SendTaskCommand("NBACK_TASK", new string[] { "2", "60" }));
            }

            if (hideMathTaskButton != null)
            {
                hideMathTaskButton.onClick.AddListener(() => SendTaskCommand("HIDE_MATH_TASK", new string[] { }));
            }

            if (hideNBackTaskButton != null)
            {
                hideNBackTaskButton.onClick.AddListener(() => SendTaskCommand("HIDE_NBACK_TASK", new string[] { }));
            }
        }

        /// <summary>
        /// Setup study control buttons
        /// </summary>
        void SetupStudyControlButtons()
        {
            if (abortAllButton != null)
            {
                abortAllButton.onClick.AddListener(() => SendStudyControlCommand("ABORT_ALL"));

                var buttonText = abortAllButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = "Alle Abbrechen";
            }

            if (endStudyButton != null)
            {
                endStudyButton.onClick.AddListener(() => SendStudyControlCommand("END_STUDY"));

                var buttonText = endStudyButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = "Studie Beenden";
            }

            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(() => SendStudyControlCommand("refresh"));

                var buttonText = refreshButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = "Aktualisieren";
            }
        }

        /// <summary>
        /// Send NPC walk command to HMD
        /// </summary>
        void SendNPCWalkCommand(string npcName, string position)
        {
            var message = new EventMessage("NPC_WALK", new string[] { npcName, position });
            SendMessageToHMD(message);
            Debug.Log($"[TabletEventControl] Sent NPC_WALK: {npcName} to {position}");
        }

        /// <summary>
        /// Send NPC talk command to HMD
        /// </summary>
        void SendNPCTalkCommand(string npcName, string audioClip)
        {
            var message = new EventMessage("NPC_TALK", new string[] { npcName, audioClip });
            SendMessageToHMD(message);
            Debug.Log($"[TabletEventControl] Sent NPC_TALK: {npcName} says {audioClip}");
        }

        /// <summary>
        /// Send task command to HMD
        /// </summary>
        void SendTaskCommand(string taskType, string[] parameters)
        {
            var message = new EventMessage(taskType, parameters);
            SendMessageToHMD(message);
            Debug.Log($"[TabletEventControl] Sent {taskType} with parameters: {string.Join(", ", parameters)}");
        }

        /// <summary>
        /// Send camera change command to HMD
        /// </summary>
        public void SendCameraCommand(int cameraIndex)
        {
            var message = new EventMessage("changeCamera", new string[] { cameraIndex.ToString() });
            SendMessageToHMD(message);
            Debug.Log($"[TabletEventControl] Sent changeCamera to index: {cameraIndex}");
        }

        void SendCameraStreamRequest()
        {
            if (webSocketClient != null)
            {
                //Todo: Send a request to the HMD to start the camera stream
                Debug.Log("[TabletEventControl] Requested camera stream from HMD");
            }
            else
            {
                Debug.LogWarning("[TabletEventControl] WebSocketClient is null, cannot request camera stream!");
            }
        }

        /// <summary>
        /// Send study control command to HMD
        /// </summary>
        void SendStudyControlCommand(string controlType)
        {
            var message = new EventMessage(controlType, new string[] { });
            SendMessageToHMD(message);
            Debug.Log($"[TabletEventControl] Sent study control: {controlType}");
        }

        /// <summary>
        /// Send message to HMD via WebSocket
        /// </summary>
        void SendMessageToHMD(EventMessage message)
        {
            if (webSocketClient != null)
            {
                webSocketClient.SendEventMessage(message);
            }
            else
            {
                Debug.LogWarning("[TabletEventControl] WebSocketClient is null!");
            }
        }

        /// <summary>
        /// Convert technical position names to user-friendly German names
        /// </summary>
        string GetFriendlyPositionName(string position)
        {
            switch (position)
            {
                case "bedLeft1": return "Bett Links 1";
                case "bedLeft2": return "Bett Links 2";
                case "bedRight1": return "Bett Rechts 1";
                case "doorInside": return "Tür Innen";
                case "doorOutside": return "Tür Außen";
                case "outside": return "Draußen";
                default: return position;
            }
        }

        /// <summary>
        /// Handle incoming messages from HMD (if needed for tablet display updates)
        /// </summary>
        public void OnMessageReceived(string msg)
        {
            EventMessage message = JsonUtility.FromJson<EventMessage>(msg);

            if (enableDetailedLogging)
                Debug.Log($"[TabletEventControl] Received message: {message.type}");

            switch (message.type)
            {
                case "audioClipsListChefarzt":
                    // Update chefarzt audio clips if needed
                    break;
                case "audioClipsListKollege":
                    // Update kollege audio clips if needed
                    break;
                case "audioClipsListPatient":
                    // Update patient audio clips if needed
                    break;
                case "scenarioUpdate":
                    break;
                case "scenarioList":
                    Debug.Log("[TabletEventControl] Received scenario list update");
                    SetupScenarioDropdown(message);
                    break;
                case "SCENE_LOADED":
                    SendCameraStreamRequest();
                    RequestStudySetup();
                    Debug.Log("[TabletEventControl] Scene loaded, requesting camera stream");
                    break;
                case "STUDY_SETUP_RESPONSE":
                    Debug.Log("[TabletEventControl] Received study setup response");
                    HandleStudySetupResponse(message);
                    break;
                default:
                    // Handle other message types as needed
                    break;
            }
        }

        /// <summary>
        /// Update audio clip arrays from HMD data (if dynamic updates are needed)
        /// </summary>
        public void UpdateAudioClips(string npcName, string[] audioClips)
        {
            switch (npcName.ToLower())
            {
                case "chefarzt":
                    chefarztAudioClips = audioClips;
                    break;
                case "kollege":
                    kollegeAudioClips = audioClips;
                    break;
                case "patient":
                    patientAudioClips = audioClips;
                    break;
            }

            Debug.Log($"[TabletEventControl] Updated audio clips for {npcName}: {audioClips.Length} clips");
        }

        /// <summary>
        /// Handle study setup response from HMD - creates dynamic UI
        /// </summary>
        void HandleStudySetupResponse(EventMessage message)
        {
            if (message.content == null || message.content.Length == 0)
            {
                Debug.LogError("[TabletEventControl] Study setup response is empty!");
                return;
            }


            foreach (var item in message.content)
            {
                Debug.Log($"[TabletEventControl] Study setup content: {item}");
            }

            try
            {
                // Parse the JSON data from the response
                string jsonData = message.content[0];
                StudySetupData setupData = JsonUtility.FromJson<StudySetupData>(jsonData);

                if (setupData == null)
                {
                    Debug.LogError("[TabletEventControl] Failed to parse study setup data!");
                    return;
                }

                // Clear existing dynamic buttons
                ClearDynamicButtons();

                // Create dynamic UI for each NPC
                foreach (var npc in setupData.npcs)
                {
                    CreateNPCButtons(npc);
                }

                // Create position buttons
                CreatePositionButtons(setupData.positions);

                if (enableDetailedLogging)
                    Debug.Log($"[TabletEventControl] Created dynamic UI for {setupData.npcs.Length} NPCs and {setupData.positions.Length} positions");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TabletEventControl] Error handling study setup response: {e.Message}");
            }
        }

        /// <summary>
        /// Create dynamic buttons for an NPC
        /// </summary>
        void CreateNPCButtons(NPCData npcData)
        {
            Transform container = GetNPCContainer(npcData.name);
            if (container == null)
            {
                Debug.LogWarning($"[TabletEventControl] No container found for NPC: {npcData.name}");
                return;
            }

            // Create a label for the NPC
            CreateNPCLabel(container, npcData.displayName);

            // Create buttons for each audio file
            foreach (var audioFile in npcData.audioFiles)
            {
                CreateAudioButton(container, npcData.name, audioFile.clipName, audioFile.label);
            }

            if (enableDetailedLogging)
                Debug.Log($"[TabletEventControl] Created {npcData.audioFiles.Length} buttons for {npcData.displayName}");
        }

        /// <summary>
        /// Create a button for an audio file
        /// </summary>
        void CreateAudioButton(Transform container, string npcName, string clipName, string label)
        {
            if (buttonPrefab == null)
            {
                Debug.LogError("[TabletEventControl] Button prefab is not assigned!");
                return;
            }

            GameObject buttonObj = Instantiate(buttonPrefab, container);
            Button button = buttonObj.GetComponent<Button>();

            if (button == null)
            {
                Debug.LogError("[TabletEventControl] Button prefab doesn't have a Button component!");
                Destroy(buttonObj);
                return;
            }

            // Set button text
            var buttonText = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = label;
            }

            // Add click listener
            button.onClick.AddListener(() => SendNPCTalkCommand(npcName, clipName));

            // Track the button for cleanup
            dynamicAudioButtons.Add(buttonObj);

            if (enableDetailedLogging)
                Debug.Log($"[TabletEventControl] Created audio button: {npcName} - {label}");
        }

        /// <summary>
        /// Create an NPC label
        /// </summary>
        void CreateNPCLabel(Transform container, string displayName)
        {
            // Create a simple text object for the NPC name
            GameObject labelObj = new GameObject($"{displayName}_Label");
            labelObj.transform.SetParent(container, false);

            // Add TextMeshPro component
            var textComponent = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
            textComponent.text = displayName;
            textComponent.fontSize = 18;
            textComponent.fontStyle = TMPro.FontStyles.Bold;
            textComponent.alignment = TMPro.TextAlignmentOptions.Center;

            // Add to cleanup list
            dynamicAudioButtons.Add(labelObj);
        }

        /// <summary>
        /// Create buttons for available positions
        /// </summary>
        void CreatePositionButtons(string[] positions)
        {
            if (positionButtonContainer == null || positions == null)
                return;

            foreach (string position in positions)
            {
                CreatePositionButton(position);
            }
        }

        /// <summary>
        /// Create a button for a position
        /// </summary>
        void CreatePositionButton(string position)
        {
            if (buttonPrefab == null || positionButtonContainer == null)
                return;

            GameObject buttonObj = Instantiate(buttonPrefab, positionButtonContainer);
            Button button = buttonObj.GetComponent<Button>();

            if (button == null)
            {
                Destroy(buttonObj);
                return;
            }

            // Set button text
            var buttonText = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = GetFriendlyPositionName(position);
            }

            // Add click listener - for now just log, you can extend this later
            button.onClick.AddListener(() => Debug.Log($"Position selected: {position}"));

            // Track the button for cleanup
            dynamicPositionButtons.Add(buttonObj);
        }

        /// <summary>
        /// Get the container for a specific NPC
        /// </summary>
        Transform GetNPCContainer(string npcName)
        {
            switch (npcName.ToLower())
            {
                case "chefarzt": return chefarztButtonContainer;
                case "kollege": return kollegeButtonContainer;
                case "patient": return patientButtonContainer;
                default: return null;
            }
        }

        /// <summary>
        /// Clear all dynamically created buttons
        /// </summary>
        void ClearDynamicButtons()
        {
            // Clear audio buttons
            foreach (var button in dynamicAudioButtons)
            {
                if (button != null)
                    Destroy(button);
            }
            dynamicAudioButtons.Clear();

            // Clear position buttons
            foreach (var button in dynamicPositionButtons)
            {
                if (button != null)
                    Destroy(button);
            }
            dynamicPositionButtons.Clear();

            if (enableDetailedLogging)
                Debug.Log("[TabletEventControl] Cleared all dynamic buttons");
        }

        /// <summary>
        /// Request study setup from HMD
        /// </summary>
        public void RequestStudySetup()
        {
            if (webSocketClient != null)
            {
                var message = new EventMessage("REQUEST_STUDY_SETUP", new string[] { });
                webSocketClient.SendEventMessage(message);
                Debug.Log("[TabletEventControl] Requested study setup from HMD");
            }
            else
            {
                Debug.LogWarning("[TabletEventControl] WebSocketClient is null!");
            }
        }

        /// <summary>
        /// Enable/disable UI panels based on study phase
        /// </summary>
        public void SetUIState(bool npcActionsEnabled, bool taskActionsEnabled, bool cameraActionsEnabled)
        {
            if (npcActionsPanel != null)
                npcActionsPanel.SetActive(npcActionsEnabled);

            if (taskActionsPanel != null)
                taskActionsPanel.SetActive(taskActionsEnabled);

            if (cameraActionsPanel != null)
                cameraActionsPanel.SetActive(cameraActionsEnabled);

            Debug.Log($"[TabletEventControl] UI State - NPC: {npcActionsEnabled}, Tasks: {taskActionsEnabled}, Camera: {cameraActionsEnabled}");
        }
    }

    // Data structures for JSON parsing
    [System.Serializable]
    public class StudySetupData
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
}
