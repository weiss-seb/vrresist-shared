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
    /// </summary>
    public class StudyLogger : MonoBehaviour
    {
        #region Singleton Implementation

        private static StudyLogger _instance;

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
            // Already handled in Awake for singleton
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

        #region Logging Methods

        /// <summary>
        /// Write a timestamped line to the current scene's log file
        /// </summary>
        /// <param name="line">The content to log</param>
        public void WriteLineToLog(string line)
        {
            StreamWriter writer = GetStreamWriter();
            if (writer != null)
            {
                writer.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ";" + line);
                writer.Flush();
                writer.Close();
            }
        }

        /// <summary>
        /// Get a StreamWriter for the current scene's log file
        /// Creates the file and directory structure if they don't exist
        /// </summary>
        /// <returns>StreamWriter for the log file, or null if creation failed</returns>
        public StreamWriter GetStreamWriter()
        {
            Debug.Log("[StudyLogger] Preparing log file for scene: " + SceneManager.GetActiveScene().name);
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

                    switch (SceneManager.GetActiveScene().buildIndex)
                    {
                        case 0: //DISCLAIMER
                            writer.WriteLine("Device Language set to : " + Application.systemLanguage);
                            break;
                        case 2: // PSQ PRE
                            writer.WriteLine("Answers of PRE-STUDY PSQ!");
                            break;
                        case 3:
                            writer.WriteLine("TaskFileName" + ";" + "SubTaskIndex" + ";" + "StudyType" + ";" + "TaskType" + ";" + "TaskResult" + ";" + "SecondsGiven" + ";" + "SecondsUsed" + ";" + "IdleTime");
                            break;
                        case 4: // PSQ MID 
                            writer.WriteLine("Answers of MID-STUDY PSQ!"); //aftertraining 
                            break;
                        case 5: //PSQ POST
                            writer.WriteLine("Answers of Mid-STUDY PSQ!");
                            break;
                        case 6: //NASAT TLX
                            writer.WriteLine("Answers of NASA TLX!");
                            break;
                        case 7: //IPQ AND DEMOGRAPHICS
                            writer.WriteLine("Answers of IPQ AND DEMOGRAPHICS!");
                            break;
                        default:
                            break;
                    }

                    writer.Flush();
                    writer.Close();
                }
                return new StreamWriter(path + LogFileName, true);
            }
            catch (IOException e)
            {
                Debug.LogError("[StudyLogger] CLOSE THE LOG FILE BEFORE STARTING STUDY");
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
        /// Get the current log file path for debugging purposes
        /// </summary>
        public string GetCurrentLogPath()
        {
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
