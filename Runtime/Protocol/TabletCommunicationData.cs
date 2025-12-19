using System;
using UnityEngine;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Data structure for sending scenario information to tablet
    /// This is a filtered version of SO_ScenarioData optimized for tablet UI
    /// </summary>
    [Serializable]
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

        public TabletScenarioData(string scenarioName, int scenarioId, string description)
        {
            this.scenarioName = scenarioName;
            this.scenarioId = scenarioId;
            this.description = description;
            this.loadingProgress = 0f;
            this.loadingStatus = "Initializing...";
            this.availableTasks = new string[] { "math_task", "nback_task", "noTasks" };
        }

        public void UpdateProgress(float progress, string status = "")
        {
            this.loadingProgress = Mathf.Clamp01(progress);
            if (!string.IsNullOrEmpty(status))
                this.loadingStatus = status;
        }

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
    [Serializable]
    public class NPCUIData
    {
        public string npcName;
        public string[] audioClips;
        public string[] audioLabels;

        public NPCUIData(string npcName, string[] audioClips, string[] audioLabels)
        {
            this.npcName = npcName;
            this.audioClips = audioClips ?? new string[0];
            this.audioLabels = audioLabels ?? new string[0];
        }

        public bool IsValid()
        {
            return audioClips.Length == audioLabels.Length && audioClips.Length > 0;
        }
    }

    /// <summary>
    /// Loading progress update message for tablet
    /// </summary>
    [Serializable]
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
        public static EventMessage CreateScenarioDataMessage(TabletScenarioData data)
        {
            string json = JsonUtility.ToJson(data);
            return new EventMessage("scenarioData", new string[] { json });
        }

        public static EventMessage CreateProgressMessage(float progress, string status, string scenarioName = "")
        {
            LoadingProgressData progressData = new LoadingProgressData(progress, status, scenarioName);
            string json = JsonUtility.ToJson(progressData);
            return new EventMessage("loadingProgress", new string[] { json });
        }

        public static EventMessage CreateNPCDataMessage(NPCUIData[] npcData)
        {
            var wrapper = new NPCDataWrapper { npcs = npcData };
            string json = JsonUtility.ToJson(wrapper);
            return new EventMessage("npcDataUpdate", new string[] { json });
        }

        public static EventMessage CreateCameraListMessage(string[] cameraNames)
        {
            var wrapper = new CameraListWrapper { cameras = cameraNames };
            string json = JsonUtility.ToJson(wrapper);
            return new EventMessage("cameraList", new string[] { json });
        }

        public static EventMessage CreateLoadingCompleteMessage(string scenarioName)
        {
            return new EventMessage("loadingComplete", new string[] { scenarioName });
        }

        // Wrapper classes for JSON serialization
        [Serializable]
        private class NPCDataWrapper { public NPCUIData[] npcs; }
        
        [Serializable]
        private class CameraListWrapper { public string[] cameras; }
    }
}
