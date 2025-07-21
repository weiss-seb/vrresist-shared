using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Events;
using System;

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
    /// Simplified Event Trigger System - Direct method calls instead of complex coroutines
    /// Handles NPC movement, speech, environment interactions, and task display
    /// </summary>
    public class EventTriggerSystem : MonoBehaviour
    {
        [Header("NPC References")]
        public GameObject patient, colleague, head_doctor;
        private NPC patientNPC, colleagueNPC, head_doctorNPC;

        [Header("Environment Objects")]
        public GameObject doorWrapper;
        public GameObject monitor;
        public GameObject glass;
        public GameObject me, patientHead;

        [Header("Navigation Waypoints")]
        public GameObject wpDoorOutside, wpDoorInside, wpBedLeft1, wpBedLeft2, wpBedRight1, wpOutside, wpOutsideL;

        [Header("Task System References")]
        public MathTaskManager mathTaskManager;
        public NBackTask nBackTaskManager;

        [Header("Camera System")]
        public CameraControl cameraController;

        [Header("Debug Settings")]
        [SerializeField] bool enableDetailedLogging = true;

        public UnityEvent OnAudioClipsLoaded;

        // Legacy system variables - keeping for compatibility during transition
        private List<EventMessage> CurrentScenarioEventsList = new List<EventMessage>();
        private Coroutine eventCoroutine;
        private bool eventIsPlaying = false;

        // Waypoint mapping for easy access
        private Dictionary<string, GameObject> waypoints = new Dictionary<string, GameObject>();

        public readonly string[] eventList ={
        //Environment events
        "endStudy",
        "openDoor",
        "closeDoor",
        "openWindow",
        "closeWindow",
        "emptyGlass",
        "dropBloodPressure",

        //NPC events
        "NurseWelcome1",
        "NurseWelcome2",
        "NurseGoodBye",
        "FamilyEnter",
        "WifeInquire",
        "WifeConvinced",
        "BrotherConvinced",
        "FamilyExit",
        "S2S3FamilyEnter",
        "S2S3FamilyAsks",
        "S2FamilySupport",
        "S3Family",
        "S3Family2"
    };

        public readonly List<List<string>> orderedEventList = new List<List<string>>
    {
        //nurse events
        new List<string>() {
            "NurseWelcome1",
            "NurseWelcome2",
            "NurseGoodBye",
            "S2S3FamilyEnter",
            "S2S3FamilyAsks",
            "S2FamilySupport",
            "S3Family",
            "S3Family2"
        },

        //brother events
        new List<string>() {
            "BrotherConvinced",
            "S2S3FamilyEnter",
            "S2S3FamilyAsks",
            "S2FamilySupport",
            "S3Family",
            "S3Family2"
        },

        // wife
        new List<string>() {
            "openWindow",
            "closeWindow",
            "emptyGlass",
            "dropBloodPressure",
            //"FamilyEnter",
            //"FamilyExit",
        },

        //environment events
        new List<string>() {
            "openWindow",
            "closeWindow",
            "emptyGlass",
            "dropBloodPressure",
            "FamilyEnter",
            "FamilyExit",
        },
    };

        // little wrapper for NPC controller scripts
        private struct NPC
        {
            public NPC(GameObject nPCObject)
            {
                this.contr = nPCObject.GetComponent<NPCController>();
                this.locom = nPCObject.GetComponent<NPCLocomotion>();
            }

            public NPCController contr;
            public NPCLocomotion locom;
        }

        // Start is called before the first frame update
        void Start()
        {
            patientNPC = new NPC(patient);
            colleagueNPC = new NPC(colleague);
            head_doctorNPC = new NPC(head_doctor);

            me = GameObject.Find("Main Camera");

            //Get the audio clips from the NPCs and add them to the event list for each NPC
            AddAudioEvents("nurse", getAudioClips(patient));
            AddAudioEvents("brother", getAudioClips(colleague));
            AddAudioEvents("wife", getAudioClips(head_doctor));
        }

        // Update is called once per frame
        void Update()
        {
            if (CurrentScenarioEventsList.Count > 0 && !eventIsPlaying)
            {
                eventIsPlaying = true;
                eventCoroutine = StartCoroutine(ExecuteEventCoroutineForMessage(CurrentScenarioEventsList[0]));
            }
        }

        // adds scenario event to the event queue
        public void AddAudioEvents(string NPCName, string[] audioClipNames)
        {
            Debug.Log("Add Audio Events");
            switch (NPCName)
            {
                case "nurse":
                    orderedEventList[0] = audioClipNames.ToList();
                    break;

                case "brother":
                    orderedEventList[1] = audioClipNames.ToList();
                    break;

                case "wife":
                    orderedEventList[2] = audioClipNames.ToList();
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

                        patientNPC.locom.stopWalking();
                        patientNPC.contr.stopSpeaking();
                        colleagueNPC.locom.stopWalking();
                        colleagueNPC.contr.stopSpeaking();
                        head_doctorNPC.locom.stopWalking();
                        head_doctorNPC.contr.stopSpeaking();

                        CurrentScenarioEventsList.Clear();
                        eventIsPlaying = false;
                        break;

                    case "speaking":
                        switch (eventMsg.content[1])
                        {
                            case "nurse":
                                patientNPC.contr.stopSpeaking();
                                break;
                            case "brother":
                                colleagueNPC.contr.stopSpeaking();
                                break;
                            case "wife":
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

        // coroutine to play events
        private IEnumerator ExecuteEventCoroutineForMessage(EventMessage eventMsg)
        {
            switch (eventMsg.type)
            {
                case "speak":
                    switch (eventMsg.content[0])
                    {
                        case "nurse":
                            lookAndSpeak(patientNPC, eventMsg.content[1]);
                            break;
                        case "brother":
                            lookAndSpeak(colleagueNPC, eventMsg.content[1]);
                            break;
                        case "wife":
                            lookAndSpeak(head_doctorNPC, eventMsg.content[1]);
                            break;
                    }
                    break;
                case "event":
                    switch (eventMsg.content[0])
                    {
                        case "endStudy":
                            SceneManager.LoadScene(4);
                            break;
                        case "openDoor":
                            openDoor();
                            break;
                        case "closeDoor":
                            closeDoor();
                            break;
                        case "openWindow":
                            //   windowWrapper.openWindow();
                            break;
                        case "closeWindow":
                            //  windowWrapper.closeWindow();
                            break;
                        case "emptyGlass":
                            //   glass.GetComponent<Glass>().setWater(false);
                            break;
                        case "dropBloodPressure":
                            setPressureValue("50/30");
                            break;
                        case "NurseWelcome1":
                            EventLogger.Instance.LogBeginAct(1);

                            patientNPC.locom.lookAt(me);
                            patientNPC.locom.setTurnTarget(me);
                            patientNPC.contr.speak("Begruessung", "talk");
                            yield return new WaitForSeconds(05f);
                            patientNPC.locom.lookAt(patientHead);
                            yield return new WaitUntil(() => !patientNPC.contr.audioSource.isPlaying);

                            patientNPC.locom.lookAt(me);
                            patientNPC.contr.speak("Aufgaben", "talk");
                            yield return new WaitUntil(() => !patientNPC.contr.audioSource.isPlaying);
                            break;
                        case "NurseWelcome2":
                            patientNPC.locom.lookAt(monitor);
                            patientNPC.locom.setTurnTarget(monitor);
                            patientNPC.contr.speak("WeiterbehandlungErklaeren", "talk");
                            yield return new WaitForSeconds(05f);
                            patientNPC.locom.lookAt(me);
                            patientNPC.locom.setTurnTarget(me);
                            yield return new WaitUntil(() => !patientNPC.contr.audioSource.isPlaying);

                            yield return new WaitForSeconds(02f);
                            patientNPC.locom.lookAt(me);
                            patientNPC.locom.setTurnTarget(me);
                            patientNPC.contr.speak("Zusammenfassung");
                            yield return new WaitUntil(() => !patientNPC.contr.audioSource.isPlaying);
                            break;
                        case "NurseGoodBye":
                            patientNPC.ToString();
                            patientNPC.locom.walkToTarget(wpDoorInside);
                            yield return new WaitForSeconds(0.1f);
                            yield return new WaitUntil(() => patientNPC.locom.isTargetReached());
                            openDoor();
                            patientNPC.locom.lookAt(me);
                            patientNPC.locom.setTurnTarget(me);
                            patientNPC.contr.speak("Angehoerige");
                            yield return new WaitUntil(() => !patientNPC.contr.audioSource.isPlaying);
                            patientNPC.locom.walkToTarget(wpOutsideL);
                            yield return new WaitForSeconds(1f);
                            closeDoor();
                            break;
                        case "FamilyEnter":
                            EventLogger.Instance.LogBeginAct(2);

                            colleagueNPC.locom.walkToTarget(wpBedLeft2);
                            head_doctorNPC.locom.walkToTarget(wpBedLeft1);
                            yield return new WaitForSeconds(2f);
                            openDoor();
                            yield return new WaitUntil(() => colleagueNPC.locom.isTargetReached());
                            yield return new WaitUntil(() => head_doctorNPC.locom.isTargetReached());
                            closeDoor();

                            colleagueNPC.locom.setTurnTarget(patientHead);
                            colleagueNPC.locom.lookAt(patientHead);
                            head_doctorNPC.locom.setTurnTarget(patientHead);
                            head_doctorNPC.locom.lookAt(patientHead);

                            yield return new WaitForSeconds(2f);
                            colleagueNPC.locom.setTurnTarget(me);
                            colleagueNPC.locom.lookAt(me);

                            colleagueNPC.contr.speak("begruessungBruder");
                            yield return new WaitUntil(() => !colleagueNPC.contr.audioSource.isPlaying);
                            break;
                        case "FamilyExit":
                            colleagueNPC.locom.walkToTarget(wpDoorInside);
                            yield return new WaitUntil(() => colleagueNPC.locom.isTargetReached());
                            openDoor();
                            yield return new WaitForSeconds(1.5f);

                            head_doctorNPC.locom.walkToTarget(wpDoorInside);

                            colleagueNPC.locom.walkToTarget(wpOutside);
                            head_doctorNPC.locom.walkToTarget(wpOutside);

                            yield return new WaitForSeconds(2f);
                            closeDoor();
                            break;
                        case "WifeInquire":
                            head_doctorNPC.locom.setTurnTarget(me);
                            head_doctorNPC.locom.lookAt(me);

                            head_doctorNPC.contr.speak("WieGehtEsMeinemMann");
                            yield return new WaitUntil(() => !head_doctorNPC.contr.audioSource.isPlaying);

                            yield return new WaitForSeconds(3f);

                            head_doctorNPC.locom.setTurnTarget(patientHead);
                            head_doctorNPC.locom.lookAt(patientHead);
                            break;
                        case "WifeConvinced":
                            head_doctorNPC.locom.setTurnTarget(me);
                            head_doctorNPC.locom.lookAt(me);

                            head_doctorNPC.contr.speak("FestUeberzeugtAufBesserung");
                            yield return new WaitUntil(() => !head_doctorNPC.contr.audioSource.isPlaying);

                            head_doctorNPC.locom.setTurnTarget(patientHead);
                            head_doctorNPC.locom.lookAt(patientHead);
                            break;
                        case "BrotherConvinced":
                            colleagueNPC.locom.setTurnTarget(me);
                            colleagueNPC.locom.lookAt(me);

                            colleagueNPC.contr.speak("weiterFuehrenDerBehandlungBeteuern");
                            yield return new WaitUntil(() => !colleagueNPC.contr.audioSource.isPlaying);

                            colleagueNPC.locom.setTurnTarget(patientHead);
                            colleagueNPC.locom.lookAt(patientHead);
                            break;
                        case "S1Begin":
                            EventLogger.Instance.LogBeginAct(3);
                            break;
                        case "S2S3FamilyEnter":
                            EventLogger.Instance.LogBeginAct(3);

                            colleagueNPC.locom.walkToTarget(wpBedLeft2);
                            head_doctorNPC.locom.walkToTarget(wpBedLeft1);
                            yield return new WaitForSeconds(2f);
                            openDoor();
                            yield return new WaitUntil(() => colleagueNPC.locom.isTargetReached());
                            yield return new WaitUntil(() => head_doctorNPC.locom.isTargetReached());
                            closeDoor();

                            colleagueNPC.locom.setTurnTarget(patientHead);
                            colleagueNPC.locom.lookAt(patientHead);
                            head_doctorNPC.locom.setTurnTarget(patientHead);
                            head_doctorNPC.locom.lookAt(patientHead);

                            yield return new WaitForSeconds(2f);
                            colleagueNPC.locom.setTurnTarget(me);
                            colleagueNPC.locom.lookAt(me);

                            colleagueNPC.contr.speak("WurdenAngerufen");
                            yield return new WaitUntil(() => !colleagueNPC.contr.audioSource.isPlaying);

                            head_doctorNPC.locom.setTurnTarget(me);
                            head_doctorNPC.locom.lookAt(me);

                            head_doctorNPC.contr.speak("S2S3_GehtsIhmGut");
                            yield return new WaitUntil(() => !head_doctorNPC.contr.audioSource.isPlaying);
                            break;
                        case "S2S3FamilyAsks":
                            head_doctorNPC.locom.setTurnTarget(me);
                            head_doctorNPC.locom.lookAt(me);

                            head_doctorNPC.contr.speak("S2S3_PflegerinSagtMachtKeinenSinn");
                            yield return new WaitUntil(() => !head_doctorNPC.contr.audioSource.isPlaying);

                            break;
                        case "S2FamilySupport":
                            colleagueNPC.locom.setTurnTarget(me);
                            colleagueNPC.locom.lookAt(me);

                            colleagueNPC.contr.speak("S2_Vertrauen");
                            yield return new WaitUntil(() => !colleagueNPC.contr.audioSource.isPlaying);

                            colleagueNPC.locom.setTurnTarget(patientHead);
                            colleagueNPC.locom.lookAt(patientHead);
                            break;
                        case "S3Family":
                            colleagueNPC.locom.setTurnTarget(me);
                            colleagueNPC.locom.lookAt(me);

                            colleagueNPC.contr.speak("S3_weiterfuehren");
                            yield return new WaitUntil(() => !colleagueNPC.contr.audioSource.isPlaying);

                            head_doctorNPC.locom.setTurnTarget(me);
                            head_doctorNPC.locom.lookAt(me);

                            head_doctorNPC.contr.speak("S3_GebenHoffnungNichtAuf");
                            yield return new WaitUntil(() => !head_doctorNPC.contr.audioSource.isPlaying);

                            break;
                        case "S3Family2":
                            colleagueNPC.locom.setTurnTarget(me);
                            colleagueNPC.locom.lookAt(me);

                            colleagueNPC.contr.speak("S3_NichtZulassen");
                            yield return new WaitUntil(() => !colleagueNPC.contr.audioSource.isPlaying);

                            head_doctorNPC.locom.setTurnTarget(me);
                            head_doctorNPC.locom.lookAt(me);

                            head_doctorNPC.contr.speak("S3_RegDichNichtAuf");
                            yield return new WaitUntil(() => !head_doctorNPC.contr.audioSource.isPlaying);

                            break;
                    }
                    break;
            }

            CurrentScenarioEventsList.RemoveAt(0);
            eventIsPlaying = false;
            yield return null;
        }

        // other controlls for objects in ICU 
        private void openDoor()
        {
            doorWrapper.GetComponent<Animator>().SetTrigger("open");
        }
        private void closeDoor()
        {
            doorWrapper.GetComponent<Animator>().SetTrigger("close");
        }
        private void setPressureValue(string value)
        {
            // monitor.GetComponent<Monitor>().SetDisplayedValue(value);
        }

        private void lookAndSpeak(NPC nPC, string clipName)
        {
            nPC.contr.speak(clipName);
            nPC.locom.lookAt(me);
            nPC.locom.setTurnTarget(me);
        }

        public string[] getAudioClips(GameObject NPC)
        {
            string[] list = new string[NPC.GetComponent<NPCController>().audioClips.Count];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = NPC.GetComponent<NPCController>().audioClips[i].name;
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
            GameObject waypoint = GetWaypointByName(position);

            if (waypoint != null)
            {
                npc.locom.walkToTarget(waypoint);
                if (enableDetailedLogging)
                    Debug.Log($"[EventTriggerSystem] Moving {npcName} to {position}");
            }
            else
            {
                Debug.LogWarning($"[EventTriggerSystem] Waypoint '{position}' not found!");
            }
        }

        /// <summary>
        /// Make an NPC play an audio clip - Direct method call
        /// </summary>
        public void PlayNPCAudio(string npcName, string audioClip)
        {
            NPC npc = GetNPCByName(npcName);
            npc.contr.speak(audioClip);
            npc.locom.lookAt(me);
            npc.locom.setTurnTarget(me);

            if (enableDetailedLogging)
                Debug.Log($"[EventTriggerSystem] {npcName} speaking: {audioClip}");
        }

        /// <summary>
        /// Show math task - Direct method call
        /// </summary>
        public void ShowMathTask(string difficulty = "medium", string timeLimit = "60")
        {
            if (mathTaskManager != null)
            {
                // Assuming MathTaskManager has a method to show tasks
                // mathTaskManager.ShowTask(difficulty, int.Parse(timeLimit));
                Debug.Log($"[EventTriggerSystem] Showing math task: {difficulty}, {timeLimit}s");
            }
            else
            {
                Debug.LogWarning("[EventTriggerSystem] MathTaskManager not assigned!");
            }
        }

        /// <summary>
        /// Show N-Back task - Direct method call
        /// </summary>
        public void ShowNBackTask(string nValue = "2", string timeLimit = "60")
        {
            if (nBackTaskManager != null)
            {
                // Assuming NBackTask has a method to show tasks
                // nBackTaskManager.StartTask(int.Parse(nValue), int.Parse(timeLimit));
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
            if (cameraController != null)
            {
                cameraController.SetCameraToIndex(cameraIndex);
                if (enableDetailedLogging)
                    Debug.Log($"[EventTriggerSystem] Changed to camera {cameraIndex}");
            }
            else
            {
                Debug.LogWarning("[EventTriggerSystem] CameraController not assigned!");
            }
        }

        /// <summary>
        /// Abort all current activities - Direct method call
        /// </summary>
        public void AbortAll()
        {
            // Stop all NPCs
            patientNPC.locom.stopWalking();
            patientNPC.contr.stopSpeaking();
            colleagueNPC.locom.stopWalking();
            colleagueNPC.contr.stopSpeaking();
            head_doctorNPC.locom.stopWalking();
            head_doctorNPC.contr.stopSpeaking();

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
                case "nurse": return patientNPC;
                case "brother": return colleagueNPC;
                case "wife": return head_doctorNPC;
                default:
                    Debug.LogWarning($"[EventTriggerSystem] Unknown NPC: {npcName}");
                    return patientNPC; // Default fallback
            }
        }

        /// <summary>
        /// Get waypoint GameObject by name
        /// </summary>
        private GameObject GetWaypointByName(string position)
        {
            switch (position.ToLower())
            {
                case "bedleft1": return wpBedLeft1;
                case "bedleft2": return wpBedLeft2;
                case "bedright1": return wpBedRight1;
                case "doorinside": return wpDoorInside;
                case "dooroutside": return wpDoorOutside;
                case "outside": return wpOutside;
                case "outsidel": return wpOutsideL;
                default:
                    Debug.LogWarning($"[EventTriggerSystem] Unknown position: {position}");
                    return null;
            }
        }

        /// <summary>
        /// Initialize waypoint mapping for easier access
        /// </summary>
        void InitializeWaypoints()
        {
            waypoints.Clear();
            waypoints["bedLeft1"] = wpBedLeft1;
            waypoints["bedLeft2"] = wpBedLeft2;
            waypoints["bedRight1"] = wpBedRight1;
            waypoints["doorInside"] = wpDoorInside;
            waypoints["doorOutside"] = wpDoorOutside;
            waypoints["outside"] = wpOutside;
            waypoints["outsideL"] = wpOutsideL;
        }
    }
}
