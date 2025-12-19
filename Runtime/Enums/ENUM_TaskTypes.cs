namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Status of a study task
    /// </summary>
    public enum ENUM_TaskStatus
    {
        Active,
        Success,
        OutOfTime,
        WrongExecution,
        Pending,
        Corrupted,
        Cancelled
    }

    /// <summary>
    /// Type of stressor condition
    /// </summary>
    public enum StressorType
    {
        WithTimePressure,
        WithoutTimePressure,
        WithInterruption,
        WithoutInterruption,
        Emotional,
        Corrupted
    }

    /// <summary>
    /// Type of cognitive task
    /// </summary>
    public enum TaskType
    {
        MathTask,
        NBackTask,
        Corrupted
    }
}
