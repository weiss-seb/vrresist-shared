using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Events;
using System;
using TMPro;

[Serializable]
public class EventMessage
{
    public string type;
    public string[] content;

    public EventMessage(string eventName, string[] parameters)
    {
        this.type = eventName;
        this.content = parameters;
    }
    public EventMessage(string eventName)
    {
        this.type = eventName;
        this.content = new string[] { };
    }
}

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Simplified Event Trigger System - Gets references dynamically from ScenarioLoader
    /// Handles NPC movement, speech, environment interactions, and task display
    /// </summary>
    public class EventTriggerSystem : MonoBehaviour
    {

        private SO_ScenarioData scenarioData;

        [Header("Component References")]
        [SerializeField] private ScenarioLoader scenarioLoader;

        [Header("Environment Objects (Manual Assignment)")]

        public GameObject me;

        [Header("Task System References")]

        public MathTaskManager mathTaskManager;
        public NBackTask nBackTaskManager;

        [Header("Camera System")]
        public CameraControl cameraController;

        [Header("UI References")]
        public TMP_Text participantExplainText;


        [Header("Debug Settings")]
        [SerializeField] bool enableDetailedLogging = true;

        public UnityEvent OnAudioClipsLoaded;

        // Dynamic NPC references - populated by ScenarioLoader
        public GameObject patient, colleague, head_doctor, family_father, family_mother;
        private NPC patientNPC, colleagueNPC, head_doctorNPC, fatherNPC, motherNPC;

        // Legacy system variables - keeping for compatibility during transition
        private List<EventMessage> CurrentScenarioEventsList = new List<EventMessage>();
        private Coroutine eventCoroutine;
        private bool eventIsPlaying = false;
        private int currentInfoTextIndex = 0;

        // Waypoint mapping for easy access
        private Dictionary<string, GameObject> waypoints = new Dictionary<string, GameObject>();


        // little wrapper for NPC controller scripts
        private struct NPC
        {
            public NPC(GameObject nPCObject)
            {
                if (nPCObject != null)
                {
                    this.contr = nPCObject.GetComponent<NPCController>();
                    this.locom = nPCObject.GetComponent<NPCLocomotion>();
                }
                else
                {
                    this.contr = null;
                    this.locom = null;
                }
            }

            public NPCController contr;
            public NPCLocomotion locom;
        }

        // Start is called before the first frame update
        void Start()
        {
            // Find ScenarioLoader if not assigned
            if (scenarioLoader == null)
            {
                scenarioLoader = FindObjectOfType<ScenarioLoader>();
                if (scenarioLoader == null)
                {
                    Debug.LogError("[EventTriggerSystem] ScenarioLoader not found in scene!");
                    return;
                }
            }

            scenarioData = scenarioLoader.GetScenarioData();

            me = Camera.main?.gameObject;

            // Wait a frame to ensure ScenarioLoader has finished loading
            StartCoroutine(InitializeAfterScenarioLoad());
        }

        /// <summary>
        /// Initialize after ScenarioLoader has finished loading references
        /// </summary>
        private IEnumerator InitializeAfterScenarioLoad()
        {
            yield return null; // Wait one frame

            InitializeReferences();
        }

        /// <summary>
        /// Initialize all references from ScenarioLoader
        /// </summary>
        private void InitializeReferences()
        {
            if (scenarioLoader == null || scenarioData == null)
            {
                Debug.LogError("[EventTriggerSystem] ScenarioLoader or ScenarioData not available!");
                return;
            }

            // Get character references from ScenarioLoader
            GetCharacterReferences();

            // Get waypoint references from ScenarioLoader
            GetWaypointReferences();

            // Initialize NPC wrappers
            InitializeNPCs();

            // Find main camera
            me = GameObject.Find("Main Camera");
            if (me == null)
            {
                me = Camera.main?.gameObject;
            }

            // Get audio clips and initialize event system
            InitializeAudioEvents();

            if (enableDetailedLogging)
                Debug.Log("[EventTriggerSystem] Initialization complete");
        }

        /// <summary>
        /// Get character references from ScenarioLoader
        /// </summary>
        private void GetCharacterReferences()
        {
            if (scenarioData.characterNames == null || scenarioData.characterNames.Length == 0)
            {
                Debug.LogWarning("[EventTriggerSystem] No character names configured in scenario data!");
                return;
            }

            // Map characters based on array order (same as ScenarioLoader)
            for (int i = 0; i < scenarioData.characterNames.Length && i < 3; i++)
            {
                GameObject character = scenarioLoader.GetCharacter(scenarioData.characterNames[i]);
                if (character != null)
                {
                    switch (i)
                    {
                        case 0:
                            patient = character;
                            if (enableDetailedLogging)
                                Debug.Log($"[EventTriggerSystem] Got patient reference: {character.name}");
                            break;
                        case 1:
                            colleague = character;
                            if (enableDetailedLogging)
                                Debug.Log($"[EventTriggerSystem] Got colleague reference: {character.name}");
                            break;
                        case 2:
                            head_doctor = character;
                            if (enableDetailedLogging)
                                Debug.Log($"[EventTriggerSystem] Got head_doctor reference: {character.name}");
                            break;
                        case 3:
                            family_father = character;
                            if (enableDetailedLogging)
                                Debug.Log($"[EventTriggerSystem] Got family_father reference: {character.name}");
                            break;
                        case 4:
                            family_mother = character;
                            if (enableDetailedLogging)
                                Debug.Log($"[EventTriggerSystem] Got family_mother reference: {character.name}");
                            break;
                    }
                }
                else
                {
                    Debug.LogWarning($"[EventTriggerSystem] Character '{scenarioData.characterNames[i]}' not found via ScenarioLoader!");
                }
            }
        }

        /// <summary>
        /// Get waypoint references from ScenarioLoader
        /// </summary>
        private void GetWaypointReferences()
        {
            if (scenarioData.availablePositions == null || scenarioData.availablePositions.Length == 0)
            {
                Debug.LogWarning("[EventTriggerSystem] No waypoint positions configured in scenario data!");
                return;
            }

            // Get waypoints from ScenarioLoader and populate the dictionary
            waypoints.Clear();
            foreach (string positionName in scenarioData.availablePositions)
            {
                GameObject waypoint = scenarioLoader.GetWaypoint(positionName);
                if (waypoint != null)
                {
                    string key = positionName.ToLower();
                    waypoints[key] = waypoint;

                    if (enableDetailedLogging)
                        Debug.Log($"[EventTriggerSystem] Got waypoint reference: {positionName}");
                }
                else
                    Debug.LogWarning($"[EventTriggerSystem] Waypoint '{positionName}' not found via ScenarioLoader!");

            }
        }

        /// <summary>
        /// Initialize NPC wrapper structs
        /// </summary>
        private void InitializeNPCs()
        {
            patientNPC = new NPC(patient);
            colleagueNPC = new NPC(colleague);
            head_doctorNPC = new NPC(head_doctor);
            fatherNPC = new NPC(family_father);
            motherNPC = new NPC(family_mother);

            if (enableDetailedLogging)
            {
                Debug.Log($"[EventTriggerSystem] Initialized NPCs - Patient: {(patientNPC.contr != null ? "OK" : "MISSING")}, " +
                         $"Colleague: {(colleagueNPC.contr != null ? "OK" : "MISSING")}, " +
                         $"Head Doctor: {(head_doctorNPC.contr != null ? "OK" : "MISSING")}) + " +
                         $"Father: {(fatherNPC.contr != null ? "OK" : "MISSING")}, " +
                         $"Mother: {(motherNPC.contr != null ? "OK" : "MISSING")}");
            }
        }

        /// <summary>
        /// Initialize audio events from NPCs
        /// </summary>
        private void InitializeAudioEvents()
        {
            //Get the audio clips from the NPCs and add them to the event list for each NPC
            if (patient != null)
                AddAudioEvents("patient", getAudioClips(patient));
            if (colleague != null)
                AddAudioEvents("colleague", getAudioClips(colleague));
            if (head_doctor != null)
                AddAudioEvents("head_doctor", getAudioClips(head_doctor));
            if (family_father != null)
                AddAudioEvents("family_father", getAudioClips(family_father));
            if (family_mother != null)
                AddAudioEvents("family_mother", getAudioClips(family_mother));

        }


        //TODO possibly deprecated
        // adds scenario event to the event queue
        public void AddAudioEvents(string NPCName, string[] audioClipNames)
        {
            Debug.Log("Add Audio Events");
            switch (NPCName)
            {
                case "patient":
                    //  orderedEventList[0] = audioClipNames.ToList();
                    break;
                case "colleague":
                    //      orderedEventList[1] = audioClipNames.ToList();
                    break;
                case "head_doctor":
                    //  orderedEventList[2] = audioClipNames.ToList();
                    break;
                case "family_father":
                    break;
                case "family_mother":
                    break;

                default:
                    break;
            }

            OnAudioClipsLoaded.Invoke();
        }

        public void QueueScenarioEvent(EventMessage eventMsg)
        {
            // catch abort event, stop coroutines and NPC walking and speaking, clear event queue
            if (eventMsg.type == "abort")
            {
                switch (eventMsg.content[0])
                {
                    case "all":
                        if (eventCoroutine != null) StopCoroutine(eventCoroutine);

                        if (patientNPC.locom != null)
                        {
                            patientNPC.locom.stopWalking();
                            patientNPC.contr.stopSpeaking();
                        }
                        if (colleagueNPC.locom != null)
                        {
                            colleagueNPC.locom.stopWalking();
                            colleagueNPC.contr.stopSpeaking();
                        }
                        if (head_doctorNPC.locom != null)
                        {
                            head_doctorNPC.locom.stopWalking();
                            head_doctorNPC.contr.stopSpeaking();
                        }

                        CurrentScenarioEventsList.Clear();
                        eventIsPlaying = false;
                        break;

                    case "speaking":
                        switch (eventMsg.content[1])
                        {
                            case "nurse":
                                if (patientNPC.contr != null)
                                    patientNPC.contr.stopSpeaking();
                                break;
                            case "brother":
                                if (colleagueNPC.contr != null)
                                    colleagueNPC.contr.stopSpeaking();
                                break;
                            case "wife":
                                if (head_doctorNPC.contr != null)
                                    head_doctorNPC.contr.stopSpeaking();
                                break;
                        }
                        break;
                }
                return;
            }
            else
            {// add event to queue
                CurrentScenarioEventsList.Add(eventMsg);
            }
        }

        private void lookAndSpeak(NPC nPC, string clipName)
        {
            if (nPC.contr != null && nPC.locom != null)
            {
                nPC.contr.Speak(clipName);
                nPC.locom.lookAt(me);
                nPC.locom.setTurnTarget(me);
            }
        }

        public string[] getAudioClips(GameObject NPC)
        {
            if (NPC == null) return new string[0];

            NPCController controller = NPC.GetComponent<NPCController>();
            if (controller == null || controller.audioClips == null) return new string[0];

            string[] list = new string[controller.audioClips.Count];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = controller.audioClips[i].name;
            }
            return list;
        }

        // ===== NEW SIMPLIFIED METHODS =====
        // These replace the complex coroutine system with direct method calls

        /// <summary>
        /// Move an NPC to a specific position - Direct method call
        /// </summary>
        public void MoveNPCTo(string npcName, string position)
        {
            NPC npc = GetNPCByName(npcName);

            if (waypoints.TryGetValue(position.ToLower(), out GameObject waypoint))
            {
                if (npc.locom != null)
                {
                    npc.locom.walkToTarget(waypoint);
                    if (enableDetailedLogging)
                        Debug.Log($"[EventTriggerSystem] Moving {npcName} to {position}");
                }
            }
            else
            {
                Debug.LogWarning($"[EventTriggerSystem] Waypoint '{position}' not found or NPC '{npcName}' invalid!");
            }
        }

        public void SetInfoText()
        {
            if (participantExplainText == null)
            {
                Debug.LogError("[EventTriggerSystem] InfoTextUI not assigned!");
                return;
            }
            string text = scenarioData.scenarioInfoTexts.Length > currentInfoTextIndex ? scenarioData.scenarioInfoTexts[currentInfoTextIndex]
                : "";

            if (participantExplainText != null)
            {
                participantExplainText.SetText(text);
            }
            currentInfoTextIndex++;
        }

        public void TriggerPhoneCallIncoming()
        {
            Debug.Log("[EventTriggerSystem] Triggering phone call incoming event");
            AudioClip phoneCallSound = Resources.Load<AudioClip>("Audio/PhoneCallSound");
            GameObject.Find("PhoneCallSound").GetComponent<AudioSource>().PlayOneShot(phoneCallSound);

            // Here you would implement the logic to show the phone call UI in VR
            // For example, enabling a phone call panel or playing a ringtone sound

            // Example placeholder logic:
            // phoneCallUI.SetActive(true);
            // phoneRingtoneAudioSource.Play();
        }

        /// <summary>
        /// Make an NPC play an audio clip - Direct method call
        /// </summary>
        public void PlayNPCAudio(string npcName, string audioClip)
        {
            NPC npc = GetNPCByName(npcName);
            if (npc.contr != null && npc.locom != null)
            {
                npc.contr.Speak(audioClip, scenarioData.GetAnimationStyleForAudio(npcName, audioClip).ToString());
                npc.locom.lookAt(me);
                npc.locom.setTurnTarget(me);

                if (enableDetailedLogging)
                    Debug.Log($"[EventTriggerSystem] {npcName} speaking: {audioClip}");
            }
            else
            {
                Debug.LogWarning($"[EventTriggerSystem] NPC '{npcName}' not found or invalid!");
            }
        }

        /// <summary>
        /// Show math task - Direct method call
        /// </summary>
        public void ShowMathTask(ENUM_TaskDifficulty difficulty)
        {

            //mathTaskManager.ShowTask(difficulty, timeLimit);
            if (mathTaskManager != null)
            {
                // Assuming MathTaskManager has a method to show tasks
                mathTaskManager.StartSession(difficulty);
                Debug.Log($"[EventTriggerSystem] Showing math task: {difficulty}");

            }
            else
            {
                Debug.LogWarning("[EventTriggerSystem] MathTaskManager not assigned!");
            }
        }

        /// <summary>
        /// Show N-Back task - Direct method call
        /// </summary>
        public void ShowNBackTask(int nValue = 2, int timeLimit = 60)
        {
            if (nBackTaskManager != null)
            {
                // Assuming NBackTask has a method to show tasks
                nBackTaskManager.StartTask(nValue, timeLimit);
                Debug.Log($"[EventTriggerSystem] Showing N-Back task: N={nValue}, {timeLimit}s");
            }
            else
            {
                Debug.LogWarning("[EventTriggerSystem] NBackTaskManager not assigned!");
            }
        }

        /// <summary>
        /// Change camera view - Direct method call
        /// </summary>
        public void ChangeCameraView(int cameraIndex)
        {
            if (!cameraController)
            {
                cameraController = FindFirstObjectByType<CameraControl>();
            }

            if (cameraController != null)
            {
                cameraController.SetCameraToIndex(cameraIndex);
                if (enableDetailedLogging)
                    Debug.Log($"[EventTriggerSystem] Changed to camera {cameraIndex}");
            }
            else
            {
                Debug.LogWarning("[EventTriggerSystem] CameraController not in scene!");
            }
        }

        /// <summary>
        /// Abort all current activities - Direct method call
        /// </summary>
        public void AbortAll()
        {
            // Stop all NPCs
            if (patientNPC.locom != null)
            {
                patientNPC.locom.stopWalking();
                patientNPC.contr.stopSpeaking();
            }
            if (colleagueNPC.locom != null)
            {
                colleagueNPC.locom.stopWalking();
                colleagueNPC.contr.stopSpeaking();
            }
            if (head_doctorNPC.locom != null)
            {
                head_doctorNPC.locom.stopWalking();
                head_doctorNPC.contr.stopSpeaking();
            }

            // Stop any running coroutines
            if (eventCoroutine != null)
            {
                StopCoroutine(eventCoroutine);
                eventCoroutine = null;
            }

            // Clear event queue
            CurrentScenarioEventsList.Clear();
            eventIsPlaying = false;

            Debug.Log("[EventTriggerSystem] Aborted all activities");
        }

        /// <summary>
        /// End the study session - Direct method call
        /// </summary>
        public void EndStudy()
        {
            Debug.Log("[EventTriggerSystem] Ending study session");
            // It's safer to load by scene name or build index from a configuration file
            // rather than a hardcoded index.
            // For now, keeping the original logic.
            SceneManager.LoadScene(4); // Assuming scene 4 is the end scene
        }

        // ===== HELPER METHODS =====

        /// <summary>
        /// Get NPC struct by name
        /// </summary>
        private NPC GetNPCByName(string npcName)
        {
            switch (npcName.ToLower())
            {
                case "patient": return patientNPC;
                case "kollege": return colleagueNPC;
                case "head_doctor":
                case "chefarzt": // Added alias for consistency
                    return head_doctorNPC;
                case "father":
                    return fatherNPC;
                case "mother":
                    return motherNPC;
                default:
                    Debug.LogWarning($"[EventTriggerSystem] Unknown NPC: {npcName}");
                    return new NPC(); // Return an empty NPC struct to avoid null issues
            }
        }
    }
}
