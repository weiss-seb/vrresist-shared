using System;
using UnityEngine;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Data structure for sending scenario information to tablet
    /// This is a filtered version of SO_ScenarioData optimized for tablet UI
    /// </summary>
    [System.Serializable]
    public class TabletScenarioData
    {
        [Header("Basic Info")]
        public string scenarioName;
        public int scenarioId;
        public string description;

        [Header("Interactive Elements")]
        public NPCUIData[] npcs;
        public string[] availablePositions;
        public string[] availableCameras;
        public string[] availableTasks;

        [Header("Loading Progress")]
        public float loadingProgress;
        public string loadingStatus;

        /// <summary>
        /// Constructor for creating tablet scenario data
        /// </summary>
        public TabletScenarioData(string scenarioName, int scenarioId, string description)
        {
            this.scenarioName = scenarioName;
            this.scenarioId = scenarioId;
            this.description = description;
            this.loadingProgress = 0f;
            this.loadingStatus = "Initializing...";

            // Initialize default task list
            this.availableTasks = new string[] { "math_task", "nback_task", "noTasks" };
        }

        /// <summary>
        /// Update loading progress
        /// </summary>
        public void UpdateProgress(float progress, string status = "")
        {
            this.loadingProgress = Mathf.Clamp01(progress);
            if (!string.IsNullOrEmpty(status))
                this.loadingStatus = status;
        }

        /// <summary>
        /// Mark as loading complete
        /// </summary>
        public void SetComplete()
        {
            this.loadingProgress = 1f;
            this.loadingStatus = "Ready";
        }
    }

    /// <summary>
    /// NPC data structure for tablet UI
    /// Contains audio clips and user-friendly labels
    /// </summary>
    [System.Serializable]
    public class NPCUIData
    {
        public string npcName;
        public string[] audioClips;
        public string[] audioLabels;

        /// <summary>
        /// Constructor for NPC UI data
        /// </summary>
        public NPCUIData(string npcName, string[] audioClips, string[] audioLabels)
        {
            this.npcName = npcName;
            this.audioClips = audioClips ?? new string[0];
            this.audioLabels = audioLabels ?? new string[0];
        }

        /// <summary>
        /// Validate that audio clips and labels match
        /// </summary>
        public bool IsValid()
        {
            return audioClips.Length == audioLabels.Length && audioClips.Length > 0;
        }
    }

    /// <summary>
    /// Loading progress update message for tablet
    /// </summary>
    [System.Serializable]
    public class LoadingProgressData
    {
        public float progress;
        public string status;
        public string scenarioName;

        public LoadingProgressData(float progress, string status, string scenarioName = "")
        {
            this.progress = Mathf.Clamp01(progress);
            this.status = status ?? "Loading...";
            this.scenarioName = scenarioName ?? "";
        }
    }

    /// <summary>
    /// Scene loading status for internal use
    /// </summary>
    public enum SceneLoadingStatus
    {
        Idle,
        PreparingLoad,
        Loading,
        PopulatingData,
        Complete,
        Error
    }

    /// <summary>
    /// Helper class for creating tablet communication messages
    /// </summary>
    public static class TabletMessageFactory
    {
        /// <summary>
        /// Create scenario data message for tablet
        /// </summary>
        public static EventMessage CreateScenarioDataMessage(TabletScenarioData data)
        {
            string json = JsonUtility.ToJson(data);
            return new EventMessage("scenarioData", new string[] { json });
        }

        /// <summary>
        /// Create loading progress message for tablet
        /// </summary>
        public static EventMessage CreateProgressMessage(float progress, string status, string scenarioName = "")
        {
            LoadingProgressData progressData = new LoadingProgressData(progress, status, scenarioName);
            string json = JsonUtility.ToJson(progressData);
            return new EventMessage("loadingProgress", new string[] { json });
        }

        /// <summary>
        /// Create NPC data update message for tablet
        /// </summary>
        public static EventMessage CreateNPCDataMessage(NPCUIData[] npcData)
        {
            // Create a wrapper for the NPC data array
            var wrapper = new { npcs = npcData };
            string json = JsonUtility.ToJson(wrapper);
            return new EventMessage("npcDataUpdate", new string[] { json });
        }

        /// <summary>
        /// Create camera list message for tablet
        /// </summary>
        public static EventMessage CreateCameraListMessage(string[] cameraNames)
        {
            var wrapper = new { cameras = cameraNames };
            string json = JsonUtility.ToJson(wrapper);
            return new EventMessage("cameraList", new string[] { json });
        }

        /// <summary>
        /// Create loading complete message for tablet
        /// </summary>
        public static EventMessage CreateLoadingCompleteMessage(string scenarioName)
        {
            return new EventMessage("loadingComplete", new string[] { scenarioName });
        }
    }
}
