using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;


public enum StressorType
{
    WithTimePressure, WithoutTimePressure, WithInterruption, WithoutInterruption, Emotional, Corrupted
}
public enum TaskType
{
    MathTask, NBackTask, Corrupted
}

public struct StressorStudyTask
{
    public StressorType StressorType;
    public TaskType Tasktype;
    public ENUM_TaskStatus TaskStatus;

    public float SecondsGiven;
    public float SecondsUsed;

    /// Constructor for StressorStudyTask struct
    public StressorStudyTask(StressorType stressorType, TaskType taskType, ENUM_TaskStatus taskStatus, float secondsGiven, float secondsUsed)
    {
        StressorType = stressorType;
        Tasktype = taskType;
        TaskStatus = taskStatus;
        SecondsGiven = secondsGiven;
        SecondsUsed = secondsUsed;
    }
}

/// <summary>
/// StudyTasksInterpreter reads instructions in textfiles and parses them. The information is saved in a struct called Task.
/// Instructions for tasks need to be separated by a ';' in this text file.
/// The given time for a task needs a ',' in front of it and a ';' at the end. Time in milliseconds. No decimalpoint allowed.
/// </summary>
public class StudyTaskParser : MonoBehaviour
{

    TextAsset TaskDataAsText;
    [SerializeField] TextAsset trainingData;
    [SerializeField] TextAsset[] interruptions;
    [SerializeField] TextAsset[] timePressure;

    [Header("Keywords for Parsing")]
    [SerializeField] private string keywordWithTimePressure;
    [SerializeField] private string keywordWithoutTimePressure;
    [SerializeField] private string keywordWithInterruption;
    [SerializeField] private string keywordWithoutInterruption;

    private List<StressorStudyTask> tasks = new List<StressorStudyTask>();
    private string nameOfSelectedFile;

    // Use this for initialization
    void Start()
    {

        if (PlayerPrefs.HasKey("TrainingFinished") && PlayerPrefs.GetInt("TrainingFinished") == 0)
        {
            LoadTaskFile("training");
        }
        else
        {
            LoadTaskFile("study");
        }
    }

    private void LoadTaskFile(string status)
    {
        switch (status)
        {
            case "training":
                // string trainingFileFolder = "/ICU_taskfiles/Training/";
                TaskDataAsText = trainingData;
                // Addressables.LoadAssetAsync<TextAsset>("Assets/Resources_moved/ICU_taskfiles/Training/training.txt").Completed += OnAdressableLoaded;
                break;

            case "study":

                string taskFileFolder = string.Empty;

                switch (GetStressorType())
                {
                    case 0:
                        TaskDataAsText = interruptions[UnityEngine.Random.Range(0, 2)];
                        break;
                    case 1:
                        TaskDataAsText = timePressure[UnityEngine.Random.Range(0, 2)];
                        break;
                    default: break;
                }

                break;

            default: break;
        }

    }

    private void OnAdressableLoaded(AsyncOperationHandle<TextAsset> obj)
    {
        TaskDataAsText = obj.Result;
    }

    public string GetTaskName()
    {
        return nameOfSelectedFile;
    }

    private int GetStressorType()
    {
        Debug.Log("Returning stressor type:" + PlayerPrefs.GetInt("StressorType"));
        return PlayerPrefs.GetInt("StressorType");

    }
    public List<StressorStudyTask> GetTasks()
    {
        return tasks;
    }

    public void ReadTaskFile()
    {
        tasks.Clear();

        string inputtext = TaskDataAsText.text;
        int commaindex = inputtext.IndexOf(";");
        int counter = 0;
        while (commaindex != -1)
        {
            StressorStudyTask t = ParseLine(inputtext.Substring(0, commaindex + 1));
            if (t.Tasktype != TaskType.Corrupted && t.StressorType != StressorType.Corrupted)
                tasks.Add(t);
            inputtext = inputtext.Substring(commaindex + 1);

            commaindex = inputtext.IndexOf(";");
            counter += 1;
            if (counter >= 5000)
                break;
        }


        foreach (StressorStudyTask t in tasks)
        {
            Debug.Log(GetTaskDescription(t));
        }
    }

    public string GetTaskDescription(StressorStudyTask t)
    {
        return "Task: " + t.StressorType + "," + t.Tasktype + ", time given: " + t.SecondsGiven;
    }

    public StressorStudyTask ParseLine(string line)
    {
        StressorType stype = StressorType.Corrupted;
        if (line.Contains(keywordWithTimePressure))
            stype = StressorType.WithTimePressure;
        else if (line.Contains(keywordWithoutTimePressure))
            stype = StressorType.WithoutTimePressure;
        else if (line.Contains(keywordWithInterruption))
            stype = StressorType.WithInterruption;
        else if (line.Contains(keywordWithoutInterruption))
            stype = StressorType.WithoutInterruption;

        TaskType ttype = TaskType.Corrupted;
        if (line.Contains("math") || line.Contains("Math") || line.Contains("MATH"))
            ttype = TaskType.MathTask;
        else if (line.Contains("nback") || line.Contains("NBack") || line.Contains("NBACK") || line.Contains("n-back"))
            ttype = TaskType.NBackTask;


        float secondsgiven = 0f;
        int indexOfSecondsGiven = line.LastIndexOf(',');
        if (indexOfSecondsGiven != -1)
        {
            indexOfSecondsGiven += 1;
            int parsedseconds = 0;
            if (int.TryParse(line.Substring(indexOfSecondsGiven, (line.LastIndexOf(';') - indexOfSecondsGiven)), out parsedseconds))
            {
                secondsgiven = (float)parsedseconds;
                secondsgiven = secondsgiven / 1000;
            }
        }
        if (stype == StressorType.WithTimePressure && secondsgiven <= 0f)
            stype = StressorType.Corrupted;

        return new StressorStudyTask(stype, ttype, ENUM_TaskStatus.Pending, secondsgiven, 0f);
    }
}
