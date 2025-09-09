using System;
using UnityEngine;

namespace OVGU.VAR.VRResist
{
    [CreateAssetMenu(fileName = "ScenarioData", menuName = "ScriptableObjects/ScenarioData", order = 1)]
    public class SO_ScenarioData : ScriptableObject
    {
        [Header("Scenario Information")]
        public int scenarioId;
        public string scenarioName;
        [TextArea(2, 4)]
        public string scenarioDescription;
        [TextArea(3, 6)]
        [Tooltip("Information text that will be displayed on the HMD during this scenario")]
        public string[] scenarioInfoTexts;

        [Header("Scene Configuration")]
        [Tooltip("Name of the Unity scene to load for this scenario")]
        public string sceneName;

        [Header("Chefarzt Audio Configuration")]
        [Tooltip("Audio clip names (identifiers) - actual AudioClip files are stored on HMD NPCController")]
        public string[] chefarztAudioClips;
        [Tooltip("German labels for UI buttons corresponding to audio clip names")]
        public string[] chefarztAudioLabels;

        [Tooltip("Animation emotions for each audio clip (must match array length of chefarztAudioClips)")]
        public ENUM_TalkEmotion[] chefarztTalkEmotions;

        [Header("Kollege Audio Configuration")]
        [Tooltip("Audio clip names (identifiers) - actual AudioClip files are stored on HMD NPCController")]
        public string[] kollegeAudioClips;
        [Tooltip("German labels for UI buttons corresponding to audio clip names")]
        public string[] kollegeAudioLabels;

        [Tooltip("Animation emotions for each audio clip (must match array length of kollegeAudioClips)")]
        public ENUM_TalkEmotion[] kollegeTalkEmotions;

        [Header("Patient Audio Configuration")]
        [Tooltip("Audio clip names (identifiers) - actual AudioClip files are stored on HMD NPCController")]
        public string[] patientAudioClips;
        [Tooltip("German labels for UI buttons corresponding to audio clip names")]
        public string[] patientAudioLabels;
        [Tooltip("Animation emotions for each audio clip (must match array length of patientAudioClips)")]
        public ENUM_TalkEmotion[] patientTalkEmotions;

        [Header("Father Audio Configuration")]
        [Tooltip("Audio clip names (identifiers) - actual AudioClip files are stored on HMD NPCController")]
        public string[] fatherAudioClips;
        [Tooltip("German labels for UI buttons corresponding to audio clip names")]

        public string[] fatherAudioLabels;
        [Tooltip("Animation emotions for each audio clip (must match array length of fatherAudioClips)")]
        public ENUM_TalkEmotion[] fatherTalkEmotions;

        [Header("Mother Audio Configuration")]
        [Tooltip("Audio clip names (identifiers) - actual AudioClip files are stored on HMD NPCController")]
        public string[] motherAudioClips;
        [Tooltip("German labels for UI buttons corresponding to audio clip names")]
        public string[] motherAudioLabels;
        [Tooltip("Animation emotions for each audio clip (must match array length of motherAudioClips)")]
        public ENUM_TalkEmotion[] motherTalkEmotions;

        [Header("Character Setup")]
        [Tooltip("Names of GameObjects to find in the scene (will be found by GameObject.Find()). Configure these to match your scene's character GameObject names.")]
        public string[] characterNames;

        [Tooltip("Tags to find characters by (alternative to names). Should correspond to characterNames array if used.")]
        public string[] characterTags;

        [Tooltip("Available waypoint/position names for NPC movement. Must match GameObject names in your scene.")]
        public string[] availablePositions;

        [Tooltip("Names of cameras to find in the scene. Must match Camera GameObject names in your scene.")]
        public string[] cameraTags;

        [Header("Loading Screen")]
        [Tooltip("Image to display during scene loading")]
        public Sprite loadingImage;

        [Tooltip("Helpful tip to show during loading")]
        public string loadingTip;

        [Header("Additional Data")]
        [Tooltip("Additional messages for this scenario")]
        public string[] messages;


        [Tooltip("Additional audio files for this scenario")]
        public string[] audioMessages;

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
                case "father": return fatherAudioClips;
                case "mother": return motherAudioClips;
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
                case "father": return fatherAudioLabels;
                case "mother": return motherAudioLabels;
                default: return new string[0];
            }
        }

        /// <summary>
        /// Get animation style for a specific audio clip
        /// </summary>
        public ENUM_TalkEmotion GetAnimationStyleForAudio(string npcName, string audioClipName)
        {
            string[] audioClips = GetAudioClipsForNPC(npcName);
            ENUM_TalkEmotion[] emotions = GetTalkEmotionsForNPC(npcName);

            if (audioClips == null || emotions == null || audioClips.Length != emotions.Length)
            {
                Debug.LogWarning($"[SO_ScenarioData] Audio clips and emotions arrays don't match for {npcName}, using Idle");
                return ENUM_TalkEmotion.Idle;
            }

            for (int i = 0; i < audioClips.Length; i++)
            {
                if (audioClips[i] == audioClipName)
                {
                    return emotions[i];
                }
            }

            Debug.LogWarning($"[SO_ScenarioData] Animation style not found for {npcName}.{audioClipName}, using Idle");
            return ENUM_TalkEmotion.Idle;
        }

        /// <summary>
        /// Get talk emotions array for a specific NPC
        /// </summary>
        private ENUM_TalkEmotion[] GetTalkEmotionsForNPC(string npcName)
        {
            switch (npcName.ToLower())
            {
                case "chefarzt": return chefarztTalkEmotions ?? new ENUM_TalkEmotion[0];
                case "kollege": return kollegeTalkEmotions ?? new ENUM_TalkEmotion[0];
                case "patient": return patientTalkEmotions ?? new ENUM_TalkEmotion[0];
                case "father": return fatherTalkEmotions ?? new ENUM_TalkEmotion[0];
                case "mother": return motherTalkEmotions ?? new ENUM_TalkEmotion[0];
                default: return new ENUM_TalkEmotion[0];
            }
        }


        #region Validation Methods

        /// <summary>
        /// Validate that the scenario data is properly configured
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        /// <summary>
        /// Validate that the scenario data is properly configured
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        public bool ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(scenarioName))
            {
                Debug.LogError($"[SO_ScenarioData] Scenario name is empty!");
                return false;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError($"[SO_ScenarioData] Scene name is empty for scenario: {scenarioName}");
                return false;
            }

            // Validate audio array lengths match
            ValidateAudioArrayLengths("Chefarzt", chefarztAudioClips, chefarztAudioLabels, chefarztTalkEmotions);
            ValidateAudioArrayLengths("Kollege", kollegeAudioClips, kollegeAudioLabels, kollegeTalkEmotions);
            ValidateAudioArrayLengths("Patient", patientAudioClips, patientAudioLabels, patientTalkEmotions);
            ValidateAudioArrayLengths("Father", fatherAudioClips, fatherAudioLabels, fatherTalkEmotions);
            ValidateAudioArrayLengths("Mother", motherAudioClips, motherAudioLabels, motherTalkEmotions);

            return true;
        }

        private void ValidateAudioArrayLengths(string npcName, string[] clips, string[] labels, ENUM_TalkEmotion[] emotions)
        {
            if (clips == null || labels == null || emotions == null) return;

            if (clips.Length != labels.Length || clips.Length != emotions.Length)
            {
                Debug.LogError($"[SO_ScenarioData] {npcName} audio arrays have mismatched lengths! " +
                              $"Clips: {clips.Length}, Labels: {labels.Length}, Emotions: {emotions.Length}");
            }
        }

        public (string[] clipNames, string[] labels) GetAudioDataForNPC(string npcName)
        {
            return (GetAudioClipsForNPC(npcName), GetAudioLabelsForNPC(npcName));
        }

        /// <summary>
        /// Get camera names for tablet UI
        /// </summary>
        /// <returns>Array of camera names</returns>
        public string[] GetCameraTags()
        {
            return cameraTags ?? new string[0];
        }

        #endregion
    }
}
