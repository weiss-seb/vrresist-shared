using System;
using System.Collections.Generic;

namespace OVGU.VAR.VRResist
{
    [Serializable]
    public class BaseTaskListWrapper
    {
        public List<BaseTask> tasks;
        public bool taskRunning;
    }

    [Serializable]
    public class BaseTaskReferenceListWrapper
    {
        public List<int> taskReferences;
        public bool taskRunning;
    }

    [Serializable]
    public class RunningTaskReferenceListWrapper
    {
        public List<int> runningTaskReferences;
        public bool taskRunning;
    }

    [Serializable]
    public class BaseTask : ITaskInterface
    {
        public int Index { get; set; }
        public StressorType Studytype { get; set; }
        public TaskType Tasktype { get; set; }
        public float SecondsGiven { get; set; }
        public float SecondsUsed { get; set; }
        public ENUM_TaskStatus Status { get; set; }

        public void StartTask()
        {
            throw new NotImplementedException();
        }

        public void StopTask()
        {
            throw new NotImplementedException();
        }

        public string GetTaskDescription()
        {
            return "Task: " + Studytype + "," + Tasktype + ", time given: " + SecondsGiven;
        }
    }

    /// <summary>
    /// Struct for stressor study tasks
    /// </summary>
    [Serializable]
    public struct StressorStudyTask
    {
        public StressorType StressorType;
        public TaskType Tasktype;
        public ENUM_TaskStatus TaskStatus;
        public float SecondsGiven;
        public float SecondsUsed;

        public StressorStudyTask(StressorType stressorType, TaskType taskType, ENUM_TaskStatus taskStatus, float secondsGiven, float secondsUsed)
        {
            StressorType = stressorType;
            Tasktype = taskType;
            TaskStatus = taskStatus;
            SecondsGiven = secondsGiven;
            SecondsUsed = secondsUsed;
        }
    }
}
