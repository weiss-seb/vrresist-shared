namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Interface for task implementations
    /// </summary>
    public interface ITaskInterface
    {
        ENUM_TaskStatus Status { get; set; }
        StressorType Studytype { get; set; }
        TaskType Tasktype { get; set; }
        float SecondsGiven { get; set; }
        float SecondsUsed { get; set; }

        void StartTask();
        void StopTask();
        string GetTaskDescription();
    }
}
