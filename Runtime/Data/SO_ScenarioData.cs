using System;
using UnityEngine;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// ScriptableObject defining scenario configuration data
    /// Used by both HMD (to load scenes/NPCs) and Tablet (to populate UI)
    /// </summary>
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
        [Tooltip("World Transforms for Cameras")]
        public CameraPosition[] cameraPositions;

        [Header("Chefarzt Audio Configuration")]
        [Tooltip("Audio clip names (identifiers) - actual AudioClip files are stored on HMD NPCController")]
        public string[] chefarztAudioClips;
        [Tooltip("German labels for UI buttons corresponding to audio clip names")]
        public string[] chefarztAudioLabels;
        [Tooltip("Animation emotions for each audio clip (must match array length of chefarztAudioClips)")]
        public ENUM_TalkEmotion[] chefarztTalkEmotions;

        [Header("Kollege Audio Configuration")]
        public string[] kollegeAudioClips;
        public string[] kollegeAudioLabels;
        public ENUM_TalkEmotion[] kollegeTalkEmotions;

        [Header("Patient Audio Configuration")]
        public string[] patientAudioClips;
        public string[] patientAudioLabels;
        public ENUM_TalkEmotion[] patientTalkEmotions;

        [Header("Father Audio Configuration")]
        public string[] fatherAudioClips;
        public string[] fatherAudioLabels;
        public ENUM_TalkEmotion[] fatherTalkEmotions;

        [Header("Mother Audio Configuration")]
        public string[] motherAudioClips;
        public string[] motherAudioLabels;
        public ENUM_TalkEmotion[] motherTalkEmotions;

        [Header("Character Setup")]
        [Tooltip("Names of GameObjects to find in the scene")]
        public string[] characterNames;
        [Tooltip("Tags to find characters by (alternative to names)")]
        public string[] characterTags;
        [Tooltip("Available waypoint/position names for NPC movement")]
        public string[] availablePositions;
        [Tooltip("Names of cameras to find in the scene")]
        public string[] cameraTags;

        [Header("Additional Data")]
        public string[] audioMessages;
        public Vector3 startPosition;

        #region Audio Data Access Methods

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

        public CameraPosition[] GetCameraPositions()
        {
            return cameraPositions ?? new CameraPosition[0];
        }

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

        public (string[] clipNames, string[] labels) GetAudioDataForNPC(string npcName)
        {
            return (GetAudioClipsForNPC(npcName), GetAudioLabelsForNPC(npcName));
        }

        public string[] GetCameraTags()
        {
            return cameraTags ?? new string[0];
        }

        #endregion

        #region Validation

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

        #endregion
    }

    [Serializable]
    public struct CameraPosition
    {
        public Vector3 position;
        public Vector3 rotation;
    }
}
