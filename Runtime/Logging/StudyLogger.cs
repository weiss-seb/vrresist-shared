using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.SceneManagement;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Singleton logger for study data collection
    /// Persists across scenes and provides centralized logging functionality
    /// Uses folder structure: Log/ParticipantID/Scene{1-6}/sceneresults.csv
    /// </summary>
    public class StudyLogger : MonoBehaviour
    {
        #region Singleton Implementation

        private static StudyLogger _instance;
        private string currentSceneFolderPath;
        private string sceneResultsPath;
        private int currentSceneNumber = -1;
        private string currentParticipantId;

        public static StudyLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<StudyLogger>();

                    if (_instance == null)
                    {
                        GameObject go = new GameObject("StudyLogger");
                        _instance = go.AddComponent<StudyLogger>();
                        DontDestroyOnLoad(go);
                        Debug.Log("[StudyLogger] Singleton instance created automatically");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Lifecycle

        void Awake()
        {
            // Enforce singleton pattern
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(this.gameObject);
                Debug.Log("[StudyLogger] StudyLogger singleton initialized");
            }
            else if (_instance != this)
            {
                Debug.LogWarning("[StudyLogger] Duplicate StudyLogger instance destroyed");
                Destroy(gameObject);
                return;
            }
        }

        void Start()
        {
            // Ensure base log directory exists
            string baseLogPath = Path.Combine(Application.persistentDataPath, "Log");
            if (!Directory.Exists(baseLogPath))
            {
                Directory.CreateDirectory(baseLogPath);
            }
        }

        void OnApplicationQuit()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Scene Logging Initialization

        /// <summary>
        /// Initializes logging for a specific scene. Call this when entering a new scenario scene.
        /// Creates folder structure: Log/ParticipantID/Scene{sceneNumber}/
        /// </summary>
        /// <param name="sceneNumber">The scenario scene number (1-6)</param>
        public void InitializeSceneLogging(int sceneNumber)
        {
            currentParticipantId = PlayerPrefs.GetString("ParticipantID", "unknown");
            currentSceneNumber = sceneNumber;

            // Create folder structure: Log/ParticipantID/Scene{sceneNumber}/
            string baseLogPath = Path.Combine(Application.persistentDataPath, "Log");
            string participantFolder = Path.Combine(baseLogPath, currentParticipantId);
            currentSceneFolderPath = Path.Combine(participantFolder, $"Scene{sceneNumber}");

            // Create directories if they don't exist
            if (!Directory.Exists(currentSceneFolderPath))
            {
                Directory.CreateDirectory(currentSceneFolderPath);
            }

            // Set up file path
            sceneResultsPath = Path.Combine(currentSceneFolderPath, "sceneresults.csv");

            // Initialize scene results file with headers if it doesn't exist
            if (!File.Exists(sceneResultsPath))
            {
                using (StreamWriter sw = new StreamWriter(sceneResultsPath, false))
                {
                    sw.WriteLine("timestamp, eventType, data");
                    sw.WriteLine($"# Session started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sw.WriteLine($"# ParticipantID: {currentParticipantId}, Scene: {sceneNumber}");
                }
            }

            Debug.Log($"[StudyLogger] Initialized logging for Participant {currentParticipantId}, Scene {sceneNumber} at {currentSceneFolderPath}");
        }

        /// <summary>
        /// Gets the current scene folder path for external use
        /// </summary>
        public string GetCurrentSceneFolderPath()
        {
            return currentSceneFolderPath;
        }

        #endregion

        #region Logging Methods

        /// <summary>
        /// Write a timestamped line to the current scene's sceneresults.csv file
        /// </summary>
        /// <param name="line">The content to log</param>
        public void WriteLineToLog(string line)
        {
            if (string.IsNullOrEmpty(sceneResultsPath))
            {
                Debug.LogWarning("[StudyLogger] Scene logging not initialized. Call InitializeSceneLogging first.");
                // Fallback to legacy behavior for backward compatibility
                WriteLineToLegacyLog(line);
                return;
            }

            try
            {
                using (StreamWriter writer = new StreamWriter(sceneResultsPath, true))
                {
                    writer.WriteLine($"{DateTime.Now:HH:mm:ss}, EVENT, {line}");
                }
            }
            catch (IOException e)
            {
                Debug.LogError($"[StudyLogger] Failed to write to log: {e.Message}");
            }
        }

        /// <summary>
        /// Legacy fallback logging method - uses old scene-based file structure
        /// </summary>
        private void WriteLineToLegacyLog(string line)
        {
            StreamWriter writer = GetLegacyStreamWriter();
            if (writer != null)
            {
                writer.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ";" + line);
                writer.Flush();
                writer.Close();
            }
        }

        /// <summary>
        /// Legacy: Get a StreamWriter for the current scene's log file
        /// Creates the file and directory structure if they don't exist
        /// </summary>
        /// <returns>StreamWriter for the log file, or null if creation failed</returns>
        private StreamWriter GetLegacyStreamWriter()
        {
            Debug.Log("[StudyLogger] Using legacy log path for scene: " + SceneManager.GetActiveScene().name);
#if UNITY_EDITOR
            string path = "Assets/studyResults/";
#elif UNITY_ANDROID || UNITY_STANDALONE_WIN
            string path = Application.persistentDataPath + "/studyResults/";
#endif
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string LogFileName = SceneManager.GetActiveScene().name + ".csv";

            try
            {
                if (!File.Exists(path + LogFileName))
                {
                    StreamWriter writer = new StreamWriter(path + LogFileName, true);
                    writer.WriteLine("This file was created at " + DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"));
                    writer.Flush();
                    writer.Close();
                }
                return new StreamWriter(path + LogFileName, true);
            }
            catch (IOException e)
            {
                Debug.LogError("[StudyLogger] IOException: " + e.Message);
                return null;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if the StudyLogger singleton is properly initialized
        /// </summary>
        public static bool IsInitialized => _instance != null;

        /// <summary>
        /// Check if scene logging has been initialized
        /// </summary>
        public bool IsSceneLoggingInitialized => !string.IsNullOrEmpty(sceneResultsPath);

        /// <summary>
        /// Get the current log file path for debugging purposes
        /// </summary>
        public string GetCurrentLogPath()
        {
            if (!string.IsNullOrEmpty(sceneResultsPath))
            {
                return sceneResultsPath;
            }

            // Legacy fallback
#if UNITY_EDITOR
            string path = "Assets/studyResults/";
#elif UNITY_ANDROID || UNITY_STANDALONE_WIN
            string path = Application.persistentDataPath + "/studyResults/";
#endif
            return path + SceneManager.GetActiveScene().name + ".csv";
        }

        #endregion
    }
}
