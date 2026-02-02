using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using OVGU.VAR.VRResist;

/// <summary>
/// Enhanced Study Task Manager - Updated for Step 7
/// Now integrates with TaskDisplaySystem for better task management
/// Maintains backward compatibility with existing message system
/// </summary>
public class StudyTaskManager : MonoBehaviour
{
    [Header("Task System Integration")]
    [SerializeField] TaskDisplaySystem taskDisplaySystem;


    [Header("Advanced Task Management")]
    [SerializeField] bool useAdvancedTaskSystem = true;
    [SerializeField] StudyTaskParser taskParser;

    // Legacy UI references (kept for backward compatibility)
    private TMP_Text taskText;
    private TMP_Text taskTitle;
    public GameObject taskCanvas;

    // List of tasks (can be BaseTask or StressorStudyTask)
    public List<BaseTask> taskList = new List<BaseTask>();


    // Task queue management
    private Queue<BaseTask> pendingTasks = new Queue<BaseTask>();
    private Queue<StressorStudyTask> pendingStressorTasks = new Queue<StressorStudyTask>();

    void Start()
    {
        InitializeTaskManager();
    }

    /// <summary>
    /// Initialize the task manager system
    /// </summary>
    void InitializeTaskManager()
    {
        Debug.Log("[StudyTaskManager] Initializing enhanced task manager...");

        // Find TaskDisplaySystem if not assigned
        if (taskDisplaySystem == null)
            taskDisplaySystem = FindObjectOfType<TaskDisplaySystem>();

        // Legacy UI setup (for backward compatibility)
        if (taskCanvas != null)
        {
            taskTitle = taskCanvas.transform.Find("TaskTitle")?.GetComponent<TMP_Text>();
            taskText = taskCanvas.transform.Find("TaskText")?.GetComponent<TMP_Text>();
        }

        // Find task parser if not assigned
        if (taskParser == null)
            taskParser = FindObjectOfType<StudyTaskParser>();

        // Initialize with no tasks
        SetTask("noTasks");

        Debug.Log("[StudyTaskManager] Task manager initialized!");
    }

    #region Message Handling (Legacy Support)

    /// <summary>
    /// Receive task message from MessageHandler (legacy support)
    /// </summary>
    public void receiveTaskMessage(EventMessage msg)
    {
        if (msg.content != null && msg.content.Length > 0)
        {
            SetTask(msg.content[0]);
        }
    }


    /// <summary>
    /// Set task by name (legacy support + enhanced functionality)
    /// </summary>
    public void setTask(string task)
    {
        SetTask(task);
    }

    /// <summary>
    /// Enhanced SetTask method - Math and NBack tasks only
    /// </summary>
    public void SetTask(string taskName)
    {
        Debug.Log($"[StudyTaskManager] Setting task: {taskName}");

        // Handle special task types (math, nback, noTasks)
        HandleSpecialTaskTypes(taskName);
    }

    /// <summary>
    /// Display no tasks message
    /// </summary>
    void DisplayNoTasks()
    {
        if (taskTitle != null)
            taskTitle.text = "Nächste Aufgabe:";

        if (taskText != null)
            taskText.text = " Zurzeit keine weiteren Aufgaben";
    }

    public string[] GetSerializableTaskList()
    {
        // Convert complex task objects to simple strings
        List<string> taskNames = new List<string>();

        if (taskList != null)
        {
            foreach (var task in taskList)
            {
                if (task != null)
                {
                    // Extract just the task name or identifier
                    taskNames.Add(task.ToString()); // or task.name, task.id, etc.
                }
            }
        }

        // Add default tasks if none exist
        if (taskNames.Count == 0)
        {
            taskNames.AddRange(new string[] { "math_task", "nback_task", "noTasks" });
        }

        return taskNames.ToArray();
    }

    /// <summary>
    /// Handle special task types (math, n-back, etc.)
    /// </summary>
    void HandleSpecialTaskTypes(string taskName)
    {
        switch (taskName.ToLower())
        {
            case "math_task":
            case "mathtask":
                ShowMathTask();
                break;
            case "nback_task":
            case "nbacktask":
                ShowNBackTask();
                break;
            case "hide_math_task":
            case "hidemathtask":
                HideMathTask();
                break;
            case "hide_nback_task":
            case "hidenbacktask":
                HideNBackTask();
                break;
            case "notasks":
            case "no_tasks":
                DisplayNoTasks();
                break;
            default:
                // For any unknown task, display no tasks
                DisplayNoTasks();
                break;
        }
    }

    #endregion

    #region Advanced Task Management

    /// <summary>
    /// Start a BaseTask with full functionality
    /// </summary>
    public void StartAdvancedTask(BaseTask task)
    {
        if (taskDisplaySystem != null)
        {
            taskDisplaySystem.StartTask(task);
        }
        else
        {
            Debug.LogWarning("[StudyTaskManager] TaskDisplaySystem not available for advanced task!");
        }
    }

    /// <summary>
    /// Start a StressorStudyTask
    /// </summary>
    public void StartStressorTask(StressorStudyTask task)
    {
        if (taskDisplaySystem != null)
        {
            taskDisplaySystem.StartStressorTask(task);
        }
        else
        {
            Debug.LogWarning("[StudyTaskManager] TaskDisplaySystem not available for stressor task!");
        }
    }

    /// <summary>
    /// Add task to queue
    /// </summary>
    public void QueueTask(BaseTask task)
    {
        pendingTasks.Enqueue(task);

        if (taskDisplaySystem != null)
        {
            taskDisplaySystem.AddTaskToQueue(task);
        }

        Debug.Log($"[StudyTaskManager] Queued task: {task.GetTaskDescription()}");
    }

    /// <summary>
    /// Add stressor task to queue
    /// </summary>
    public void QueueStressorTask(StressorStudyTask task)
    {
        pendingStressorTasks.Enqueue(task);

        if (taskDisplaySystem != null)
        {
            taskDisplaySystem.AddStressorTaskToQueue(task);
        }

        Debug.Log($"[StudyTaskManager] Queued stressor task: {GetStressorTaskDescription(task)}");
    }

    /// <summary>
    /// Process next task in queue
    /// </summary>
    public void ProcessNextTask()
    {
        if (pendingTasks.Count > 0)
        {
            BaseTask nextTask = pendingTasks.Dequeue();
            StartAdvancedTask(nextTask);
        }
        else if (pendingStressorTasks.Count > 0)
        {
            StressorStudyTask nextTask = pendingStressorTasks.Dequeue();
            StartStressorTask(nextTask);
        }
        else
        {
            SetTask("noTasks");
        }
    }

    /// <summary>
    /// Complete current task with status
    /// </summary>
    public void CompleteCurrentTask(ENUM_TaskStatus status)
    {
        if (taskDisplaySystem != null)
        {
            if (taskDisplaySystem.GetCurrentTask() != null)
            {
                taskDisplaySystem.CompleteTask(status);
            }
            else if (taskDisplaySystem.GetCurrentStressorTask() != null)
            {
                taskDisplaySystem.CompleteStressorTask(status);
            }
        }

        // Auto-process next task after a delay
        StartCoroutine(ProcessNextTaskAfterDelay(2f));
    }

    /// <summary>
    /// Process next task after delay
    /// </summary>
    IEnumerator ProcessNextTaskAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ProcessNextTask();
    }

    #endregion

    #region Task Creation Helpers

    /// <summary>
    /// Create a BaseTask from parameters
    /// </summary>
    public BaseTask CreateTask(TaskType taskType, StressorType stressorType, float timeLimit = 0f)
    {
        return new BaseTask
        {
            Tasktype = taskType,
            Studytype = stressorType,
            SecondsGiven = timeLimit,
            Status = ENUM_TaskStatus.Pending
        };
    }

    /// <summary>
    /// Create a StressorStudyTask from parameters
    /// </summary>
    public StressorStudyTask CreateStressorTask(TaskType taskType, StressorType stressorType, float timeLimit = 0f)
    {
        return new StressorStudyTask(stressorType, taskType, ENUM_TaskStatus.Pending, timeLimit, 0f);
    }

    /// <summary>
    /// Load tasks from StudyTaskParser
    /// </summary>
    public void LoadTasksFromParser()
    {
        if (taskParser != null)
        {
            taskParser.ReadTaskFile();
            var tasks = taskParser.GetTasks();

            foreach (var task in tasks)
            {
                QueueStressorTask(task);
            }

            Debug.Log($"[StudyTaskManager] Loaded {tasks.Count} tasks from parser");
        }
        else
        {
            Debug.LogWarning("[StudyTaskManager] StudyTaskParser not available!");
        }
    }

    #endregion

    #region Specific Task Types

    /// <summary>
    /// Show math task
    /// </summary>
    public void ShowMathTask(string difficulty = "medium", string timeLimit = "60")
    {
        if (taskDisplaySystem != null)
        {
            taskDisplaySystem.ShowMathTask(difficulty, timeLimit);
        }
        else
        {
            var mathTask = CreateTask(TaskType.MathTask, StressorType.WithTimePressure, float.Parse(timeLimit));
            StartAdvancedTask(mathTask);
        }
    }

    /// <summary>
    /// Show N-back task
    /// </summary>
    public void ShowNBackTask(string nLevel = "2", string timeLimit = "60")
    {
        if (taskDisplaySystem != null)
        {
            taskDisplaySystem.ShowNBackTask(nLevel, timeLimit);
        }
        else
        {
            var nbackTask = CreateTask(TaskType.NBackTask, StressorType.WithTimePressure, float.Parse(timeLimit));
            StartAdvancedTask(nbackTask);
        }
    }

    /// <summary>
    /// Hide math task
    /// </summary>
    public void HideMathTask()
    {
        if (taskDisplaySystem != null && taskDisplaySystem.IsTaskActive())
        {
            taskDisplaySystem.CancelCurrentTask();
        }
        SetTask("noTasks");
    }

    /// <summary>
    /// Hide N-back task
    /// </summary>
    public void HideNBackTask()
    {
        if (taskDisplaySystem != null && taskDisplaySystem.IsTaskActive())
        {
            taskDisplaySystem.CancelCurrentTask();
        }
        SetTask("noTasks");
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Get stressor task description
    /// </summary>
    string GetStressorTaskDescription(StressorStudyTask task)
    {
        return $"Task: {task.StressorType}, {task.Tasktype}, time given: {task.SecondsGiven}";
    }

    /// <summary>
    /// Check if task system is active
    /// </summary>
    public bool IsTaskActive()
    {
        return taskDisplaySystem != null && taskDisplaySystem.IsTaskActive();
    }

    /// <summary>
    /// Get current task count in queue
    /// </summary>
    public int GetQueuedTaskCount()
    {
        return pendingTasks.Count + pendingStressorTasks.Count;
    }

    /// <summary>
    /// Clear all queued tasks
    /// </summary>
    public void ClearTaskQueue()
    {
        pendingTasks.Clear();
        pendingStressorTasks.Clear();

        if (taskDisplaySystem != null)
        {
            taskDisplaySystem.ClearTaskQueue();
        }
    }

    #endregion

    #region Public API


    /// <summary>
    /// Set task display system reference
    /// </summary>
    public void SetTaskDisplaySystem(TaskDisplaySystem displaySystem)
    {
        taskDisplaySystem = displaySystem;
    }

    /// <summary>
    /// Enable/disable advanced task system
    /// </summary>
    public void SetAdvancedTaskSystem(bool enabled)
    {
        useAdvancedTaskSystem = enabled;
    }

    #endregion
}
