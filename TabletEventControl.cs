using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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
        [SerializeField] Text participantIDText;

        [Header("Pre-configured UI Sections")]
        [Tooltip("UI Panels - configure these in Unity Editor with pre-made buttons")]
        [SerializeField] GameObject npcActionsPanel;
        [SerializeField] GameObject taskActionsPanel;
        [SerializeField] GameObject cameraActionsPanel;
        [SerializeField] GameObject studyControlPanel;


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
        [SerializeField] Button questionnaireButton;

        [Header("Dynamic UI Generation")]
        [Tooltip("Button prefab for dynamically created audio buttons")]
        [SerializeField] GameObject buttonPrefab;
        [Tooltip("Character panel prefab with TMPro_Text, Image, and TalkButtons container")]
        [SerializeField] GameObject characterButtonPanelPrefab;
        [Tooltip("Parent container where character panels will be instantiated")]
        [SerializeField] Transform characterPanelsContainer;
        [Tooltip("Parent container for dynamically created position buttons")]
        [SerializeField] Transform positionButtonContainer;

        [SerializeField] TMP_Text[] characterNameLabels;
        // Text fields to show NPC info - dynamically assigned based on available characters

        [Header("Debug Settings")]
        [SerializeField] bool enableDetailedLogging = true;

        // Lists to keep track of dynamically created buttons for cleanup
        private List<GameObject> dynamicAudioButtons = new List<GameObject>();
        private List<GameObject> dynamicPositionButtons = new List<GameObject>();

        // Dynamic character mapping based on JSON order
        private Dictionary<string, int> characterToContainerIndex = new Dictionary<string, int>();
        private Dictionary<string, Transform> characterToTalkButtonsContainer = new Dictionary<string, Transform>();
        private List<string> availableCharacters = new List<string>();
        private List<GameObject> dynamicCharacterPanels = new List<GameObject>();

        void Start()
        {
            SetupStudyControlButtons();
        }

        /// <summary>
        /// Setup scenario dropdown and subscribe to scenario changes
        /// </summary>
        public void SetupScenarioDropdown(EventMessage message)
        {
            PopulateScenarioDropdown(message.content);



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

            StudyLogger.Instance.WriteLineToLog("Changing to Scenario: " + (scenarioDropdown.value + 1).ToString());

        }

        public void SetParticipantID()
        {
            PlayerPrefs.SetInt("CurrentParticipantID", int.Parse(participantIDText.text));
            PlayerPrefs.Save();
        }

        //    /// <summary> 
        //     /// Refresh all button configurations after scenario change
        //     /// </summary>
        //     void RefreshButtonConfigurations()
        //     {
        //         // Clear existing button listeners
        //         ClearButtonListeners();

        //         // Re-setup all buttons with new scenario data

        //         SetupTaskActionButtons();
        //         SetupStudyControlButtons();
        //     }

        /// <summary>
        /// Clear all existing button listeners
        /// </summary>
        void ClearButtonListeners()
        {
            // Clear other button listeners
            ClearButtonArrayListeners(cameraButtons);

            if (showMathTaskButton != null) showMathTaskButton.onClick.RemoveAllListeners();
            if (showNBackTaskButton != null) showNBackTaskButton.onClick.RemoveAllListeners();
            if (hideMathTaskButton != null) hideMathTaskButton.onClick.RemoveAllListeners();
            if (hideNBackTaskButton != null) hideNBackTaskButton.onClick.RemoveAllListeners();
            if (abortAllButton != null) abortAllButton.onClick.RemoveAllListeners();
            if (endStudyButton != null) endStudyButton.onClick.RemoveAllListeners();
            if (questionnaireButton != null) questionnaireButton.onClick.RemoveAllListeners();
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
                endStudyButton.onClick.AddListener(() => SendStudyControlCommand("END_SCENE"));

                var buttonText = endStudyButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (buttonText != null) buttonText.text = "Studie Beenden";
            }


            //TODO USe this to open the questionnaore scene on the headset
            if (questionnaireButton != null)
            {
                questionnaireButton.onClick.AddListener(() => SendStudyControlCommand("OPEN_QUESTIONNAIRE"));

                var buttonText = questionnaireButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                buttonText.text = "Fragebogen öffnen";
            }
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

            //save currently open scenario in playerprefs
            PlayerPrefs.SetString("CurrentScenario", scenarioDropdown.options[scenarioDropdown.value].text);
            PlayerPrefs.Save();

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

                // Clear existing dynamic buttons and mappings
                ClearDynamicButtons();
                ClearCharacterMappings();

                // Build character mapping based on JSON order
                BuildCharacterMappings(setupData.npcs);

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

            // Create buttons for each audio file (character name is already set in the CharacterButtonPanel)
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
            button.onClick.AddListener(() => StudyLogger.Instance.WriteLineToLog($"NPC_TALK: {npcName} - {clipName}"));

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
        /// Get the talk buttons container for a specific NPC
        /// </summary>
        Transform GetNPCContainer(string npcName)
        {
            if (characterToTalkButtonsContainer.TryGetValue(npcName, out Transform container))
            {
                return container;
            }

            if (enableDetailedLogging)
                Debug.LogWarning($"[TabletEventControl] No talk buttons container found for NPC: {npcName}");

            return null;
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
                    DestroyImmediate(button);
            }
            dynamicAudioButtons.Clear();

            // Clear position buttons
            foreach (var button in dynamicPositionButtons)
            {
                if (button != null)
                    DestroyImmediate(button);
            }
            dynamicPositionButtons.Clear();

            if (enableDetailedLogging)
                Debug.Log("[TabletEventControl] Cleared all dynamic buttons");
        }

        /// <summary>
        /// Clear character mappings
        /// </summary>
        void ClearCharacterMappings()
        {
            characterToContainerIndex.Clear();
            availableCharacters.Clear();

            // Clear character panels
            ClearCharacterPanels();

            // Clear character name labels
            if (characterNameLabels != null)
            {
                foreach (var label in characterNameLabels)
                {
                    if (label != null)
                        label.text = "";
                }
            }

            if (enableDetailedLogging)
                Debug.Log("[TabletEventControl] Cleared character mappings");
        }

        /// <summary>
        /// Build character mappings based on JSON order - creates CharacterButtonPanel prefabs
        /// </summary>
        void BuildCharacterMappings(NPCData[] npcs)
        {
            if (npcs == null || characterButtonPanelPrefab == null || characterPanelsContainer == null)
            {
                Debug.LogError("[TabletEventControl] Missing required prefab or container for character panels!");
                return;
            }

            // Clear existing character panels
            ClearCharacterPanels();

            for (int i = 0; i < npcs.Length; i++)
            {
                string characterName = npcs[i].name;
                string displayName = npcs[i].displayName;

                // Instantiate CharacterButtonPanel prefab
                GameObject characterPanel = Instantiate(characterButtonPanelPrefab, characterPanelsContainer);
                dynamicCharacterPanels.Add(characterPanel);

                // Find the TMPro_Text component and set character name
                TMP_Text nameText = characterPanel.GetComponentInChildren<TMP_Text>();
                if (nameText != null)
                {
                    nameText.text = displayName;
                }
                else
                {
                    Debug.LogWarning($"[TabletEventControl] No TMP_Text found in CharacterButtonPanel for {characterName}");
                }

                // Find the TalkButtons container (should be a child with name "TalkButtons")
                Transform talkButtonsContainer = characterPanel.transform.Find("Buttonpanel/TalkButtons");
                if (talkButtonsContainer == null)
                {
                    // Try alternative path structures
                    talkButtonsContainer = characterPanel.transform.Find("TalkButtons");
                    if (talkButtonsContainer == null)
                    {
                        // Search recursively for any Transform with "TalkButtons" in the name
                        talkButtonsContainer = FindChildRecursive(characterPanel.transform, "TalkButtons");
                    }
                }

                if (talkButtonsContainer != null)
                {
                    // Store mapping
                    availableCharacters.Add(characterName);
                    characterToContainerIndex[characterName] = i;
                    characterToTalkButtonsContainer[characterName] = talkButtonsContainer;

                    if (enableDetailedLogging)
                        Debug.Log($"[TabletEventControl] Created character panel for '{characterName}' ({displayName}) with TalkButtons container");
                }
                else
                {
                    Debug.LogError($"[TabletEventControl] Could not find TalkButtons container in CharacterButtonPanel for {characterName}");
                }
            }

            if (enableDetailedLogging)
                Debug.Log($"[TabletEventControl] Built character mappings for {availableCharacters.Count} characters using CharacterButtonPanel prefabs");
        }

        /// <summary>
        /// Recursively find a child transform by name
        /// </summary>
        Transform FindChildRecursive(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Contains(childName))
                    return child;

                Transform found = FindChildRecursive(child, childName);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// Clear all dynamically created character panels
        /// </summary>
        void ClearCharacterPanels()
        {
            foreach (var panel in dynamicCharacterPanels)
            {
                if (panel != null)
                    Destroy(panel);
            }
            dynamicCharacterPanels.Clear();
            characterToTalkButtonsContainer.Clear();

            if (enableDetailedLogging)
                Debug.Log("[TabletEventControl] Cleared all character panels");
        }

        public void SetNextInfoText()
        {
            if (webSocketClient != null)
            {
                var message = new EventMessage("SET_INFO_TEXT", new string[] { });
                webSocketClient.SendEventMessage(message);
                Debug.Log("[TabletEventControl] Requested study setup from HMD");
            }
            else
            {
                Debug.LogWarning("[TabletEventControl] WebSocketClient is null!");
            }

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
