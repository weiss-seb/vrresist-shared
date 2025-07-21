using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Enhanced Task Display System for VR Study
    /// Handles both simple task blocks and complex StressorStudyTask display
    /// Provides visual feedback, progress tracking, and timer functionality
    /// </summary>
    public class TaskDisplaySystem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] GameObject taskCanvas;
        [SerializeField] TMP_Text taskTitle;
        [SerializeField] TMP_Text taskDescription;
        [SerializeField] TMP_Text taskTimer;
        [SerializeField] TMP_Text taskStatus;
        [SerializeField] Image taskProgressBar;
        [SerializeField] Image taskStatusIcon;

        [Header("Task List Display")]
        [SerializeField] GameObject taskListPanel;
        [SerializeField] Transform taskListContainer;
        [SerializeField] GameObject taskItemPrefab;
        [SerializeField] ScrollRect taskListScrollRect;

        [Header("Visual Feedback")]
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color activeColor = Color.yellow;
        [SerializeField] Color successColor = Color.green;
        [SerializeField] Color failureColor = Color.red;
        [SerializeField] Color warningColor = Color.cyan;

        [Header("Timer Settings")]
        [SerializeField] bool showTimer = true;
        [SerializeField] bool showProgressBar = true;
        [SerializeField] float warningTimeThreshold = 10f; // Show warning when less than 10 seconds left

        [Header("Debug Settings")]
        [SerializeField] bool enableDetailedLogging = true;

        // Current task state
        private BaseTask currentTask;
        private StressorStudyTask currentStressorTask;
        private string currentTaskBlock;
        private float taskStartTime;
        private bool isTaskActive = false;
        private bool isTimerRunning = false;

        // Task list management
        private List<BaseTask> taskQueue = new List<BaseTask>();
        private List<StressorStudyTask> stressorTaskQueue = new List<StressorStudyTask>();
        private List<GameObject> taskListItems = new List<GameObject>();

        void Start()
        {
            InitializeTaskDisplay();
        }

        void Update()
        {
            if (isTimerRunning && currentTask != null)
            {
                UpdateTaskTimer();
            }
            else if (isTimerRunning && currentStressorTask.TaskStatus == ENUM_TaskStatus.Active)
            {
                UpdateStressorTaskTimer();
            }
        }

        /// <summary>
        /// Initialize the task display system
        /// </summary>
        void InitializeTaskDisplay()
        {
            Debug.Log("[TaskDisplaySystem] Initializing task display system...");

            // Find UI components if not assigned
            if (taskCanvas == null)
                taskCanvas = GameObject.Find("TaskCanvas");

            if (taskCanvas != null)
            {
                if (taskTitle == null)
                    taskTitle = taskCanvas.transform.Find("TaskTitle")?.GetComponent<TMP_Text>();
                if (taskDescription == null)
                    taskDescription = taskCanvas.transform.Find("TaskText")?.GetComponent<TMP_Text>();
                if (taskTimer == null)
                    taskTimer = taskCanvas.transform.Find("TaskTimer")?.GetComponent<TMP_Text>();
                if (taskStatus == null)
                    taskStatus = taskCanvas.transform.Find("TaskStatus")?.GetComponent<TMP_Text>();
                if (taskProgressBar == null)
                    taskProgressBar = taskCanvas.transform.Find("TaskProgressBar")?.GetComponent<Image>();
                if (taskStatusIcon == null)
                    taskStatusIcon = taskCanvas.transform.Find("TaskStatusIcon")?.GetComponent<Image>();
            }

            // Initialize UI elements
            if (taskTitle != null)
                taskTitle.text = "Nächste Aufgabe:";

            if (taskTimer != null)
                taskTimer.gameObject.SetActive(showTimer);

            if (taskProgressBar != null)
                taskProgressBar.gameObject.SetActive(showProgressBar);

            Debug.Log("[TaskDisplaySystem] Task display system initialized!");
        }

        #region Task Block System (Simple Tasks)



        /// <summary>
        /// Display a task block
        /// </summary>
        void DisplayTaskBlock(TaskBlockDefinition taskBlock)
        {
            if (taskTitle != null)
                taskTitle.text = taskBlock.title;

            if (taskDescription != null)
            {
                if (taskBlock.tasks.Length == 0)
                {
                    taskDescription.text = taskBlock.description;
                }
                else
                {
                    string taskText = "";
                    foreach (string task in taskBlock.tasks)
                    {
                        taskText += " - " + task + "\n";
                    }
                    taskDescription.text = taskText.TrimEnd('\n');
                }
            }

            // Hide timer and progress for simple task blocks
            if (taskTimer != null)
                taskTimer.gameObject.SetActive(false);
            if (taskProgressBar != null)
                taskProgressBar.gameObject.SetActive(false);

            SetTaskStatusColor(normalColor);
            isTaskActive = false;
            isTimerRunning = false;
        }

        #endregion

        #region Advanced Task System (BaseTask)

        /// <summary>
        /// Start a BaseTask with full functionality
        /// </summary>
        public void StartTask(BaseTask task)
        {
            currentTask = task;
            currentTask.Status = ENUM_TaskStatus.Active;
            taskStartTime = Time.time;
            isTaskActive = true;
            isTimerRunning = true;

            DisplayAdvancedTask(task);

            if (enableDetailedLogging)
                Debug.Log($"[TaskDisplaySystem] Started task: {task.GetTaskDescription()}");
        }

        /// <summary>
        /// Display an advanced BaseTask
        /// </summary>
        void DisplayAdvancedTask(BaseTask task)
        {
            if (taskTitle != null)
                taskTitle.text = "Aktuelle Aufgabe:";

            if (taskDescription != null)
                taskDescription.text = GetTaskTypeDescription(task.Tasktype);

            if (taskStatus != null)
                taskStatus.text = GetStatusText(task.Status);

            // Show timer and progress bar for advanced tasks
            if (taskTimer != null && showTimer)
            {
                taskTimer.gameObject.SetActive(true);
                UpdateTaskTimerDisplay(task.SecondsGiven, 0);
            }

            if (taskProgressBar != null && showProgressBar)
            {
                taskProgressBar.gameObject.SetActive(true);
                taskProgressBar.fillAmount = 0f;
            }

            SetTaskStatusColor(activeColor);
        }

        /// <summary>
        /// Update task timer for BaseTask
        /// </summary>
        void UpdateTaskTimer()
        {
            if (currentTask == null) return;

            float elapsedTime = Time.time - taskStartTime;
            currentTask.SecondsUsed = elapsedTime;

            float remainingTime = currentTask.SecondsGiven - elapsedTime;

            if (remainingTime <= 0)
            {
                // Task time expired
                CompleteTask(ENUM_TaskStatus.OutOfTime);
                return;
            }

            // Update UI
            UpdateTaskTimerDisplay(currentTask.SecondsGiven, elapsedTime);

            // Update progress bar
            if (taskProgressBar != null && showProgressBar)
            {
                float progress = elapsedTime / currentTask.SecondsGiven;
                taskProgressBar.fillAmount = progress;
            }

            // Warning color when time is running out
            if (remainingTime <= warningTimeThreshold)
            {
                SetTaskStatusColor(warningColor);
            }
        }

        /// <summary>
        /// Complete the current task with a status
        /// </summary>
        public void CompleteTask(ENUM_TaskStatus status)
        {
            if (currentTask == null) return;

            currentTask.Status = status;
            isTaskActive = false;
            isTimerRunning = false;

            // Update UI
            if (taskStatus != null)
                taskStatus.text = GetStatusText(status);

            Color statusColor = status == ENUM_TaskStatus.Success ? successColor : failureColor;
            SetTaskStatusColor(statusColor);

            if (enableDetailedLogging)
                Debug.Log($"[TaskDisplaySystem] Task completed with status: {status}");

            // Auto-hide after a delay
            StartCoroutine(HideTaskAfterDelay(3f));
        }

        #endregion

        #region Stressor Task System

        /// <summary>
        /// Start a StressorStudyTask
        /// </summary>
        public void StartStressorTask(StressorStudyTask task)
        {
            currentStressorTask = task;
            currentStressorTask.TaskStatus = ENUM_TaskStatus.Active;
            taskStartTime = Time.time;
            isTaskActive = true;
            isTimerRunning = true;

            DisplayStressorTask(task);

            if (enableDetailedLogging)
                Debug.Log($"[TaskDisplaySystem] Started stressor task: {GetStressorTaskDescription(task)}");
        }

        /// <summary>
        /// Display a StressorStudyTask
        /// </summary>
        void DisplayStressorTask(StressorStudyTask task)
        {
            if (taskTitle != null)
                taskTitle.text = GetStressorTypeTitle(task.StressorType);

            if (taskDescription != null)
                taskDescription.text = GetTaskTypeDescription(task.Tasktype);

            if (taskStatus != null)
                taskStatus.text = GetStatusText(task.TaskStatus);

            // Show timer for timed tasks
            if (taskTimer != null && showTimer && task.SecondsGiven > 0)
            {
                taskTimer.gameObject.SetActive(true);
                UpdateTaskTimerDisplay(task.SecondsGiven, 0);
            }

            if (taskProgressBar != null && showProgressBar && task.SecondsGiven > 0)
            {
                taskProgressBar.gameObject.SetActive(true);
                taskProgressBar.fillAmount = 0f;
            }

            SetTaskStatusColor(activeColor);
        }

        /// <summary>
        /// Update timer for StressorStudyTask
        /// </summary>
        void UpdateStressorTaskTimer()
        {
            if (currentStressorTask.TaskStatus != ENUM_TaskStatus.Active) return;

            float elapsedTime = Time.time - taskStartTime;
            currentStressorTask.SecondsUsed = elapsedTime;

            if (currentStressorTask.SecondsGiven > 0)
            {
                float remainingTime = currentStressorTask.SecondsGiven - elapsedTime;

                if (remainingTime <= 0)
                {
                    CompleteStressorTask(ENUM_TaskStatus.OutOfTime);
                    return;
                }

                UpdateTaskTimerDisplay(currentStressorTask.SecondsGiven, elapsedTime);

                if (taskProgressBar != null && showProgressBar)
                {
                    float progress = elapsedTime / currentStressorTask.SecondsGiven;
                    taskProgressBar.fillAmount = progress;
                }

                if (remainingTime <= warningTimeThreshold)
                {
                    SetTaskStatusColor(warningColor);
                }
            }
        }

        /// <summary>
        /// Complete the current stressor task
        /// </summary>
        public void CompleteStressorTask(ENUM_TaskStatus status)
        {
            currentStressorTask.TaskStatus = status;
            isTaskActive = false;
            isTimerRunning = false;

            if (taskStatus != null)
                taskStatus.text = GetStatusText(status);

            Color statusColor = status == ENUM_TaskStatus.Success ? successColor : failureColor;
            SetTaskStatusColor(statusColor);

            if (enableDetailedLogging)
                Debug.Log($"[TaskDisplaySystem] Stressor task completed with status: {status}");

            StartCoroutine(HideTaskAfterDelay(3f));
        }

        #endregion

        #region Task Queue Management

        /// <summary>
        /// Add task to queue
        /// </summary>
        public void AddTaskToQueue(BaseTask task)
        {
            taskQueue.Add(task);
            UpdateTaskListDisplay();
        }

        /// <summary>
        /// Add stressor task to queue
        /// </summary>
        public void AddStressorTaskToQueue(StressorStudyTask task)
        {
            stressorTaskQueue.Add(task);
            UpdateTaskListDisplay();
        }

        /// <summary>
        /// Get next task from queue
        /// </summary>
        public BaseTask GetNextTask()
        {
            if (taskQueue.Count > 0)
            {
                BaseTask nextTask = taskQueue[0];
                taskQueue.RemoveAt(0);
                UpdateTaskListDisplay();
                return nextTask;
            }
            return null;
        }

        /// <summary>
        /// Get next stressor task from queue
        /// </summary>
        public StressorStudyTask? GetNextStressorTask()
        {
            if (stressorTaskQueue.Count > 0)
            {
                StressorStudyTask nextTask = stressorTaskQueue[0];
                stressorTaskQueue.RemoveAt(0);
                UpdateTaskListDisplay();
                return nextTask;
            }
            return null;
        }

        /// <summary>
        /// Update the task list display
        /// </summary>
        void UpdateTaskListDisplay()
        {
            if (taskListContainer == null) return;

            // Clear existing items
            foreach (GameObject item in taskListItems)
            {
                if (item != null)
                    DestroyImmediate(item);
            }
            taskListItems.Clear();

            // Add BaseTask items
            foreach (BaseTask task in taskQueue)
            {
                CreateTaskListItem(task.GetTaskDescription(), GetStatusColor(task.Status));
            }

            // Add StressorStudyTask items
            foreach (StressorStudyTask task in stressorTaskQueue)
            {
                CreateTaskListItem(GetStressorTaskDescription(task), GetStatusColor(task.TaskStatus));
            }
        }

        /// <summary>
        /// Create a task list item
        /// </summary>
        void CreateTaskListItem(string description, Color statusColor)
        {
            if (taskItemPrefab == null) return;

            GameObject item = Instantiate(taskItemPrefab, taskListContainer);
            TMP_Text itemText = item.GetComponentInChildren<TMP_Text>();
            if (itemText != null)
            {
                itemText.text = description;
                itemText.color = statusColor;
            }

            taskListItems.Add(item);
        }

        #endregion

        #region Message Handling (Integration with existing system)


        /// <summary>
        /// Handle math task message
        /// </summary>
        public void ShowMathTask(string difficulty = "medium", string timeLimit = "60")
        {
            var mathTask = new BaseTask
            {
                Tasktype = TaskType.MathTask,
                Studytype = StressorType.WithTimePressure,
                SecondsGiven = float.Parse(timeLimit),
                Status = ENUM_TaskStatus.Pending
            };

            StartTask(mathTask);
        }

        /// <summary>
        /// Handle N-back task message
        /// </summary>
        public void ShowNBackTask(string nLevel = "2", string timeLimit = "60")
        {
            var nbackTask = new BaseTask
            {
                Tasktype = TaskType.NBackTask,
                Studytype = StressorType.WithTimePressure,
                SecondsGiven = float.Parse(timeLimit),
                Status = ENUM_TaskStatus.Pending
            };

            StartTask(nbackTask);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Update timer display
        /// </summary>
        void UpdateTaskTimerDisplay(float totalTime, float elapsedTime)
        {
            if (taskTimer == null) return;

            float remainingTime = totalTime - elapsedTime;
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);

            taskTimer.text = $"{minutes:00}:{seconds:00}";
        }

        /// <summary>
        /// Set task status color
        /// </summary>
        void SetTaskStatusColor(Color color)
        {
            if (taskStatusIcon != null)
                taskStatusIcon.color = color;
            if (taskTitle != null)
                taskTitle.color = color;
        }

        /// <summary>
        /// Get status color
        /// </summary>
        Color GetStatusColor(ENUM_TaskStatus status)
        {
            switch (status)
            {
                case ENUM_TaskStatus.Active: return activeColor;
                case ENUM_TaskStatus.Success: return successColor;
                case ENUM_TaskStatus.OutOfTime:
                case ENUM_TaskStatus.WrongExecution: return failureColor;
                case ENUM_TaskStatus.Pending: return normalColor;
                default: return normalColor;
            }
        }

        /// <summary>
        /// Get status text
        /// </summary>
        string GetStatusText(ENUM_TaskStatus status)
        {
            switch (status)
            {
                case ENUM_TaskStatus.Active: return "Aktiv";
                case ENUM_TaskStatus.Success: return "Erfolgreich";
                case ENUM_TaskStatus.OutOfTime: return "Zeit abgelaufen";
                case ENUM_TaskStatus.WrongExecution: return "Falsch ausgeführt";
                case ENUM_TaskStatus.Pending: return "Wartend";
                case ENUM_TaskStatus.Cancelled: return "Abgebrochen";
                default: return "Unbekannt";
            }
        }

        /// <summary>
        /// Get task type description in German
        /// </summary>
        string GetTaskTypeDescription(TaskType taskType)
        {
            switch (taskType)
            {
                case TaskType.MathTask: return "Mathematische Aufgabe";
                case TaskType.NBackTask: return "N-Back Aufgabe";
                default: return "Unbekannte Aufgabe";
            }
        }

        /// <summary>
        /// Get stressor type title
        /// </summary>
        string GetStressorTypeTitle(StressorType stressorType)
        {
            switch (stressorType)
            {
                case StressorType.WithTimePressure: return "Aufgabe mit Zeitdruck:";
                case StressorType.WithoutTimePressure: return "Aufgabe ohne Zeitdruck:";
                case StressorType.WithInterruption: return "Aufgabe mit Unterbrechung:";
                case StressorType.WithoutInterruption: return "Aufgabe ohne Unterbrechung:";
                case StressorType.Emotional: return "Emotionale Aufgabe:";
                default: return "Aufgabe:";
            }
        }

        /// <summary>
        /// Get stressor task description
        /// </summary>
        string GetStressorTaskDescription(StressorStudyTask task)
        {
            return $"{GetStressorTypeTitle(task.StressorType)} {GetTaskTypeDescription(task.Tasktype)}";
        }

        /// <summary>
        /// Display no tasks message
        /// </summary>
        void DisplayNoTasks()
        {
            if (taskTitle != null)
                taskTitle.text = "Nächste Aufgabe:";
            if (taskDescription != null)
                taskDescription.text = "Zurzeit keine weiteren Aufgaben";
            if (taskTimer != null)
                taskTimer.gameObject.SetActive(false);
            if (taskProgressBar != null)
                taskProgressBar.gameObject.SetActive(false);

            SetTaskStatusColor(normalColor);
        }

        /// <summary>
        /// Hide task after delay
        /// </summary>
        IEnumerator HideTaskAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            DisplayNoTasks();
        }

        #endregion

        #region Public API Methods

        /// <summary>
        /// Check if a task is currently active
        /// </summary>
        public bool IsTaskActive()
        {
            return isTaskActive;
        }

        /// <summary>
        /// Get current task (if any)
        /// </summary>
        public BaseTask GetCurrentTask()
        {
            return currentTask;
        }

        /// <summary>
        /// Get current stressor task (if any)
        /// </summary>
        public StressorStudyTask? GetCurrentStressorTask()
        {
            return currentStressorTask.TaskStatus != ENUM_TaskStatus.Corrupted ? currentStressorTask : null;
        }

        /// <summary>
        /// Cancel current task
        /// </summary>
        public void CancelCurrentTask()
        {
            if (currentTask != null)
            {
                CompleteTask(ENUM_TaskStatus.Cancelled);
            }
            else if (currentStressorTask.TaskStatus == ENUM_TaskStatus.Active)
            {
                CompleteStressorTask(ENUM_TaskStatus.Cancelled);
            }
        }

        /// <summary>
        /// Clear all tasks from queue
        /// </summary>
        public void ClearTaskQueue()
        {
            taskQueue.Clear();
            stressorTaskQueue.Clear();
            UpdateTaskListDisplay();
        }

        #endregion
    }

    /// <summary>
    /// Task block definition for simple task blocks
    /// </summary>
    [System.Serializable]
    public class TaskBlockDefinition
    {
        public string name;
        public string title;
        public string description;
        public string[] tasks;

        public TaskBlockDefinition(string name, string title, string[] tasks)
        {
            this.name = name;
            this.title = title;
            this.description = title;
            this.tasks = tasks;
        }
    }
}
