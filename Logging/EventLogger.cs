using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class EventLogger : MonoBehaviour
{
    private string tlxResultsPath, mathTaskResultsPath, nBackResultsPath;

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
        //string logpath = "D:/Studies/SBu/MA_LogaData/";
        //string logpath = "D:/MA Data/logdata/";
        string logpath = Application.persistentDataPath + "/Log/";
        // eventLogPath = logpath + date.ToString("dd-MM-yyyy_HH-mm-ss") + "-Events" + ".csv";
        tlxResultsPath = logpath + date.ToString("dd-MM-yyyy_HH-mm-ss") + "-Questionnaire" + ".csv";
        mathTaskResultsPath = logpath + date.ToString("dd-MM-yyyy_HH-mm-ss") + "-MathTask" + ".csv";
        nBackResultsPath = logpath + date.ToString("dd-MM-yyyy_HH-mm-ss") + "-NBack" + ".csv";


        StreamWriter streamWriterTLX = new StreamWriter(tlxResultsPath, true);
        streamWriterTLX.WriteLine("timestamp, item, result");
        streamWriterTLX.Close();

        StreamWriter streamWriterMathTask = new StreamWriter(mathTaskResultsPath, true);
        streamWriterMathTask.WriteLine("timestamp, act number");
        streamWriterMathTask.Close();

        StreamWriter streamWriterNBack = new StreamWriter(nBackResultsPath, true);
        streamWriterNBack.WriteLine("");
        streamWriterNBack.Close();
    }

    public void LogItem(string index, string value)
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
}
