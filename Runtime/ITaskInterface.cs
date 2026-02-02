using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OVGU.VAR.VRResist
{
    public interface ITaskInterface
    {

        ENUM_TaskStatus Status { get; set; }
        StressorType Studytype { get; set; }
        TaskType Tasktype { get; set; }
        float SecondsGiven { get; set; }
        float SecondsUsed { get; set; }


        abstract void StartTask();
        abstract void StopTask();

        abstract string GetTaskDescription();
    }

}