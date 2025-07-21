# Implementation Guide - Enhanced VR Resist Architecture

## Overview

This guide documents the enhanced architecture implementation for the VR Resist project, focusing on improved tablet communication, scene management, and data organization using ScriptableObjects.

## Architecture Changes Implemented

### 1. ScriptableObject Data Architecture

#### SO_ScenarioData.cs
- **Purpose**: Centralized configuration for each VR scenario
- **Location**: `Assets/Scripts/Scriptable/SO_ScenarioData.cs`
- **Key Features**:
  - Scenario metadata (name, description, ID)
  - Scene configuration (character names, camera positions)
  - Loading screen assets (image, tips)
  - Validation methods for configuration integrity
  - Camera name extraction for tablet UI

#### Enhanced NPCController.cs
- **Location**: `Assets/Scripts/NPCController.cs`
- **Key Enhancements**:
  - `GetAudioData()` method returns both clip names and user-friendly labels
  - Better integration with ScriptableObject architecture
  - Improved audio clip management for tablet communication

### 2. Communication & Networking Enhancements

#### TCPServer.cs (Singleton Implementation)
- **Purpose**: Persistent TCP server that survives scene changes
- **Location**: `Assets/Scripts/WorkingImageTransfer/TCPServer.cs`
- **Key Features**:
  - Singleton pattern with DontDestroyOnLoad
  - Automatic MessageHandler discovery in new scenes
  - Scene loading management integration
  - Basic tablet communication for scenario changes

#### Enhanced MessageHandler.cs
- **Location**: `Assets/Scripts/WorkingImageTransfer/MessageHandler.cs`
- **Current Implementation**:
  - Streamlined message processing with core action types
  - Legacy message support for backward compatibility
  - Improved error handling and logging
  - Enhanced refresh functionality in `HandleRequest()` method
  - Direct method calls to EventTriggerSystem for better performance

#### TabletCommunicationData.cs
- **Purpose**: Data structures optimized for tablet communication
- **Location**: `Assets/Scripts/WorkingImageTransfer/TabletCommunicationData.cs`
- **Key Features**:
  - `TabletScenarioData`: Filtered scenario data for tablet UI
  - `NPCUIData`: NPC information with user-friendly labels
  - `TabletMessageFactory`: Helper methods for creating tablet messages
  - Loading progress tracking structures

### 3. Scene Management System

#### SceneLoadingManager.cs
- **Purpose**: Manages scene loading with visual feedback
- **Location**: `Assets/Scripts/WorkingImageTransfer/SceneLoadingManager.cs`
- **Key Features**:
  - Async scene loading with progress tracking
  - Loading screen management with fade effects
  - Integration with ScriptableObject data for loading assets
  - Tablet synchronization during loading process

#### Enhanced CameraControl.cs
- **Location**: `Assets/Scripts/WorkingImageTransfer/CameraControl.cs`
- **Key Enhancements**:
  - Integration with SO_ScenarioData for camera configuration
  - Methods for tablet UI integration
  - Camera validation and error handling
  - Preparation for future streaming functionality

## Implementation Details

### Current MessageHandler Architecture

The MessageHandler now uses a streamlined approach with three message categories:

1. **Core Action Types** (New System):
   - `NPC_WALK`: Move NPCs to positions
   - `NPC_TALK`: Play NPC audio clips
   - `MATH_TASK`: Start math cognitive tasks
   - `NBACK_TASK`: Start n-back cognitive tasks
   - `CAMERA_CHANGE`: Switch camera views
   - `ABORT_ALL`: Stop all activities
   - `END_STUDY`: End the study session
   - `SCENARIO_CHANGE`: Load new scenarios

2. **Legacy Types** (Backward Compatibility):
   - `speak`: Converted to NPC_TALK
   - `changeCamera`: Converted to CAMERA_CHANGE
   - `abort`: Converted to ABORT_ALL
   - `event`: Forwarded to EventTriggerSystem

3. **System Messages**:
   - `request`: Handles refresh requests and system queries
   - `task`: Forwarded to StudyTaskManager
   - `chat`: Debug chat messages

### Refresh Request Implementation

The refresh functionality is handled in the `HandleRequest()` method:

```csharp
private void HandleRequest(EventMessage msg)
{
    if (msg.content.Length > 0 && msg.content[0] == "refresh")
    {
        Debug.Log("[MessageHandler] Handling refresh request");

        // Send available audio clips for each NPC
        SendEventMessageToClient(new EventMessage("audioClipsListChefarzt", GetAudioClipsForCharacter(chefarzt)));
        SendEventMessageToClient(new EventMessage("audioClipsListKollege", GetAudioClipsForCharacter(kollege)));
        SendEventMessageToClient(new EventMessage("audioClipsListPatient", GetAudioClipsForCharacter(patient)));

        // Send available events and tasks
        if (eventTriggerSystem != null)
            SendEventMessageToClient(new EventMessage("eventList", eventTriggerSystem.eventList));

        // Send simplified task list (only cognitive tasks supported)
        if (taskManager != null)
        {
            string[] simplifiedTaskList = new string[] { "math_task", "nback_task", "noTasks" };
            SendEventMessageToClient(new EventMessage("taskList", simplifiedTaskList));
        }
    }
}
```

## Usage Guide

### Creating a New Scenario

1. **Create ScriptableObject**:
   - Right-click in Project window
   - Create → ScriptableObjects → ScenarioData
   - Name it appropriately (e.g., "HospitalScenario")

2. **Configure Scenario Data**:
   ```
   Scenario Name: "Hospital Recovery Room"
   Scene Name: "HospitalScene" (exact scene name)
   Description: "Patient recovery scenario with family interactions"
   Scenario ID: 1
   
   Character Names: ["chefarzt", "kollege", "patient"] (exact GameObject names)
   Available Positions: ["bedLeft1", "bedLeft2", "bedRight1", "doorInside", "doorOutside", "outside"]
   Available Cameras: [assign camera references]
   
   Loading Image: [assign sprite]
   Loading Tip: "Prepare for family interactions..."
   ```

3. **Scene Setup**:
   - Ensure NPCs have NPCController components
   - Set up waypoint positions as specified in ScriptableObject
   - Add MessageHandler to scene
   - Optionally add SceneLoadingManager for loading screens

### Loading a Scenario

```csharp
// Get the TCP server instance
TCPServer tcpServer = TCPServer.Instance;

// Load scenario (ScriptableObject)
SO_ScenarioData scenarioData = Resources.Load<SO_ScenarioData>("HospitalScenario");
tcpServer.LoadSceneWithScenario(scenarioData);
```

### Tablet Communication

The system automatically sends structured data to the tablet:

1. **On Refresh Requests**: Audio clips, event lists, and task lists
2. **On Scenario Changes**: New scenario data and loading progress
3. **On NPC Actions**: Status updates and confirmations
4. **On Camera Changes**: Available cameras and current view

Example tablet message structure:
```json
{
  "type": "taskList",
  "content": ["math_task", "nback_task", "noTasks"]
}
```

## File Structure

```
Assets/Scripts/
├── Scriptable/
│   └── SO_ScenarioData.cs          # ScriptableObject for scenario configuration
├── WorkingImageTransfer/
│   ├── TCPServer.cs                # Singleton TCP server
│   ├── MessageHandler.cs           # Enhanced message processing
│   ├── TabletCommunicationData.cs  # Tablet data structures
│   ├── SceneLoadingManager.cs      # Scene loading with UI
│   ├── CameraControl.cs            # Enhanced camera management
│   └── IMPLEMENTATION_GUIDE.md     # This document
└── NPCController.cs                # Enhanced NPC management
```

## Benefits of Current Implementation

### 1. Improved Message Processing
- **Streamlined Architecture**: Clear separation of core actions, legacy support, and system messages
- **Direct Method Calls**: Better performance with direct EventTriggerSystem calls
- **Error Resilience**: Comprehensive error handling and logging
- **Backward Compatibility**: Legacy message types still supported

### 2. Enhanced Data Management
- **Centralized Configuration**: All scenario data in ScriptableObjects
- **Type Safety**: Strongly typed data structures
- **Validation**: Built-in configuration validation
- **Reusability**: ScriptableObjects can be reused across scenes

### 3. Better Communication
- **Persistent Connection**: TCP server survives scene changes
- **Automatic Discovery**: Components automatically found in new scenes
- **Structured Messages**: Well-defined data structures for tablet communication
- **Simplified Task Lists**: Only supported cognitive tasks sent to tablet

### 4. Robust Scene Management
- **Loading Feedback**: Visual progress indicators
- **Smooth Transitions**: Fade effects and minimum loading times
- **Tablet Synchronization**: Real-time loading progress to tablet
- **Flexible Loading**: Support for both direct and managed scene loading

## Testing the Implementation

### 1. Test Message Processing

Monitor console for these log messages:
- `[MessageHandler] Processing message of type: [type]`
- `[MessageHandler] Processing core action: [action]`
- `[MessageHandler] Handling refresh request`

### 2. Test TCP Communication

Check for these log messages:
- `[TCPServer] TCP Server singleton initialized successfully`
- `[TCPServer] Connected to MessageHandler: [name]`
- `[MessageHandler] Sending JSON: [json]`

### 3. Test Tablet Reception

The tablet should receive structured JSON for:
- Audio clip lists for each NPC
- Simplified task list: `["math_task", "nback_task", "noTasks"]`
- Event lists from EventTriggerSystem

## Common Issues & Solutions

### Issue 1: "MessageHandler Not Found"
**Solution**: Ensure each scene has a GameObject with MessageHandler component

### Issue 2: "TCP Server Not Connecting"
**Solution**: 
- Check firewall settings
- Verify port 8080 is available
- Ensure tablet is on same network

### Issue 3: "NPC Commands Not Working"
**Solution**: 
- Verify NPC GameObjects are assigned in MessageHandler
- Check EventTriggerSystem is properly configured
- Ensure waypoint positions exist in scene

### Issue 4: "Scene Loading Problems"
**Solution**:
- Verify scene names match exactly in ScriptableObject
- Check scenes are added to Build Settings
- Ensure ScriptableObject is in Resources folder or properly referenced

## Migration Notes

### From Previous Architecture
- The enhanced MessageHandler maintains backward compatibility
- Existing tablet communication continues to work
- New ScriptableObject system is additive, not replacing existing functionality
- TCPServer singleton automatically handles scene transitions

### Future Enhancements Ready
- **Camera Streaming**: SceneLoadingManager prepared for camera feed to tablet
- **Advanced Tablet UI**: TabletCommunicationData structures ready for complex UI
- **Scenario Editor**: ScriptableObject architecture supports custom editors
- **Performance Monitoring**: Built-in logging and validation for debugging

## Conclusion

The enhanced architecture provides a robust foundation for VR scenario management with improved tablet integration. The current implementation successfully addresses the original communication issues while maintaining backward compatibility and providing a solid foundation for future development.

Key improvements include:
- Streamlined message processing with clear categorization
- Persistent TCP server with automatic component discovery
- ScriptableObject-based scenario configuration
- Enhanced error handling and logging
- Simplified task management focused on cognitive tasks

The system is production-ready and extensible for future requirements.
