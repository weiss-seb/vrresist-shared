using UnityEngine;


namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Data structure for a single study scenario
    /// Contains all NPC configurations, audio clips, and positions for one scenario
    /// </summary>
    [System.Serializable]
    public class ScenarioData
    {
        [Header("Scenario Information")]
        public int scenarioId;
        public string scenarioName;
        [TextArea(2, 4)]
        public string scenarioDescription;
        [TextArea(3, 6)]
        [Tooltip("Information text that will be displayed on the HMD during this scenario")]
        public string scenarioInfoText;

        [Header("Scene Configuration")]
        [Tooltip("Name of the Unity scene to load for this scenario")]
        public string sceneName;

        [Header("NPC Starting Positions")]
        public Vector3 chefarztStartPosition;
        public Vector3 kollegeStartPosition;
        public Vector3 patientStartPosition;

        [Header("Participant Starting Location")]
        public Vector3 participantStartPosition;

        /// <summary>
        /// Available walk positions for NPCs
        /// </summary>
        [Header("Available Walk Positions")]
        public Transform[] availableWalkPositions;

        [Header("Chefarzt Audio Configuration")]
        [Tooltip("Audio clip names (identifiers) - actual AudioClip files are stored on HMD NPCController")]
        public string[] chefarztAudioClips;
        [Tooltip("German labels for UI buttons corresponding to audio clip names")]
        public string[] chefarztAudioLabels;

        [Header("Kollege Audio Configuration")]
        [Tooltip("Audio clip names (identifiers) - actual AudioClip files are stored on HMD NPCController")]
        public string[] kollegeAudioClips;
        [Tooltip("German labels for UI buttons corresponding to audio clip names")]
        public string[] kollegeAudioLabels;

        [Header("Patient Audio Configuration")]
        [Tooltip("Audio clip names (identifiers) - actual AudioClip files are stored on HMD NPCController")]
        public string[] patientAudioClips;
        [Tooltip("German labels for UI buttons corresponding to audio clip names")]
        public string[] patientAudioLabels;


        /// <summary>
        /// Get audio clips for a specific NPC
        /// </summary>
        public string[] GetAudioClipsForNPC(string npcName)
        {
            switch (npcName.ToLower())
            {
                case "chefarzt": return chefarztAudioClips;
                case "kollege": return kollegeAudioClips;
                case "patient": return patientAudioClips;
                default: return new string[0];
            }
        }

        /// <summary>
        /// Get audio labels for a specific NPC
        /// </summary>
        public string[] GetAudioLabelsForNPC(string npcName)
        {
            switch (npcName.ToLower())
            {
                case "chefarzt": return chefarztAudioLabels;
                case "kollege": return kollegeAudioLabels;
                case "patient": return patientAudioLabels;
                default: return new string[0];
            }
        }

        /// <summary>
        /// Get starting position for a specific NPC
        /// </summary>
        public Vector3 GetStartingPositionForNPC(string npcName)
        {
            switch (npcName.ToLower())
            {
                case "chefarzt": return chefarztStartPosition;
                case "kollege": return kollegeStartPosition;
                case "patient": return patientStartPosition;
                default: return Vector3.zero;
            }
        }
    }
}