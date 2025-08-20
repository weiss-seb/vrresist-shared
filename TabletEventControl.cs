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
        [SerializeField] ScenarioManager scenarioManager;
        [Tooltip("Only needed if this tablet also displays tasks locally")]
        [SerializeField] StudyTaskManager studyTaskManager;

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

        [Header("Study Configuration")]
        [Tooltip("Available NPC walk positions - must match button order")]
        [SerializeField]
        string[] availablePositions = {
            "bedLeft1", "bedLeft2", "bedRight1", "doorInside", "doorOutside", "outside"
        };

        [Header("Dynamic Audio Configuration")]
        [Tooltip("Audio clips and labels are loaded dynamically from ScenarioManager")]
        [SerializeField] string[] chefarztAudioClips = new string[0];
        [SerializeField] string[] kollegeAudioClips = new string[0];
        [SerializeField] string[] patientAudioClips = new string[0];
        [SerializeField] string[] chefarztAudioLabels = new string[0];
        [SerializeField] string[] kollegeAudioLabels = new string[0];
        [SerializeField] string[] patientAudioLabels = new string[0];

        [Header("Debug Settings")]
        [SerializeField] bool enableDetailedLogging = true;

        // Current scenario data
        private ScenarioData currentScenario;

        void Start()
        {
            // Setup scenario dropdown
            SetupScenarioDropdown();

            // Setup pre-configured UI button listeners
            SetupPreConfiguredUI();

            // Request initial data from HMD
            RequestInitialData();
        }

        /// <summary>
        /// Setup scenario dropdown and subscribe to scenario changes
        /// </summary>
        void SetupScenarioDropdown()
        {
            if (scenarioManager == null)
            {
                Debug.LogWarning("[TabletEventControl] ScenarioManager not assigned!");
                return;
            }

            if (scenarioDropdown == null)
            {
                Debug.LogWarning("[TabletEventControl] Scenario dropdown not assigned!");
                return;
            }

            // Subscribe to scenario changes
            scenarioManager.OnScenarioChanged += OnScenarioChanged;

            // Populate dropdown with scenario names
            PopulateScenarioDropdown();

            // Setup dropdown change listener
            scenarioDropdown.onValueChanged.AddListener(OnScenarioDropdownChanged);

            // Load initial scenario
            if (scenarioManager.GetScenarioCount() > 0)
            {
                OnScenarioChanged(scenarioManager.GetCurrentScenario());
            }

            if (enableDetailedLogging)
                Debug.Log("[TabletEventControl] Scenario dropdown setup complete");
        }

        /// <summary>
        /// Populate scenario dropdown with available scenarios
        /// </summary>
        void PopulateScenarioDropdown()
        {
            if (scenarioDropdown == null || scenarioManager == null) return;

            scenarioDropdown.ClearOptions();

            var scenarioNames = scenarioManager.GetScenarioNames();
            var options = new List<TMP_Dropdown.OptionData>();

            foreach (var name in scenarioNames)
            {
                options.Add(new TMP_Dropdown.OptionData(name));
            }

            scenarioDropdown.AddOptions(options);

            if (enableDetailedLogging)
                Debug.Log($"[TabletEventControl] Populated dropdown with {options.Count} scenarios");
        }

        /// <summary>
        /// Handle scenario dropdown value change
        /// </summary>
        void OnScenarioDropdownChanged(int scenarioIndex)
        {
            if (scenarioManager != null)
            {
                scenarioManager.SwitchToScenario(scenarioIndex);
            }
        }

        /// <summary>
        /// Handle scenario change event from ScenarioManager
        /// </summary>
        void OnScenarioChanged(ScenarioData newScenario)
        {
            if (newScenario == null)
            {
                Debug.LogWarning("[TabletEventControl] Received null scenario data!");
                return;
            }

            currentScenario = newScenario;

            if (enableDetailedLogging)
                Debug.Log($"[TabletEventControl] Scenario changed to: {newScenario.scenarioName}");

            // Update UI with new scenario data
            UpdateUIForScenario(newScenario);
        }

        /// <summary>
        /// Update UI elements based on current scenario
        /// </summary>
        void UpdateUIForScenario(ScenarioData scenario)
        {
            // Update available positions
            availablePositions = scenario.availableWalkPositions ?? availablePositions;

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
            SetupCameraActionButtons();
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
        void RequestInitialData()
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
            SetupCameraActionButtons();
            SetupStudyControlButtons();

            Debug.Log("[TabletEventControl] Pre-configured UI setup complete!");
        }

        /// <summary>
        /// Setup NPC action buttons - assigns listeners to pre-configured buttons
        /// </summary>
        void SetupNPCActionButtons()
        {
            // Setup walk buttons for each NPC
            SetupWalkButtons(chefarztWalkButtons, "chefarzt");
            SetupWalkButtons(kollegeWalkButtons, "kollege");
            SetupWalkButtons(patientWalkButtons, "patient");

            // Setup talk buttons for each NPC
            SetupTalkButtons(chefarztTalkButtons, "chefarzt", chefarztAudioClips, chefarztAudioLabels);
            SetupTalkButtons(kollegeTalkButtons, "kollege", kollegeAudioClips, kollegeAudioLabels);
            SetupTalkButtons(patientTalkButtons, "patient", patientAudioClips, patientAudioLabels);
        }

        /// <summary>
        /// Setup walk buttons for a specific NPC
        /// </summary>
        void SetupWalkButtons(Button[] buttons, string npcName)
        {
            if (buttons == null) return;

            for (int i = 0; i < buttons.Length && i < availablePositions.Length; i++)
            {
                if (buttons[i] != null)
                {
                    string position = availablePositions[i];
                    buttons[i].onClick.AddListener(() => SendNPCWalkCommand(npcName, position));

                    // Update button text to show position
                    var buttonText = buttons[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = GetFriendlyPositionName(position);
                    }

                    if (enableDetailedLogging)
                        Debug.Log($"[TabletEventControl] Setup walk button: {npcName} → {position}");
                }
            }
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
        /// Setup camera control buttons
        /// </summary>
        void SetupCameraActionButtons()
        {
            if (cameraButtons == null) return;

            for (int i = 0; i < cameraButtons.Length; i++)
            {
                if (cameraButtons[i] != null)
                {
                    int cameraIndex = i; // Capture for closure
                    cameraButtons[i].onClick.AddListener(() => SendCameraCommand(cameraIndex));

                    // Update button text
                    var buttonText = cameraButtons[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = $"Kamera {cameraIndex + 1}";
                    }

                    if (enableDetailedLogging)
                        Debug.Log($"[TabletEventControl] Setup camera button: Camera {cameraIndex + 1}");
                }
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
        void SendCameraCommand(int cameraIndex)
        {
            var message = new EventMessage("changeCamera", new string[] { cameraIndex.ToString() });
            SendMessageToHMD(message);
            Debug.Log($"[TabletEventControl] Sent changeCamera to index: {cameraIndex}");
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
        public void OnMessageReceived(EventMessage message)
        {
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
                case "taskList":
                    // Update task display if this tablet also shows tasks
                    if (studyTaskManager != null)
                    {
                        // Handle task list updates
                    }
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
}
