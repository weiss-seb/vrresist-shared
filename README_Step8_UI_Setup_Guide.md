# Step 8: Pre-configure UI in Editor - Setup Guide

## Overview
Step 8 focuses on setting up the Unity Editor UI connections for the Task Display System and Event Control system. This step provides detailed instructions and helper scripts to streamline the Unity Editor configuration process.

## Required UI Hierarchy Setup

### 1. Task Display UI Structure
Create the following UI hierarchy in your Canvas:

```
Canvas
├── TaskCanvas (GameObject)
│   ├── TaskTitle (TextMeshPro - Text UI)
│   ├── TaskText (TextMeshPro - Text UI) 
│   ├── TaskTimer (TextMeshPro - Text UI)
│   ├── TaskStatus (TextMeshPro - Text UI)
│   ├── TaskProgressBar (Image - UI)
│   ├── TaskStatusIcon (Image - UI)
│   └── TaskListPanel (GameObject)
│       ├── TaskListScrollRect (Scroll Rect)
│       └── TaskListContainer (GameObject with Vertical Layout Group)
```

### 2. Event Control UI Structure
Create the following UI panels for remote control:

```
Canvas
├── EventControlCanvas (GameObject)
│   ├── NPCActionsPanel (GameObject)
│   │   ├── NurseSection (GameObject)
│   │   │   ├── NurseWalkButtons (Button[])
│   │   │   └── NurseTalkButtons (Button[])
│   │   ├── BrotherSection (GameObject)
│   │   │   ├── BrotherWalkButtons (Button[])
│   │   │   └── BrotherTalkButtons (Button[])
│   │   └── WifeSection (GameObject)
│   │       ├── WifeWalkButtons (Button[])
│   │       └── WifeTalkButtons (Button[])
│   ├── TaskActionsPanel (GameObject)
│   │   ├── ShowMathTaskButton (Button)
│   │   ├── ShowNBackTaskButton (Button)
│   │   ├── HideMathTaskButton (Button)
│   │   └── HideNBackTaskButton (Button)
│   ├── CameraActionsPanel (GameObject)
│   │   └── CameraButtons (Button[]) - Camera1, Camera2, etc.
│   └── StudyControlPanel (GameObject)
│       ├── AbortAllButton (Button)
│       ├── EndStudyButton (Button)
│       └── RefreshButton (Button)
```

## Component Configuration

### 1. TaskDisplaySystem Component Setup

**GameObject:** Create an empty GameObject named "TaskDisplaySystem"
**Component:** Add the TaskDisplaySystem script

**Inspector Settings:**
```
[Header("UI References")]
Task Canvas: Drag TaskCanvas GameObject
Task Title: Drag TaskTitle TextMeshPro component
Task Description: Drag TaskText TextMeshPro component
Task Timer: Drag TaskTimer TextMeshPro component
Task Status: Drag TaskStatus TextMeshPro component
Task Progress Bar: Drag TaskProgressBar Image component
Task Status Icon: Drag TaskStatusIcon Image component

[Header("Task List Display")]
Task List Panel: Drag TaskListPanel GameObject
Task List Container: Drag TaskListContainer Transform
Task Item Prefab: Create and assign TaskItemPrefab
Task List Scroll Rect: Drag TaskListScrollRect component

[Header("Visual Feedback")]
Normal Color: White (255, 255, 255, 255)
Active Color: Yellow (255, 255, 0, 255)
Success Color: Green (0, 255, 0, 255)
Failure Color: Red (255, 0, 0, 255)
Warning Color: Orange (255, 165, 0, 255)

[Header("Timer Settings")]
Show Timer: ✓ Checked
Show Progress Bar: ✓ Checked
Warning Time Threshold: 10

[Header("Debug Settings")]
Enable Detailed Logging: ✓ Checked (for development)
```

### 2. StudyTaskManager Component Setup

**GameObject:** Find existing StudyTaskManager or create new GameObject
**Component:** StudyTaskManager script should already be attached

**Inspector Settings:**
```
[Header("Task System Integration")]
Task Display System: Drag TaskDisplaySystem GameObject

[Header("Advanced Task Management")]
Use Advanced Task System: ✓ Checked
Task Parser: Drag StudyTaskParser GameObject (if available)

[Header("Legacy UI Support")]
Task Canvas: Drag TaskCanvas GameObject (for backward compatibility with UI elements)
```

**Note:** Legacy task list array has been removed. Only Math and N-Back tasks are supported.

### 3. EventControl Component Setup

**GameObject:** Create an empty GameObject named "EventControl"
**Component:** Add the EventControl script

**Inspector Settings:**
```
[Header("System References")]
Web Socket Client: Drag WebSocketClient GameObject
Message Handler: Drag MessageHandler GameObject
Event Trigger System: Drag EventTriggerSystem GameObject
Study Task Manager: Drag StudyTaskManager GameObject

[Header("NPC References")]
Nurse Object: Drag Nurse GameObject
Brother Object: Drag Brother GameObject
Wife Object: Drag Wife GameObject
Doctor Object: Drag Doctor GameObject (if available)
Anesthesiologist Object: Drag Anesthesiologist GameObject (if available)

[Header("Pre-configured UI Sections")]
NPC Actions Panel: Drag NPCActionsPanel GameObject
Task Actions Panel: Drag TaskActionsPanel GameObject
Camera Actions Panel: Drag CameraActionsPanel GameObject
Study Control Panel: Drag StudyControlPanel GameObject

[Header("NPC Action Buttons - Assign in Unity Editor")]
Nurse Walk Buttons: Drag all nurse walk buttons (Size: 6)
Brother Walk Buttons: Drag all brother walk buttons (Size: 6)
Wife Walk Buttons: Drag all wife walk buttons (Size: 6)
Nurse Talk Buttons: Drag all nurse talk buttons (Size: varies)
Brother Talk Buttons: Drag all brother talk buttons (Size: varies)
Wife Talk Buttons: Drag all wife talk buttons (Size: varies)

[Header("Task Action Buttons - Assign in Unity Editor")]
Show Math Task Button: Drag ShowMathTaskButton
Show N Back Task Button: Drag ShowNBackTaskButton
Hide Math Task Button: Drag HideMathTaskButton
Hide N Back Task Button: Drag HideNBackTaskButton

[Header("Camera Control Buttons - Assign in Unity Editor")]
Camera Buttons: Drag all camera buttons (Size: varies)

[Header("Study Control Buttons - Assign in Unity Editor")]
Abort All Button: Drag AbortAllButton
End Study Button: Drag EndStudyButton
Refresh Button: Drag RefreshButton

[Header("Study Configuration")]
Available Positions: Keep default array:
- bedLeft1
- bedLeft2
- bedRight1
- doorInside
- doorOutside
- outside

[Header("Debug Settings")]
Enable Detailed Logging: ✓ Checked (for development)
```

## Button Configuration Guide

### NPC Walk Buttons
Create 6 buttons for each NPC with the following labels:
1. "Bed Left 1" → bedLeft1
2. "Bed Left 2" → bedLeft2
3. "Bed Right 1" → bedRight1
4. "Door Inside" → doorInside
5. "Door Outside" → doorOutside
6. "Outside" → outside

### NPC Talk Buttons
The number of talk buttons depends on available audio clips for each NPC. The EventControl script will automatically populate these based on the NPCController components.

### Task Action Buttons
Create buttons with the following labels:
- "Show Math Task" (ShowMathTaskButton)
- "Show N-Back Task" (ShowNBackTaskButton)
- "Hide Math Task" (HideMathTaskButton)
- "Hide N-Back Task" (HideNBackTaskButton)

**Note:** Legacy task block buttons (TaskBlock1, TaskBlock2, TaskBlock3) are no longer supported after legacy removal.

### Camera Buttons
Create buttons for each available camera view:
- "Camera 1", "Camera 2", "Camera 3", etc.

### Study Control Buttons
Create buttons with the following labels:
- "Abort All" (AbortAllButton)
- "End Study" (EndStudyButton)
- "Refresh" (RefreshButton)

## Task Item Prefab Creation

### Create TaskItemPrefab:
1. Create new GameObject named "TaskItemPrefab"
2. Add Image component (background)
3. Add child GameObject with TextMeshPro component
4. Configure layout:
   - Background: Semi-transparent panel
   - Text: White text, appropriate font size
   - Layout: Horizontal or vertical as needed
5. Save as Prefab in Resources folder

## Auto-Setup Helper Script

The `UISetupHelper.cs` script provides automated UI creation and configuration tools.

### Using UISetupHelper:

1. **Add UISetupHelper to Scene:**
   - Create empty GameObject named "UISetupHelper"
   - Add UISetupHelper component
   - Assign target Canvas in inspector

2. **Configure Settings:**
   ```
   [Header("Auto-Setup Configuration")]
   Auto Setup On Start: ✓ (if you want automatic setup)
   Target Canvas: Drag your main Canvas

   [Header("Task Display Setup")]
   Create Task Display UI: ✓ Checked
   Task Canvas Position: (100, -100)
   Task Canvas Size: (400, 300)

   [Header("Event Control Setup")]
   Create Event Control UI: ✓ Checked
   Event Control Position: (-400, 0)
   Event Control Size: (300, 600)
   ```

3. **Run Setup:**
   - Right-click UISetupHelper component → "Setup UI"
   - Or enable "Auto Setup On Start" and play scene

### Context Menu Options:
- **Setup UI:** Creates all UI elements automatically
- **Create Task Item Prefab:** Creates prefab for task list items
- **Auto-Assign TaskDisplaySystem:** Attempts to auto-assign UI references

## Manual Setup Steps (Alternative)

If you prefer manual setup or need to customize the layout:

### Step 1: Create Task Display UI

1. **Create TaskCanvas:**
   - Right-click Canvas → Create Empty → Name: "TaskCanvas"
   - Add Image component (background)
   - Set color to semi-transparent black (0, 0, 0, 0.7)

2. **Create Text Elements:**
   - TaskTitle: TextMeshPro - Text (UI)
   - TaskText: TextMeshPro - Text (UI)
   - TaskTimer: TextMeshPro - Text (UI)
   - TaskStatus: TextMeshPro - Text (UI)

3. **Create Visual Elements:**
   - TaskProgressBar: Image (UI) with fill type
   - TaskStatusIcon: Image (UI) for status indicator

4. **Create Task List (Optional):**
   - TaskListPanel: GameObject with Image background
   - TaskListScrollRect: Scroll Rect component
   - TaskListContainer: GameObject with Vertical Layout Group

### Step 2: Create Event Control UI

1. **Create EventControlCanvas:**
   - Right-click Canvas → Create Empty → Name: "EventControlCanvas"
   - Add Image component (dark background)

2. **Create Panel Structure:**
   ```
   EventControlCanvas
   ├── NPCActionsPanel
   │   ├── NurseSection
   │   ├── BrotherSection
   │   └── WifeSection
   ├── TaskActionsPanel
   ├── CameraActionsPanel
   └── StudyControlPanel
   ```

3. **Add Buttons to Each Panel:**
   - Use Button (TextMeshPro) components
   - Configure button colors and text
   - Organize in logical groups

### Step 3: Component Assignment

1. **TaskDisplaySystem Component:**
   - Create GameObject named "TaskDisplaySystem"
   - Add TaskDisplaySystem script
   - Drag all UI elements to corresponding fields

2. **StudyTaskManager Component:**
   - Find existing StudyTaskManager GameObject
   - Assign TaskDisplaySystem reference
   - Configure advanced task system settings

3. **EventControl Component:**
   - Create GameObject named "EventControl"
   - Add EventControl script
   - Assign all system references and UI buttons

## Validation and Testing

### Validation Checklist:

#### Task Display System:
- [ ] TaskCanvas exists and has background
- [ ] TaskTitle shows "Nächste Aufgabe:"
- [ ] TaskText displays task descriptions
- [ ] TaskTimer shows MM:SS format
- [ ] TaskStatus displays current status
- [ ] TaskProgressBar fills during timed tasks
- [ ] TaskStatusIcon changes color with status

#### Event Control System:
- [ ] All NPC buttons are assigned and functional
- [ ] Task action buttons trigger correct commands
- [ ] Camera buttons change camera views
- [ ] Study control buttons work properly
- [ ] Button labels are clear and descriptive

#### Component Integration:
- [ ] TaskDisplaySystem references are assigned
- [ ] StudyTaskManager has TaskDisplaySystem reference
- [ ] EventControl has all system references
- [ ] NPC GameObjects are assigned for audio clips
- [ ] WebSocketClient is connected for remote control

### Testing Procedures:

1. **Task Display Testing:**
   ```csharp
   // Test cognitive tasks only (legacy task blocks removed)
   studyTaskManager.SetTask("math_task");
   studyTaskManager.SetTask("nback_task");
   studyTaskManager.SetTask("noTasks");

   // Test advanced tasks with new TaskType enum
   var mathTask = studyTaskManager.CreateTask(TaskType.MathTask, StressorType.WithTimePressure, 60f);
   studyTaskManager.StartAdvancedTask(mathTask);
   
   var nbackTask = studyTaskManager.CreateTask(TaskType.NBackTask, StressorType.WithTimePressure, 45f);
   studyTaskManager.StartAdvancedTask(nbackTask);
   ```

2. **Event Control Testing:**
   - Click NPC walk buttons → Check NPC movement
   - Click NPC talk buttons → Check audio playback
   - Click task buttons → Check task display changes
   - Click camera buttons → Check camera switching

3. **WebSocket Testing:**
   - Connect tablet/remote device
   - Send task commands via WebSocket
   - Verify commands are received and processed
   - Check bidirectional communication

## Troubleshooting

### Common Issues:

1. **UI Elements Not Visible:**
   - Check Canvas render mode and camera assignment
   - Verify UI element positions and anchoring
   - Check Canvas Scaler settings

2. **Buttons Not Responding:**
   - Verify Button components are added
   - Check EventSystem exists in scene
   - Ensure buttons are not blocked by other UI elements

3. **TaskDisplaySystem Not Working:**
   - Check all UI references are assigned
   - Verify TaskDisplaySystem is in correct namespace
   - Check for null reference exceptions in console

4. **EventControl Not Sending Commands:**
   - Verify WebSocketClient is connected
   - Check MessageHandler is receiving messages
   - Ensure EventTriggerSystem is processing events

5. **NPC Audio Not Playing:**
   - Check NPCController components exist
   - Verify audio clips are assigned
   - Check audio source settings

### Debug Tools:

1. **Enable Detailed Logging:**
   ```csharp
   // In TaskDisplaySystem
   enableDetailedLogging = true;

   // In EventControl
   enableDetailedLogging = true;
   ```

2. **Console Commands:**
   - Check Unity Console for error messages
   - Look for WebSocket connection status
   - Monitor task state changes

3. **Inspector Debugging:**
   - Watch TaskDisplaySystem fields in real-time
   - Monitor StudyTaskManager task queue
   - Check EventControl button assignments

## Performance Optimization

### UI Performance:
- Use object pooling for task list items
- Minimize UI updates in Update() loops
- Use Canvas Groups for batch UI operations
- Optimize text rendering with static text where possible

### Memory Management:
- Properly dispose of unused UI elements
- Use weak references for event subscriptions
- Clear task queues when not needed
- Optimize texture sizes for UI images

## Customization Options

### Visual Customization:
- Modify colors in TaskDisplaySystem inspector
- Adjust font sizes and styles
- Change button layouts and spacing
- Add custom icons and graphics

### Functional Customization:
- Add new task types and descriptions
- Extend NPC action capabilities
- Create custom button layouts
- Implement additional UI panels

### Localization:
- Replace German text with other languages
- Use localization system for dynamic text
- Support multiple language switching
- Adapt UI layouts for different text lengths

## Conclusion

Step 8 provides comprehensive tools and instructions for setting up the UI system in Unity Editor. The combination of automated setup scripts and detailed manual instructions ensures that the system can be configured efficiently while maintaining flexibility for customization.

### Key Benefits:
- **Automated Setup:** UISetupHelper reduces manual work
- **Comprehensive Documentation:** Detailed instructions for all components
- **Validation Tools:** Built-in testing and troubleshooting guides
- **Flexibility:** Support for both automated and manual setup approaches
- **Scalability:** Easy to extend and customize for specific needs

The UI setup is now ready for production use and provides a solid foundation for the VR study's remote control and task management interface.

## Files Created/Modified:
- ✅ **NEW:** `UISetupHelper.cs` - Automated UI setup script
- ✅ **DOCUMENTED:** `README_Step8_UI_Setup_Guide.md` - Comprehensive setup guide

**Status: READY FOR IMPLEMENTATION** ✅

## Next Steps:
1. Run UISetupHelper in Unity Editor
2. Assign component references as described
3. Test all UI functionality
4. Validate WebSocket communication
5. Perform final integration testing

The system is now ready for Step 9: Final Integration and Testing.
