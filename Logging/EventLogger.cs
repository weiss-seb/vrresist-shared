using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace OVGU.VAR.VRResist.Logging
{
    public enum LogType
    {
        TLX,
        Mannequin,
        MathTask,
        NBackTask
    }

    public class EventLogger : MonoBehaviour
    {
        private string currentSceneFolderPath;
        private string sceneResultsPath;
        private string questionnaireResultsPath;
        private string mentalLoadResultsPath;

        private static EventLogger instance;
        Dictionary<string, string> resultList = new Dictionary<string, string>();

        private string currentParticipantId;
        private int currentSceneNumber = -1;

        // Static singleton property
        public static EventLogger Instance
        {
            // Here we use the ?? operator, to return 'instance' if 'instance' does not equal null
            // otherwise we assign instance to a new component and return that
            get { return instance ?? (instance = new GameObject("EventLogger").AddComponent<EventLogger>()); }
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Start is called before the first frame update
        void Start()
        {
            // Ensure base log directory exists
            string baseLogPath = Path.Combine(Application.persistentDataPath, "Log");
            if (!Directory.Exists(baseLogPath))
            {
                Directory.CreateDirectory(baseLogPath);
            }
        }

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

            // Set up file paths
            sceneResultsPath = Path.Combine(currentSceneFolderPath, "sceneresults.csv");
            questionnaireResultsPath = Path.Combine(currentSceneFolderPath, "questionnaireresults.csv");
            mentalLoadResultsPath = Path.Combine(currentSceneFolderPath, "mentalload_results.csv");

            // Initialize scene results file with headers if it doesn't exist (for scenario scene events)
            if (!File.Exists(sceneResultsPath))
            {
                using (StreamWriter sw = new StreamWriter(sceneResultsPath, false))
                {
                    sw.WriteLine("timestamp, eventType, data");
                }
            }

            // Initialize questionnaire results file with headers if it doesn't exist
            if (!File.Exists(questionnaireResultsPath))
            {
                using (StreamWriter sw = new StreamWriter(questionnaireResultsPath, false))
                {
                    sw.WriteLine("timestamp, item, result");
                }
            }

            // Initialize mental load results file with headers if it doesn't exist (MathTask, NBack)
            if (!File.Exists(mentalLoadResultsPath))
            {
                using (StreamWriter sw = new StreamWriter(mentalLoadResultsPath, false))
                {
                    sw.WriteLine("timestamp, taskType, difficulty, operation, firstNumber, secondNumber, userAnswer, correctAnswer, timeTakenMs, isCorrect, elapsedTimeMs, remainingTimeMs, userChoice");
                }
            }

            Debug.Log($"[EventLogger] Initialized logging for Participant {currentParticipantId}, Scene {sceneNumber} at {currentSceneFolderPath}");
        }

        /// <summary>
        /// Gets the current scene folder path for external use
        /// </summary>
        public string GetCurrentSceneFolderPath()
        {
            return currentSceneFolderPath;
        }

        public void LogQuestionnaireItem(string index, string value)
        {
            if (resultList.ContainsKey(index))
                resultList[index] = value;
            else
                resultList.Add(index, value);
        }

        /// <summary>
        /// Writes all questionnaire results to the questionnaireresults.csv file
        /// </summary>
        public void WriteResults()
        {
            if (string.IsNullOrEmpty(questionnaireResultsPath))
            {
                Debug.LogError("[EventLogger] Questionnaire results path not initialized. Call InitializeSceneLogging first.");
                return;
            }

            var date = DateTime.Now;
            using (StreamWriter streamWriter = new StreamWriter(questionnaireResultsPath, true))
            {
                streamWriter.WriteLine($"# Results written at {date.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss")}");

                foreach (KeyValuePair<string, string> kvp in resultList)
                {
                    var timestamp = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
                    streamWriter.WriteLine($"{timestamp}, {kvp.Key}, {kvp.Value}");
                }
            }

            Debug.Log($"[EventLogger] Questionnaire results saved to {questionnaireResultsPath}");
            resultList.Clear();
        }

        /// <summary>
        /// Logs a string to the appropriate results file based on log type
        /// - TLX/Mannequin -> questionnaireresults.csv
        /// - MathTask/NBackTask -> mentalload_results.csv
        /// </summary>
        /// <param name="logType">The type of log (TLX, Mannequin, MathTask, NBackTask)</param>
        /// <param name="message">The message to log</param>
        public void LogToFile(LogType logType, string message)
        {
            string path = GetPathForLogType(logType);

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"[EventLogger] Path not initialized for log type: {logType}. Call InitializeSceneLogging first.");
                return;
            }

            try
            {
                var timestamp = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
                using (StreamWriter streamWriter = new StreamWriter(path, true))
                {
                    streamWriter.WriteLine($"{timestamp}, {logType}, {message}");
                }
                Debug.Log($"[EventLogger] Logged to {logType}: {message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EventLogger] Failed to write to {logType} log: {e.Message}");
            }
        }

        /// <summary>
        /// Logs scene-specific events to sceneresults.csv (for scenario 1-4 scene events)
        /// </summary>
        /// <param name="eventType">Type of scene event</param>
        /// <param name="data">Event data</param>
        public void LogSceneEvent(string eventType, string data)
        {
            if (string.IsNullOrEmpty(sceneResultsPath))
            {
                Debug.LogError("[EventLogger] Scene results path not initialized. Call InitializeSceneLogging first.");
                return;
            }

            try
            {
                var timestamp = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
                using (StreamWriter streamWriter = new StreamWriter(sceneResultsPath, true))
                {
                    streamWriter.WriteLine($"{timestamp}, {eventType}, {data}");
                }
                Debug.Log($"[EventLogger] Logged scene event: {eventType}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EventLogger] Failed to write scene event: {e.Message}");
            }
        }

        /// <summary>
        /// Gets the file path for the specified log type.
        /// </summary>
        private string GetPathForLogType(LogType logType)
        {
            switch (logType)
            {
                case LogType.TLX:
                case LogType.Mannequin:
                    return questionnaireResultsPath;
                case LogType.MathTask:
                case LogType.NBackTask:
                    return mentalLoadResultsPath;
                default:
                    return null;
            }
        }

    }
}
