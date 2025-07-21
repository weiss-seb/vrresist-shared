using System.Collections.Generic;

namespace OVGU.VAR.VRResist

{
    [System.Serializable]
    public class BaseTaskListWrapper
    {
        public List<BaseTask> tasks;
        public bool taskRunning;
    }
    [System.Serializable]
    public class BaseTaskReferenceListWrapper
    {
        public List<int> taskReferences;
        public bool taskRunning;
    }

    [System.Serializable]
    public class RunningTaskReferenceListWrapper
    {
        public List<int> runningTaskReferences;
        public bool taskRunning;
    }


    [System.Serializable]
    public class BaseTask : ITaskInterface
    {
        public int Index
        {
            get; set;
        }
        public StressorType Studytype
        {
            get;
            set;
        }
        public TaskType Tasktype
        {
            get;
            set;
        }
        public float SecondsGiven
        {
            get;
            set;
        }
        public float SecondsUsed
        {
            get;
            set;
        }
        public ENUM_TaskStatus Status
        {
            get;
            set;
        }
        public void StartTask()
        {
            throw new System.NotImplementedException();
        }

        public void StopTask()
        {
            throw new System.NotImplementedException();
        }

        public string GetTaskDescription()
        {

            return "Task: " + Studytype + "," + Tasktype + ", time given: " + SecondsGiven;
        }
        /*
        public BaseTask Basetask()
        {
            Studytype = StressorType.Corrupted;
            Tasktype = TaskType.Corrupted;
            Status = ENUM_TaskStatus.Pending;
            SecondsGiven = 30000;
            return this;
        }
        */
    }
}
