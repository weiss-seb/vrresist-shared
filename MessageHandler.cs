using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Events;



namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Streamlined message handler for VR study system
    /// Processes messages from remote tablet and executes corresponding actions in HMD
    /// </summary>

    #region EditoFields
    public class MessageHandler : MonoBehaviour
    {

        [Header("System References")]
        public EventTriggerSystem eventTriggerSystem;
        [SerializeField] ScenarioLoader scenarioLoader;

        [Header("UI References")]
        public TMP_Text debugText;
        public ScenarioSceneManager scenarioSceneManager;

        //Event that send a string message to the TCP server to be sent to the client
        public UnityEvent<string> sendMessageEvent;

        TCPServer _tcpServer;

        [Header("XR Prefab")]
        [SerializeField] GameObject xrPrefab;

        [Header("Debug Settings")]
        [SerializeField] bool enableDetailedLogging = true;


        #endregion
        #region LifeCycle
        void Start()
        {

            DiscoverScenarioComponents();

        }
        // Reinitialize references after scene load
        void DiscoverScenarioComponents()
        {
            if (scenarioLoader == null)
            {
                scenarioLoader = FindObjectOfType<ScenarioLoader>();
                if (scenarioLoader == null)
                {
                    Debug.LogError("[MessageHandler] ScenarioLoader not found in scene!");
                }
            }

            eventTriggerSystem = FindObjectOfType<EventTriggerSystem>();
            if (eventTriggerSystem == null)
            {
                Debug.LogError("[MessageHandler] EventTriggerSystem not found in scene!");
            }


            scenarioSceneManager = FindObjectOfType<ScenarioSceneManager>();
            // Find reference to ScenarioSceneManager loaded in the waiting room scene
            if (scenarioSceneManager == null)
            {
                scenarioSceneManager = FindObjectOfType<ScenarioSceneManager>();
                if (scenarioSceneManager == null)
                {
                    Debug.LogError("[MessageHandler] ScenarioSceneManager not found in scene!");
                }
            }

            // _tcpServer = GameObject.Find("TCP_Server").GetComponent<TCPServer>(); //comes from the scene that was loaded before
            // _tcpServer.messageEvent.AddListener(OnReceive);

            xrPrefab = GameObject.FindWithTag("XRUser");

            // Notify that setup is complete
            Debug.Log("[MessageHandler] Message handler setup complete");
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[MessageHandler] Scene loaded: {scene.name} (mode: {mode})");

            DiscoverScenarioComponents();
        }

        #endregion

        /// <summary>
        /// Main message processing method - handles all incoming messages from remote tablet
        /// </summary>
        public void OnReceive(string msgJson)
        {
            Debug.Log($"[MessageHandler] Received JSON: {msgJson}");

            try
            {
                EventMessage msg = JsonUtility.FromJson<EventMessage>(msgJson);
                ProcessMessage(msg);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MessageHandler] Failed to parse message: {e.Message}");
            }
        }

        /// <summary>
        /// Process the parsed message based on its type
        /// </summary>
        private void ProcessMessage(EventMessage msg)
        {
            Debug.Log($"[MessageHandler] Processing message of type: {msg.type}");
            // Handle new streamlined message types first
            if (IsCoreActionType(msg.type))
            {
                ProcessCoreAction(msg);
            }
            // Handle legacy message types for backward compatibility
            else if (IsLegacyType(msg.type))
            {
                ProcessLegacyMessage(msg);
            }
            // Handle system messages
            else
            {
                ProcessSystemMessage(msg);
            }
        }

        /// <summary>
        /// Check if a message type is a core action type (new system)
        /// </summary>
        private bool IsCoreActionType(string messageType)
        {
            return messageType == "NPC_WALK" ||
                   messageType == "NPC_TALK" ||
                   messageType == "MATH_TASK" ||
                   messageType == "NBACK_TASK" ||
                   messageType == "CAMERA_CHANGE" ||
                   messageType == "ABORT_ALL" ||
                   messageType == "END_STUDY" ||
                   messageType == "REQUEST_STUDY_SETUP" ||
                   messageType == "SCENARIO_CHANGE" ||
                   messageType == "SET_INFO_TEXT" ||
                   messageType == "PHONE_CALL_INCOMING" ||
                   messageType == "OPEN_QUESTIONNAIRE";
        }

        /// <summary>
        /// Check if a message type is a legacy type (old system)
        /// </summary>
        private bool IsLegacyType(string messageType)
        {
            return messageType == "speak" ||
                   messageType == "event" ||
                   messageType == "changeCamera" ||
                   messageType == "abort";
        }

        /// <summary>
        /// Process core action messages (new streamlined system)
        /// </summary>
        private void ProcessCoreAction(EventMessage msg)
        {
            Debug.Log($"[MessageHandler] Processing core action: {msg.type}");

            switch (msg.type)
            {
                case "NPC_WALK":
                    HandleNPCWalk(msg);
                    break;

                case "NPC_TALK":
                    HandleNPCTalk(msg);
                    break;

                case "MATH_TASK":
                    HandleMathTask(msg);
                    break;

                case "NBACK_TASK":
                    HandleNBackTask(msg);
                    break;

                case "CAMERA_CHANGE":
                    HandleCameraChange(msg);
                    break;

                case "ABORT_ALL":
                    HandleAbortAll(msg);
                    break;

                case "END_STUDY":
                    HandleEndStudy(msg);
                    break;

                case "SCENARIO_CHANGE":
                    HandleScenarioChange(msg);
                    break;

                case "REQUEST_STUDY_SETUP":
                    HandleStudySetupRequest(msg);
                    break;
                case "SET_INFO_TEXT":
                    HandleSetInfoText(msg);
                    break;

                case "PHONE_CALL_INCOMING":
                    HandlePhoneCallIncoming(msg);
                    break;

                case "OPEN_QUESTIONNAIRE":
                    HandleOpenQuestionnaire(msg);
                    break;

                default:
                    Debug.LogWarning($"[MessageHandler] Unknown core action type: {msg.type}");
                    break;
            }
        }

        private void HandleOpenQuestionnaire(EventMessage msg)
        {
            Debug.Log("[MessageHandler] Opening Questionnaire Scene");

            scenarioSceneManager.LoadQuestionnaireScene();
        }

        private void HandleSetInfoText(EventMessage msg)
        {
            eventTriggerSystem.SetInfoText();
        }

        private void HandlePhoneCallIncoming(EventMessage msg)
        {
            Debug.Log("[MessageHandler] Handling PHONE_CALL_INCOMING command");

            eventTriggerSystem.TriggerPhoneCallIncoming();
        }

        /// <summary>
        /// Process legacy messages for backward compatibility
        /// </summary>
        private void ProcessLegacyMessage(EventMessage msg)
        {
            Debug.Log($"[MessageHandler] Processing legacy message: {msg.type}");

            switch (msg.type)
            {
                case "speak":
                    // Convert to new NPC_TALK format
                    if (msg.content.Length >= 2)
                    {
                        var newMsg = new EventMessage("NPC_TALK", msg.content);
                        HandleNPCTalk(newMsg);
                    }
                    break;

                case "changeCamera":
                    // Convert to new CAMERA_CHANGE format
                    var cameraMsg = new EventMessage("CAMERA_CHANGE", msg.content);
                    HandleCameraChange(cameraMsg);
                    break;

                case "abort":
                    // Convert to new ABORT_ALL format
                    var abortMsg = new EventMessage("ABORT_ALL", new string[] { });
                    HandleAbortAll(abortMsg);
                    break;

                case "event":
                    // Keep legacy event handling for complex scenarios
                    eventTriggerSystem.QueueScenarioEvent(msg);
                    break;

                default:
                    Debug.LogWarning($"[MessageHandler] Unknown legacy message type: {msg.type}");
                    break;
            }
        }

        /// <summary>
        /// Process system messages (requests, tasks, etc.)
        /// </summary>
        private void ProcessSystemMessage(EventMessage msg)
        {
            Debug.Log($"[MessageHandler] Processing system message: {msg.type}");

            switch (msg.type)
            {
                case "request":
                    HandleRequest(msg);
                    break;
                case "chat":
                    HandleChat(msg);
                    break;

                default:
                    // Fallback to legacy event system for unknown messages
                    Debug.LogWarning($"[MessageHandler] Unknown system message, forwarding to event system: {msg.type}");
                    eventTriggerSystem.QueueScenarioEvent(msg);
                    break;
            }
        }

        #region Core Action Handlers

        /// <summary>
        /// Handle NPC walk command: content[0]=npcName, content[1]=positionName
        /// Uses new simplified EventTriggerSystem.MoveNPCTo() method
        /// </summary>
        private void HandleNPCWalk(EventMessage msg)
        {
            if (msg.content.Length < 2)
            {
                Debug.LogError("[MessageHandler] NPC_WALK requires 2 parameters: npcName, positionName");
                return;
            }

            string npcName = msg.content[0];
            string positionName = msg.content[1];

            Debug.Log($"[MessageHandler] Moving {npcName} to {positionName}");

            // Use new simplified direct method call
            if (eventTriggerSystem != null)
            {
                eventTriggerSystem.MoveNPCTo(npcName, positionName);
            }
            else
            {
                Debug.LogError("[MessageHandler] EventTriggerSystem not assigned!");
            }
        }

        /// <summary>
        /// Handle NPC talk command: content[0]=npcName, content[1]=audioClipName
        /// Uses new simplified EventTriggerSystem.PlayNPCAudio() method
        /// </summary>
        private void HandleNPCTalk(EventMessage msg)
        {
            Debug.Log("[MessageHandler] Handling NPC_TALK command");
            if (msg.content.Length < 2)
            {
                Debug.LogError("[MessageHandler] NPC_TALK requires 2 parameters: npcName, audioClipName");
                return;
            }

            string npcName = msg.content[0];
            string audioClipName = msg.content[1];

            Debug.Log($"[MessageHandler] {npcName} speaking: {audioClipName}");

            // Use new simplified direct method call
            if (eventTriggerSystem != null)
            {
                eventTriggerSystem.PlayNPCAudio(npcName, audioClipName);
            }
            else
            {
                Debug.LogError("[MessageHandler] EventTriggerSystem not assigned!");
            }
        }

        /// <summary>
        /// Handle math task command: content[0]=difficulty, content[1]=timeLimit
        /// Uses new simplified EventTriggerSystem.ShowMathTask() method
        /// </summary>
        private void HandleMathTask(EventMessage msg)
        {
            Debug.Log("[MessageHandler] Showing math task");

            if (eventTriggerSystem != null)
            {
                if (Enum.TryParse(msg.content[0], out ENUM_TaskDifficulty parsedNValue) == true)
                {
                    eventTriggerSystem.ShowMathTask(parsedNValue);
                }
            }
            else
            {
                Debug.LogError("[MessageHandler] EventTriggerSystem not assigned!");
            }
        }

        /// <summary>
        /// Handle n-back task command: content[0]=nValue, content[1]=timeLimit
        /// Uses new simplified EventTriggerSystem.ShowNBackTask() method
        /// </summary>
        private void HandleNBackTask(EventMessage msg)
        {
            Debug.Log("[MessageHandler] Showing n-back task");
            int nValue = 0;
            int timeLimit = 60; // default to 60 seconds

            if (Enum.TryParse(msg.content[0], out ENUM_TaskDifficulty parsedNValue) == true)
            {
                nValue = GetNBackFromDifficulty(parsedNValue);
            }

            if (Enum.TryParse(msg.content[1], out ENUM_TaskDifficulty parsedTime) == true)
            {
                timeLimit = GetTimeFromDifficulty(parsedTime);
            }


            // transfrom diff to time 
            // Use new simplified direct method call
            if (eventTriggerSystem != null)
            {
                eventTriggerSystem.ShowNBackTask(nValue, timeLimit);
            }
            else
            {
                Debug.LogError("[MessageHandler] EventTriggerSystem not assigned!");
            }
        }

        /// <summary>
        /// Convert difficulty enum to time in seconds
        /// </summary>
        /// <param name="difficulty"></param>
        /// <returns></returns>
        private int GetTimeFromDifficulty(ENUM_TaskDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ENUM_TaskDifficulty.EASY:
                    return 60; // 1 minute
                case ENUM_TaskDifficulty.MEDIUM:
                    return 40; // 3 minutes
                case ENUM_TaskDifficulty.HARD:
                    return 20; // 5 minutes
                default:
                    Debug.LogWarning($"[MessageHandler] Unknown difficulty level: {difficulty}, defaulting to medium (3 minutes)");
                    return 3; // Default to medium if unknown
            }
        }

        private int GetNBackFromDifficulty(ENUM_TaskDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ENUM_TaskDifficulty.EASY:
                    return 1; // 1-back
                case ENUM_TaskDifficulty.MEDIUM:
                    return 2; // 2-back
                case ENUM_TaskDifficulty.HARD:
                    return 3; // 3-back
                default:
                    Debug.LogWarning($"[MessageHandler] Unknown difficulty level: {difficulty}, defaulting to medium (2-back)");
                    return 2; // Default to medium if unknown
            }
        }

        /// <summary>
        /// Handle camera change command: content[0]=cameraIndex
        /// Uses new simplified EventTriggerSystem.ChangeCameraView() method
        /// </summary>
        private void HandleCameraChange(EventMessage msg)
        {
            if (msg.content.Length < 1)
            {
                Debug.LogError("[MessageHandler] CAMERA_CHANGE requires 1 parameter: cameraIndex");
                return;
            }

            if (int.TryParse(msg.content[0], out int cameraIndex))
            {
                Debug.Log($"[MessageHandler] Changing camera to index: {cameraIndex}");

                // Use new simplified direct method call
                if (eventTriggerSystem != null)
                {
                    eventTriggerSystem.ChangeCameraView(cameraIndex);
                }
                else
                {
                    Debug.LogError("[MessageHandler] EventTriggerSystem not assigned!");
                }
            }
            else
            {
                Debug.LogError($"[MessageHandler] Invalid camera index: {msg.content[0]}");
            }
        }

        /// <summary>
        /// Handle abort all command - stops all current activities
        /// Uses new simplified EventTriggerSystem.AbortAll() method
        /// </summary>
        private void HandleAbortAll(EventMessage msg)
        {
            Debug.Log("[MessageHandler] Aborting all activities");

            // Use new simplified direct method call
            if (eventTriggerSystem != null)
            {
                eventTriggerSystem.AbortAll();
            }
            else
            {
                Debug.LogError("[MessageHandler] EventTriggerSystem not assigned!");
            }
        }

        /// <summary>
        /// Handle end study command
        /// Uses new simplified EventTriggerSystem.EndStudy() method
        /// </summary>
        private void HandleEndStudy(EventMessage msg)
        {
            Debug.Log("[MessageHandler] Ending study");

            // Use new simplified direct method call
            if (eventTriggerSystem != null)
            {
                eventTriggerSystem.EndStudy();
            }
            else
            {
                Debug.LogError("[MessageHandler] EventTriggerSystem not assigned!");
            }
        }

        /// <summary>
        /// Handle scenario change command: content[0]=scenarioName, content[1]=scenarioDataJson
        /// Uses ScenarioSceneManager to load the appropriate scene for the scenario
        /// </summary>
        private void HandleScenarioChange(EventMessage msg)
        {

            int scenarioID = int.Parse(msg.content[0]);

            // if difficulty exists in content[1], set it in PlayerPrefs
            if (msg.content.Length > 1 && !string.IsNullOrEmpty(msg.content[1]))
            {
                PlayerPrefs.SetString("LastDifficultyUsed", msg.content[1]);
                Debug.Log($"[MessageHandler] Set LastDifficultyUsed to {msg.content[1]}");
            }

            Debug.Log($"[MessageHandler] Changing to scenario: {scenarioID}");

            try
            {
                string helpText = scenarioSceneManager.GetScenarioHelpText(scenarioID);


                if (scenarioSceneManager != null)
                {
                    // Use ScenarioSceneManager to load the appropriate scene
                    scenarioSceneManager.LoadScenarioScene(scenarioID);
                    Debug.Log($"[MessageHandler] Requested scene load for scenario ID: {scenarioID})");
                }
                else if (scenarioSceneManager == null)
                {
                    Debug.LogError("[MessageHandler] ScenarioSceneManager not assigned!");
                }
                else
                {
                    Debug.LogError("[MessageHandler] Failed to parse scenario data JSON");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MessageHandler] Error processing scenario change: {e.Message}");
            }
        }

        /// <summary>
        /// Handle study setup request - sends current scenario data to tablet
        /// </summary>
        private void HandleStudySetupRequest(EventMessage msg)
        {
            Debug.Log("[MessageHandler] Handling study setup request");

            if (scenarioLoader == null)
            {
                Debug.LogError("[MessageHandler] ScenarioLoader not assigned!");
                return;
            }

            try
            {
                // Build the study setup response data
                string studySetupData = scenarioLoader.BuildStudySetupData();
                Debug.Log("[MessageHandler] Study setup json = " + studySetupData);
                // Create response message
                EventMessage response = new EventMessage("STUDY_SETUP_RESPONSE", new string[] { studySetupData });

                // Send response to tablet
                SendEventMessageToClient(response);

                Debug.Log("[MessageHandler] Study setup response sent successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MessageHandler] Error building study setup response: {e.Message}");
            }
        }


        #endregion

        /// <summary>
        /// Handle refresh requests from remote tablet
        /// </summary>
        private void HandleRequest(EventMessage msg)
        {

            //TODO This is fired 4 times when the app starts, why?
            if (msg.content.Length > 0 && msg.content[0] == "refresh")
            {
                Debug.Log("[MessageHandler] Handling refresh request");

                //Send scene information by getting scene info from scenemanager and packing it into an EventMessage
                if (scenarioSceneManager != null)
                {
                    var sceneInfo = scenarioSceneManager.GetSceneListInfo();
                    SendEventMessageToClient(new EventMessage("scenarioList", sceneInfo));
                }

            }
        }

        /// <summary>
        /// Handle chat messages
        /// </summary>
        private void HandleChat(EventMessage msg)
        {
            if (msg.content.Length > 0 && debugText != null)
            {
                debugText.text = $"Chat: {msg.content[0]}";
            }
        }

        /// <summary>
        ///  // Convert the message to JSON and send it via TCP
        /// </summary>
        /// <param name="msg"></param>
        public void SendEventMessageToClient(EventMessage msg)
        {
            string s = JsonUtility.ToJson(msg);
            Debug.Log("Sending JSON: " + s);
            sendMessageEvent.Invoke(s);

        }

        private string[] GetAudioClipsForCharacter(GameObject NPC)
        {
            string[] list = new string[NPC.GetComponent<NPCController>().audioClips.Count];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = NPC.GetComponent<NPCController>().audioClips[i].name;
            }
            return list;
        }
    }
}
