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
        private string tlxResultsPath, mathTaskResultsPath, nBackResultsPath, mannequinResultsPath;

        private static EventLogger instance;
        Dictionary<string, string> resultList = new Dictionary<string, string>();


        // Static singleton property
        public static EventLogger Instance
        {
            // Here we use the ?? operator, to return 'instance' if 'instance' does not equal null
            // otherwise we assign instance to a new component and return that
            get { return instance ?? (instance = new GameObject("Singleton").AddComponent<EventLogger>()); }
        }

        // Start is called before the first frame update
        void Start()
        {
            var date = DateTime.Now;

            if (!Directory.Exists(Application.persistentDataPath + "/Log"))
            {
                Directory.CreateDirectory(Application.persistentDataPath + "/Log");
            }

            string logpath = Application.persistentDataPath + "/Log/";
            // eventLogPath = logpath + date.ToString("dd-MM-yyyy_HH-mm-ss") + "-Events" + ".csv";
            tlxResultsPath = logpath + date.ToString("dd-MM-yyyy_HH-mm-ss") + "-Questionnaire" + ".csv";
            mannequinResultsPath = logpath + date.ToString("dd-MM-yyyy_HH-mm-ss") + "-Questionnaire" + ".csv";
            mathTaskResultsPath = logpath + date.ToString("dd-MM-yyyy_HH-mm-ss") + "-MathTask" + ".csv";
            nBackResultsPath = logpath + date.ToString("dd-MM-yyyy_HH-mm-ss") + "-NBack" + ".csv";


            StreamWriter streamWriterTLX = new StreamWriter(tlxResultsPath, true);
            streamWriterTLX.WriteLine("timestamp, item, result");
            streamWriterTLX.Close();

            StreamWriter streamwriterMannequin = new StreamWriter(mannequinResultsPath, true);
            streamwriterMannequin.WriteLine("timestamp, item, result");
            streamwriterMannequin.Close();

            StreamWriter streamWriterMathTask = new StreamWriter(mathTaskResultsPath, true);
            streamWriterMathTask.WriteLine("timestamp, act number");
            streamWriterMathTask.Close();

            StreamWriter streamWriterNBack = new StreamWriter(nBackResultsPath, true);
            streamWriterNBack.WriteLine("timestamp, elapsedTimeMs, remainingTimeMs, userChoice, isCorrect");
            streamWriterNBack.Close();
        }

        public void LogQuestionnaireItem(string index, string value)
        {

            if (resultList.ContainsKey(index))
                resultList[index] = value;
            else
                resultList.Add(index, value);
        }

        public void WriteResults()
        {
            //Log the last item

            var date = DateTime.Now;
            StreamWriter streamWriter = new StreamWriter(tlxResultsPath, true);
            streamWriter.WriteLine("This file was created at " + date.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"));

            var results = "";
            foreach (KeyValuePair<string, string> kvp in resultList)
            {
                results = kvp.Key + "," + kvp.Value + "\n";
                streamWriter.WriteLine(results);
            }


            streamWriter.Close();
            resultList.Clear();
            Debug.Log("Results saved to " + tlxResultsPath);
        }

        /// <summary>
        /// Logs a string to the specified log file.
        /// </summary>
        /// <param name="logType">The type of log (TLX, Mannequin, MathTask, NBackTask)</param>
        /// <param name="message">The message to log</param>
        public void LogToFile(LogType logType, string message)
        {
            string path = GetPathForLogType(logType);

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"[EventLogger] Path not initialized for log type: {logType}");
                return;
            }

            try
            {
                var timestamp = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss");
                StreamWriter streamWriter = new StreamWriter(path, true);
                streamWriter.WriteLine($"{timestamp}, {message}");
                streamWriter.Close();
                Debug.Log($"[EventLogger] Logged to {logType}: {message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[EventLogger] Failed to write to {logType} log: {e.Message}");
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
                    return tlxResultsPath;
                case LogType.Mannequin:
                    return mannequinResultsPath;
                case LogType.MathTask:
                    return mathTaskResultsPath;
                case LogType.NBackTask:
                    return nBackResultsPath;
                default:
                    return null;
            }
        }

    }
}
