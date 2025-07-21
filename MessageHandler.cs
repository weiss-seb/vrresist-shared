using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Threading.Tasks.Sources;


namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Streamlined message handler for VR study system
    /// Processes messages from remote tablet and executes corresponding actions in HMD
    /// </summary>

    #region EditoFields
    public class MessageHandler : MonoBehaviour
    {
        [Header("NPC References")]
        public GameObject chefarzt, kollege, patient;
        [Header("System References")]
        public EventTriggerSystem eventTriggerSystem;
        public StudyTaskManager taskManager;

        [SerializeField] MathTaskManager mathTaskManager;
        [SerializeField] NBackTask nBackTaskManager;

        [Header("UI References")]
        public TMP_Text debugText;
        [SerializeField] ScenarioSceneManager scenarioSceneManager;


        TCPServer _tcpServer;

        [Header("XR Prefab")]
        [SerializeField] GameObject xrPrefab;

        [Header("Debug Settings")]
        [SerializeField] bool enableDetailedLogging = true;

        #endregion
        #region LifeCycle
        void Start()
        {
            eventTriggerSystem = FindObjectOfType<EventTriggerSystem>();

            // Find reference to ScenarioSceneManager loaded in the waiting room scene
            if (scenarioSceneManager == null)
            {
                scenarioSceneManager = FindObjectOfType<ScenarioSceneManager>();
                if (scenarioSceneManager == null)
                {
                    Debug.LogError("[MessageHandler] ScenarioSceneManager not found in scene!");
                }
            }
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

            // Reinitialize references after scene load
            if (eventTriggerSystem == null)
            {
                eventTriggerSystem = FindObjectOfType<EventTriggerSystem>();
            }

            if (scenarioSceneManager == null)
            {
                scenarioSceneManager = FindObjectOfType<ScenarioSceneManager>();
            }

            _tcpServer = GameObject.Find("TCPServer").GetComponent<TCPServer>();

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

        public void SetTCPServer(TCPServer server)
        {
            _tcpServer = server;
            if (_tcpServer != null)
            {
                //_tcpServer.OnMessageReceived += OnReceive;
                Debug.Log("[MessageHandler] TCP Server set successfully");
            }
            else
            {
                Debug.LogError("[MessageHandler] Failed to set TCP Server - it is null");
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
                   messageType == "SCENARIO_CHANGE";
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

                default:
                    Debug.LogWarning($"[MessageHandler] Unknown core action type: {msg.type}");
                    break;
            }
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


                //TODO: put nback and math task handling here
                case "task":
                    if (taskManager != null)
                    {
                        taskManager.receiveTaskMessage(msg);
                    }
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

            string npcName = StandardizeNPCName(msg.content[0]);
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

            string npcName = StandardizeNPCName(msg.content[0]);
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

            string difficulty = msg.content.Length > 0 ? msg.content[0] : "medium";
            string timeLimit = msg.content.Length > 1 ? msg.content[1] : "60";

            // Use new simplified direct method call
            if (eventTriggerSystem != null)
            {
                eventTriggerSystem.ShowMathTask(difficulty, timeLimit);
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

            string nValue = msg.content.Length > 0 ? msg.content[0] : "2";
            string timeLimit = msg.content.Length > 1 ? msg.content[1] : "60";

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
            if (msg.content.Length < 2)
            {
                Debug.LogError("[MessageHandler] SCENARIO_CHANGE requires 2 parameters: scenarioName, scenarioDataJson");
                return;
            }

            string scenarioName = msg.content[0];
            string scenarioDataJson = msg.content[1];

            Debug.Log($"[MessageHandler] Changing to scenario: {scenarioName}");

            try
            {
                // Parse scenario data from JSON
                var scenarioData = JsonUtility.FromJson<ScenarioData>(scenarioDataJson);

                //TODO scenariodata should be completely unpacked and injected into the next scene so that we can set up message handler there with the new characters and positions


                if (scenarioData != null && scenarioSceneManager != null)
                {
                    // Use ScenarioSceneManager to load the appropriate scene
                    scenarioSceneManager.LoadScenarioScene(scenarioData.scenarioId, scenarioData.scenarioInfoText);
                    Debug.Log($"[MessageHandler] Requested scene load for scenario: {scenarioName} (ID: {scenarioData.scenarioId})");
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
        /// Move NPC to starting position for scenario
        /// </summary>
        private void MoveNPCToStartingPosition(string npcName, Vector3 startPosition)
        {
            GameObject npc = GetNPCObject(npcName);
            if (npc != null && startPosition != Vector3.zero)
            {
                npc.transform.position = startPosition;
                Debug.Log($"[MessageHandler] Moved {npcName} to starting position: {startPosition}");
            }
        }

        #endregion

        #region System Message Handlers

        /// <summary>
        /// Handle refresh requests from remote tablet
        /// </summary>
        private void HandleRequest(EventMessage msg)
        {
            if (msg.content.Length > 0 && msg.content[0] == "refresh")
            {
                Debug.Log("[MessageHandler] Handling refresh request");

                //Send scene information by getting scene info from scenemanager and packing it into an EventMessage
                if (scenarioSceneManager != null)
                {
                    var sceneInfo = scenarioSceneManager.GetAllSceneInfo();
                    SendEventMessageToClient(new EventMessage("scenarioList", sceneInfo));
                }

                // Send available audio clips for each NPC
                SendEventMessageToClient(new EventMessage("audioClipsListChefarzt", GetAudioClipsForCharacter(chefarzt)));
                SendEventMessageToClient(new EventMessage("audioClipsListKollege", GetAudioClipsForCharacter(kollege)));
                SendEventMessageToClient(new EventMessage("audioClipsListPatient", GetAudioClipsForCharacter(patient)));

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

        #endregion

        #region Helper Methods

        /// <summary>
        /// Standardize NPC names to ensure consistency
        /// </summary>
        private string StandardizeNPCName(string inputName)
        {
            if (string.IsNullOrEmpty(inputName)) return "";

            string name = inputName.ToLower().Trim();
            switch (name)
            {
                case "chefarzt":
                case "chef":
                case "arzt":
                case "dr":
                    return "chefarzt";

                case "kollege":
                case "colleague":
                case "mitarbeiter":
                    return "kollege";

                case "patient":
                case "kranker":
                case "person":
                    return "patient";

                case "doctor":
                    return "doctor";

                case "anesthesiologist":
                case "anaesthesist":
                case "narkosearzt":
                    return "anesthesiologist";

                default:
                    Debug.LogWarning($"[MessageHandler] Unknown NPC name: {inputName}, using as-is");
                    return inputName.ToLower();
            }
        }

        /// <summary>
        /// Get NPC GameObject by standardized name
        /// </summary>
        private GameObject GetNPCObject(string npcName)
        {
            switch (npcName)
            {
                case "chefarzt": return chefarzt;
                case "kollege": return kollege;
                case "patient": return patient;
                default: return null;
            }
        }

        /// <summary>
        /// Stop all NPC activities
        /// </summary>
        private void StopAllNPCs()
        {
            GameObject[] npcs = { chefarzt, kollege, patient };

            foreach (var npc in npcs)
            {
                if (npc != null)
                {
                    var locomotion = npc.GetComponent<NPCLocomotion>();
                    var controller = npc.GetComponent<NPCController>();

                    if (locomotion != null) locomotion.stopWalking();
                    if (controller != null) controller.stopSpeaking();
                }
            }
        }


        #endregion

        /// <summary>
        ///  // Convert the message to JSON and send it via TCP
        /// </summary>
        /// <param name="msg"></param>
        public void SendEventMessageToClient(EventMessage msg)
        {
            string s = JsonUtility.ToJson(msg);
            Debug.Log("Sending JSON: " + s);
            _tcpServer.SendMessageToClient(s);
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
