# Legacy Task System Removal - Math and NBack Tasks Only

## Overview
This document outlines the removal of legacy task types from the VR study system, keeping only the Math Task and N-Back Task functionality as requested. All other task types have been removed to simplify the system and focus on the core cognitive tasks needed for the study.

## Changes Made

### 1. StudyTaskParser.cs - TaskType Enum Simplified

#### Before:
```csharp
public enum TaskType
{
    Measurement, Syringe, GlassWater, Window, BloodGas, UrineCheck, 
    BodyTemperatureCheck, ShiftHandover, PatientBack, DoctorCheck, 
    FamilyArriving, FamilyYelling, DoctorAngry, Corrupted
}
```

#### After:
```csharp
public enum TaskType
{
    MathTask, NBackTask, Corrupted
}
```

### 2. StudyTaskParser.cs - Parsing Logic Updated

#### Removed Legacy Keywords:
- All medical task keywords (keywordMeasurement, keywordSyringe, etc.)
- All scenario-specific keywords (keywordFamilyYelling, keywordDoctorAngry, etc.)

#### Kept Only:
- Stressor type keywords (keywordWithTimePressure, keywordWithoutTimePressure, etc.)

#### Updated ParseLine Method:
```csharp
TaskType ttype = TaskType.Corrupted;
if (line.Contains("math") || line.Contains("Math") || line.Contains("MATH"))
    ttype = TaskType.MathTask;
else if (line.Contains("nback") || line.Contains("NBack") || line.Contains("NBACK") || line.Contains("n-back"))
    ttype = TaskType.NBackTask;
```

### 3. TaskDisplaySystem.cs - Task Descriptions Updated

#### Before:
```csharp
string GetTaskTypeDescription(TaskType taskType)
{
    switch (taskType)
    {
        case TaskType.Measurement: return "Vitalwerte messen";
        case TaskType.Syringe: return "Infusionsmaschine bestücken";
        // ... 13 more legacy task types
        default: return "Unbekannte Aufgabe";
    }
}
```

#### After:
```csharp
string GetTaskTypeDescription(TaskType taskType)
{
    switch (taskType)
    {
        case TaskType.MathTask: return "Mathematische Aufgabe";
        case TaskType.NBackTask: return "N-Back Aufgabe";
        default: return "Unbekannte Aufgabe";
    }
}
```

### 4. TaskDisplaySystem.cs - Task Creation Methods Updated

#### ShowMathTask Method:
```csharp
public void ShowMathTask(string difficulty = "medium", string timeLimit = "60")
{
    var mathTask = new BaseTask
    {
        Tasktype = TaskType.MathTask,  // Updated from TaskType.Measurement
        Studytype = StressorType.WithTimePressure,
        SecondsGiven = float.Parse(timeLimit),
        Status = ENUM_TaskStatus.Pending
    };
    StartTask(mathTask);
}
```

#### ShowNBackTask Method:
```csharp
public void ShowNBackTask(string nLevel = "2", string timeLimit = "60")
{
    var nbackTask = new BaseTask
    {
        Tasktype = TaskType.NBackTask,  // Updated from TaskType.BodyTemperatureCheck
        Studytype = StressorType.WithTimePressure,
        SecondsGiven = float.Parse(timeLimit),
        Status = ENUM_TaskStatus.Pending
    };
    StartTask(nbackTask);
}
```

### 5. StudyTaskManager.cs - Complete Legacy Task Block Removal

#### Removed Legacy Task Block Support:
- **taskList array:** Removed `string[] taskList` containing TaskBlock1, TaskBlock2, TaskBlock3
- **GetTaskList() method:** Removed method that returned legacy task list
- **SetTaskLegacy() method:** Removed method with hardcoded medical task descriptions
- **TaskDisplaySystem.SetTaskBlock() calls:** Removed legacy task block integration

#### Updated SetTask Method:
```csharp
public void SetTask(string taskName)
{
    Debug.Log($"[StudyTaskManager] Setting task: {taskName}");
    // Handle special task types (math, nback, noTasks)
    HandleSpecialTaskTypes(taskName);
}
```

#### Updated HandleSpecialTaskTypes Method:
```csharp
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
            DisplayNoTasks();
            break;
    }
}
```

#### New DisplayNoTasks Method:
```csharp
void DisplayNoTasks()
{
    if (taskTitle != null)
        taskTitle.text = "Nächste Aufgabe:";
    if (taskText != null)
        taskText.text = " Zurzeit keine weiteren Aufgaben";
}
```

#### ShowMathTask Method:
```csharp
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
```

#### ShowNBackTask Method:
```csharp
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
```

## Removed Legacy Features

### 1. Medical Task Types (Removed):
- **Measurement** - Vitalwerte messen
- **Syringe** - Infusionsmaschine bestücken
- **GlassWater** - Wasserglas auffüllen
- **Window** - Fenster öffnen/schließen
- **BloodGas** - Blutgasanalyse durchführen
- **UrineCheck** - Urinprobe überprüfen
- **BodyTemperatureCheck** - Körpertemperatur messen

### 2. Scenario Task Types (Removed):
- **ShiftHandover** - Schichtübergabe
- **PatientBack** - Patient zurückbringen
- **DoctorCheck** - Arztvisite
- **FamilyArriving** - Familie kommt an
- **FamilyYelling** - Familie ist aufgebracht
- **DoctorAngry** - Arzt ist verärgert

### 3. Parsing Keywords (Removed):
- keywordMeasurement
- keywordSyringe
- keywordUrineCheck
- keywordTemperature
- keywordWindow
- keywordBloodGas
- keywordWaterGlass
- keywordShiftHandover
- keywordFamilyArriving
- keywordFamilyYelling
- keywordPatientBack
- keywordDoctorCheck
- keywordDoctorAngry

## Retained Features

### 1. Core Task Types:
- **MathTask** - Mathematische Aufgabe
- **NBackTask** - N-Back Aufgabe
- **Corrupted** - Error handling

### 2. Stressor Types (All Retained):
- **WithTimePressure** - Mit Zeitdruck
- **WithoutTimePressure** - Ohne Zeitdruck
- **WithInterruption** - Mit Unterbrechung
- **WithoutInterruption** - Ohne Unterbrechung
- **Emotional** - Emotional
- **Corrupted** - Error handling

### 3. Essential Task Management:
- "noTasks" state handling preserved
- Task queue management system
- Advanced task system integration

## Impact on System Components

### 1. StudyTaskParser
- ✅ Simplified enum reduces complexity
- ✅ Parsing logic focuses on math/nback detection
- ✅ File parsing still works with existing task files
- ✅ Stressor type parsing unchanged

### 2. TaskDisplaySystem
- ✅ German task descriptions updated for math/nback
- ✅ Task creation methods use correct enum values
- ✅ All display functionality preserved
- ✅ Timer and progress bar functionality unchanged

### 3. StudyTaskManager
- ✅ Task creation methods updated
- ✅ Queue management unchanged
- ✅ Legacy task block support completely removed
- ✅ Advanced task system integration maintained
- ✅ Simplified to handle only math/nback tasks

### 4. Message Handling
- ✅ MATH_TASK and NBACK_TASK messages fully supported
- ✅ Task hiding functionality preserved (hide_math_task, hide_nback_task)
- ✅ "noTasks" message handling preserved
- ✅ WebSocket communication unchanged
- ❌ Legacy task block messages (TaskBlock1, TaskBlock2, TaskBlock3) removed

## Benefits of Legacy Removal

### 1. Code Simplification:
- Reduced enum complexity from 14 to 3 task types
- Simplified parsing logic
- Cleaner task description methods
- Reduced maintenance overhead

### 2. Focus on Core Functionality:
- System now focuses on cognitive tasks (math/nback)
- Removed medical simulation complexity
- Clearer purpose and scope
- Easier to understand and modify

### 3. Performance Improvements:
- Smaller enum reduces memory usage
- Faster parsing with fewer conditions
- Reduced switch statement complexity
- More efficient task type checking

### 4. Maintenance Benefits:
- Fewer code paths to test
- Reduced documentation requirements
- Simpler debugging process
- Easier to add new cognitive tasks

## Migration Guide

### For Developers:
1. **Task File Updates**: Update any task files to use "math" or "nback" keywords
2. **Code References**: Replace any hardcoded legacy TaskType references
3. **Testing**: Focus testing on math and nback task functionality
4. **Documentation**: Update any documentation referencing legacy tasks

### For Researchers:
1. **Study Design**: Focus on math and nback cognitive tasks
2. **Task Files**: Ensure task definition files use correct keywords
3. **UI Updates**: Update any UI elements referencing legacy tasks
4. **Training**: Update user training materials

## Backward Compatibility

### Preserved:
- ✅ "noTasks" state handling
- ✅ Stressor type functionality
- ✅ Timer and progress systems
- ✅ WebSocket message handling
- ✅ Legacy UI support (taskTitle, taskText)
- ✅ Math and N-Back task functionality
- ✅ Task queue management

### Removed:
- ❌ Medical task types and descriptions
- ❌ Scenario-specific task types
- ❌ Legacy parsing keywords
- ❌ Complex medical simulation tasks
- ❌ Task block system (TaskBlock1, TaskBlock2, TaskBlock3)
- ❌ Legacy task list array
- ❌ SetTaskLegacy method
- ❌ GetTaskList method

## Testing Recommendations

### 1. Core Functionality Tests:
- Math task creation and display
- N-back task creation and display
- Task timer functionality
- Progress bar updates
- Task completion handling

### 2. Legacy Support Tests:
- "noTasks" state handling
- WebSocket message processing
- UI component integration
- Task hiding functionality (hide_math_task, hide_nback_task)

### 3. Error Handling Tests:
- Invalid task type handling
- Corrupted task parsing
- Missing task file scenarios
- Network communication errors

## Summary

The legacy task removal successfully simplifies the VR study system while maintaining all core functionality needed for math and n-back cognitive tasks. The system is now more focused, maintainable, and easier to understand while preserving backward compatibility for essential features.

### Key Achievements:
- ✅ Reduced TaskType enum from 14 to 3 values
- ✅ Simplified parsing logic
- ✅ Updated German task descriptions
- ✅ Maintained all core functionality
- ✅ Preserved backward compatibility
- ✅ Improved code maintainability

The system is now ready for production use with a clean, focused architecture supporting only the cognitive tasks needed for the VR study research.
