using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Simplified message types for the study system
    /// Each type corresponds to a specific action that can be triggered from the remote tablet
    /// </summary>
    public enum StudyMessageType
    {
        NPC_WALK,       // Move an NPC to a specific position
        NPC_TALK,       // Make an NPC play a specific audio clip
        MATH_TASK,      // Show a math task to the participant
        NBACK_TASK,     // Show an n-back task to the participant
        CAMERA_CHANGE,  // Change the camera view
        ABORT_ALL,      // Stop all current activities
        END_STUDY       // End the study session
    }

    /// <summary>
    /// NPC identifiers for the study
    /// </summary>
    public enum NPCType
    {
        patient,
        colleague,
        head_doctor,
        mother,
        father
    }

    /// <summary>
    /// Simplified event control system for VR study
    /// Pre-configured UI sections that can be set up in Unity Editor instead of dynamic generation
    /// </summary>
    public class EventControl : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] WebSocketClient webSocketClient;
        [SerializeField] MessageHandler messageHandler;
        [SerializeField] EventTriggerSystem eventTriggerSystem;
        [SerializeField] StudyTaskManager studyTaskManager;

        [Header("NPC References")]
        [Tooltip("NPC GameObjects to get audio clips from")]
        [SerializeField] GameObject patientGameObject;
        [SerializeField] GameObject patientGameObject;
        [SerializeField] GameObject brotherObject;
        [SerializeField] GameObject wifeObject;
        [SerializeField] GameObject doctorObject;
        [SerializeField] GameObject anesthesiologistObject;

        [Header("Pre-configured UI Sections")]
        [Tooltip("UI Panels - configure these in Unity Editor with pre-made buttons")]
        [SerializeField] GameObject npcActionsPanel;
        [SerializeField] GameObject taskActionsPanel;
        [SerializeField] GameObject cameraActionsPanel;
        [SerializeField] GameObject studyControlPanel;

        [Header("NPC Action Buttons - Assign in Unity Editor")]
        [Tooltip("NPC Movement Buttons")]
        [SerializeField] Button[] nurseWalkButtons;
        [SerializeField] Button[] brotherWalkButtons;
        [SerializeField] Button[] wifeWalkButtons;

        [Tooltip("NPC Speech Buttons")]
        [SerializeField] Button[] nurseTalkButtons;
        [SerializeField] Button[] brotherTalkButtons;
        [SerializeField] Button[] wifeTalkButtons;

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

        [Tooltip("Available audio clips per NPC - will be auto-populated")]
        [SerializeField] string[] nurseAudioClips;
        [SerializeField] string[] brotherAudioClips;
        [SerializeField] string[] wifeAudioClips;
        [SerializeField] string[] doctorAudioClips;
        [SerializeField] string[] anesthesiologistAudioClips;

        [Header("Debug Settings")]
        [SerializeField] bool enableDetailedLogging = true;

        void Start()
        {
            // Auto-populate audio clips from NPC objects
            LoadAudioClipsFromNPCs();

            // Setup pre-configured UI button listeners
            SetupPreConfiguredUI();
        }

        /// <summary>
        /// Automatically load audio clips from NPC GameObjects
        /// </summary>
        void LoadAudioClipsFromNPCs()
        {
            Debug.Log("[EventControl] Loading audio clips from NPC objects...");

            // Load audio clips for each NPC if the GameObject is assigned
            if (patientGameObject != null)
                nurseAudioClips = GetAudioClipsFromNPC(patientGameObject).ToArray();
            if (patientGameObject != null)
                nurseAudioClips = GetAudioClipsFromNPC(patientGameObject).ToArray();

            if (brotherObject != null)
                brotherAudioClips = GetAudioClipsFromNPC(brotherObject).ToArray();

            if (wifeObject != null)
                wifeAudioClips = GetAudioClipsFromNPC(wifeObject).ToArray();

            if (doctorObject != null)
                doctorAudioClips = GetAudioClipsFromNPC(doctorObject).ToArray();

            if (anesthesiologistObject != null)
                anesthesiologistAudioClips = GetAudioClipsFromNPC(anesthesiologistObject).ToArray();

            Debug.Log($"[EventControl] Loaded audio clips - Nurse: {nurseAudioClips.Length}, Brother: {brotherAudioClips.Length}, Wife: {wifeAudioClips.Length}");
        }

        /// <summary>
        /// Get audio clip names from an NPC GameObject
        /// </summary>
        List<string> GetAudioClipsFromNPC(GameObject npcObject)
        {
            List<string> audioClips = new List<string>();

            if (npcObject != null)
            {
                var npcController = npcObject.GetComponent<NPCController>();
                if (npcController != null && npcController.audioClips != null)
                {
                    foreach (var audioClip in npcController.audioClips)
                    {
                        if (audioClip != null)
                        {
                            audioClips.Add(audioClip.name);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[EventControl] NPCController not found or no audio clips on {npcObject.name}");
                }
            }

            return audioClips;
        }

        /// <summary>
        /// Setup pre-configured UI button listeners instead of dynamic generation
        /// This method assigns actions to buttons that are already created in the Unity Editor
        /// </summary>
        void SetupPreConfiguredUI()
        {
            Debug.Log("[EventControl] Setting up pre-configured UI button listeners...");

            SetupNPCActionButtons();
            SetupTaskActionButtons();
            SetupCameraActionButtons();
            SetupStudyControlButtons();

            Debug.Log("[EventControl] Pre-configured UI setup complete!");
        }

        /// <summary>
        /// Setup NPC action buttons - assigns listeners to pre-configured buttons
        /// </summary>
        void SetupNPCActionButtons()
        {
            // Setup nurse walk buttons
            SetupWalkButtons(nurseWalkButtons, "nurse");
            SetupWalkButtons(brotherWalkButtons, "brother");
            SetupWalkButtons(wifeWalkButtons, "wife");

            // Setup nurse talk buttons
            SetupTalkButtons(nurseTalkButtons, "nurse", nurseAudioClips);
            SetupTalkButtons(brotherTalkButtons, "brother", brotherAudioClips);
            SetupTalkButtons(wifeTalkButtons, "wife", wifeAudioClips);
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

                    if (enableDetailedLogging)
                        Debug.Log($"[EventControl] Setup walk button: {npcName} → {position}");
                }
            }
        }

        /// <summary>
        /// Setup talk buttons for a specific NPC
        /// </summary>
        void SetupTalkButtons(Button[] buttons, string npcName, string[] audioClips)
        {
            if (buttons == null || audioClips == null) return;

            for (int i = 0; i < buttons.Length && i < audioClips.Length; i++)
            {
                if (buttons[i] != null)
                {
                    string audioClip = audioClips[i];
                    buttons[i].onClick.AddListener(() => SendNPCTalkCommand(npcName, audioClip));

                    if (enableDetailedLogging)
                        Debug.Log($"[EventControl] Setup talk button: {npcName} says {audioClip}");
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

                    if (enableDetailedLogging)
                        Debug.Log($"[EventControl] Setup camera button: Camera {cameraIndex + 1}");
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
            }

            if (endStudyButton != null)
            {
                endStudyButton.onClick.AddListener(() => SendStudyControlCommand("END_STUDY"));
            }

            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(() => SendStudyControlCommand("refresh"));
            }
        }

        /// <summary>
        /// Send NPC walk command to HMD
        /// </summary>
        void SendNPCWalkCommand(string npcName, string position)
        {
            var message = new EventMessage("NPC_WALK", new string[] { npcName, position });
            SendMessageToHMD(message);
            Debug.Log($"[EventControl] Sent NPC_WALK: {npcName} to {position}");
        }

        /// <summary>
        /// Send NPC talk command to HMD
        /// </summary>
        void SendNPCTalkCommand(string npcName, string audioClip)
        {
            var message = new EventMessage("NPC_TALK", new string[] { npcName, audioClip });
            SendMessageToHMD(message);
            Debug.Log($"[EventControl] Sent NPC_TALK: {npcName} says {audioClip}");
        }

        /// <summary>
        /// Send task command to HMD
        /// </summary>
        void SendTaskCommand(string taskType, string[] parameters)
        {
            var message = new EventMessage(taskType, parameters);
            SendMessageToHMD(message);
            Debug.Log($"[EventControl] Sent {taskType} with parameters: {string.Join(", ", parameters)}");
        }

        /// <summary>
        /// Send camera change command to HMD
        /// </summary>
        void SendCameraCommand(int cameraIndex)
        {
            var message = new EventMessage("CAMERA_CHANGE", new string[] { cameraIndex.ToString() });
            SendMessageToHMD(message);
            Debug.Log($"[EventControl] Sent CAMERA_CHANGE to index: {cameraIndex}");
        }

        /// <summary>
        /// Send study control command to HMD
        /// </summary>
        void SendStudyControlCommand(string controlType)
        {
            var message = new EventMessage(controlType, new string[] { });
            SendMessageToHMD(message);
            Debug.Log($"[EventControl] Sent study control: {controlType}");
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
                Debug.LogWarning("[EventControl] WebSocketClient is null!");
            }
        }

        /// <summary>
        /// Convert technical audio clip names to user-friendly names
        /// </summary>
        string GetFriendlyAudioName(string audioClipName)
        {
            // Add mappings for user-friendly names
            switch (audioClipName)
            {
                case "begruessung": return "Begrüßung";
                case "weiterFuehrenDerBehandlungBeteuern": return "Behandlung fortführen";
                case "begruessungBruder": return "Begrüßung Bruder";
                // Add more mappings as needed
                default: return audioClipName;
            }
        }
    }
}
