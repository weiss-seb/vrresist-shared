# VR Resist - Enhanced Architecture Summary

## Overview

This document outlines the enhanced architecture for the VR Resist project, focusing on improved tablet communication, scene management, and data organization using ScriptableObjects.

## Key Components

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

#### NPCController.cs (Enhanced)
- **Purpose**: Manages NPC behavior and audio clips
- **Location**: `Assets/Scripts/NPCController.cs`
- **Key Enhancements**:
  - `GetAudioData()` method returns both clip names and user-friendly labels
  - Better integration with ScriptableObject architecture
  - Improved audio clip management

### 2. Communication & Networking

#### TCPServer.cs (Singleton)
- **Purpose**: Persistent TCP server that survives scene changes
- **Location**: `Assets/Scripts/WorkingImageTransfer/TCPServer.cs`
- **Key Features**:
  - Singleton pattern with DontDestroyOnLoad
  - Automatic MessageHandler discovery in new scenes
  - Scene loading management integration
  - Basic tablet communication for scenario changes

#### MessageHandler.cs (Enhanced)
- **Purpose**: Handles incoming messages from tablet
- **Location**: `Assets/Scripts/WorkingImageTransfer/MessageHandler.cs`
- **Key Enhancements**:
  - Better integration with ScriptableObject data
  - Improved error handling and logging
  - Enhanced refresh functionality

#### TabletCommunicationData.cs
- **Purpose**: Data structures optimized for tablet communication
- **Location**: `Assets/Scripts/WorkingImageTransfer/TabletCommunicationData.cs`
- **Key Features**:
  - `TabletScenarioData`: Filtered scenario data for tablet UI
  - `NPCUIData`: NPC information with user-friendly labels
  - `TabletMessageFactory`: Helper methods for creating tablet messages
  - Loading progress tracking structures

### 3. Scene Management

#### SceneLoadingManager.cs
- **Purpose**: Manages scene loading with visual feedback
- **Location**: `Assets/Scripts/WorkingImageTransfer/SceneLoadingManager.cs`
- **Key Features**:
  - Async scene loading with progress tracking
  - Loading screen management with fade effects
  - Integration with ScriptableObject data for loading assets
  - Tablet synchronization during loading process

#### CameraControl.cs (Enhanced)
- **Purpose**: Manages camera switching and streaming
- **Location**: `Assets/Scripts/WorkingImageTransfer/CameraControl.cs`
- **Key Enhancements**:
  - Integration with SO_ScenarioData for camera configuration
  - Methods for tablet UI integration
  - Camera validation and error handling
  - Preparation for future streaming functionality

## Architecture Benefits

### 1. Improved Data Management
- **Centralized Configuration**: All scenario data in ScriptableObjects
- **Type Safety**: Strongly typed data structures
- **Validation**: Built-in configuration validation
- **Reusability**: ScriptableObjects can be reused across scenes

### 2. Enhanced Communication
- **Persistent Connection**: TCP server survives scene changes
- **Automatic Discovery**: Components automatically found in new scenes
- **Structured Messages**: Well-defined data structures for tablet communication
- **Error Resilience**: Better error handling and recovery

### 3. Better Scene Management
- **Loading Feedback**: Visual progress indicators
- **Smooth Transitions**: Fade effects and minimum loading times
- **Tablet Synchronization**: Real-time loading progress to tablet
- **Flexible Loading**: Support for both direct and managed scene loading

## Usage Guide

### Creating a New Scenario

1. **Create ScriptableObject**:
   ```csharp
   // In Unity Editor: Right-click → Create → ScriptableObjects → ScenarioData
   ```

2. **Configure Scenario Data**:
   - Set scenario name, description, and ID
   - Specify scene name to load
   - Add character names (must match GameObject names in scene)
   - Configure available camera positions
   - Set loading screen image and tip

3. **Scene Setup**:
   - Ensure NPCs have NPCController components
   - Set up camera positions as specified in ScriptableObject
   - Add MessageHandler to scene
   - Optionally add SceneLoadingManager for loading screens

### Loading a Scenario

```csharp
// Get the TCP server instance
TCPServer tcpServer = TCPServer.Instance;

// Load scenario (ScriptableObject)
SO_ScenarioData scenarioData = // ... load your ScriptableObject
tcpServer.LoadSceneWithScenario(scenarioData);
```

### Tablet Communication

The system automatically sends structured data to the tablet:

1. **Scenario Changes**: When scenes load
2. **Loading Progress**: During scene loading
3. **NPC Data**: Audio clips and labels
4. **Camera Information**: Available cameras for switching

## File Structure

```
Assets/Scripts/
├── Scriptable/
│   └── SO_ScenarioData.cs          # ScriptableObject for scenario configuration
├── WorkingImageTransfer/
│   ├── TCPServer.cs                # Singleton TCP server
│   ├── MessageHandler.cs           # Message processing
│   ├── TabletCommunicationData.cs  # Tablet data structures
│   ├── SceneLoadingManager.cs      # Scene loading with UI
│   ├── CameraControl.cs            # Camera management
│   └── ARCHITECTURE_SUMMARY.md     # This document
└── NPCController.cs                # Enhanced NPC management
```

## Integration Notes

### Existing Code Compatibility
- The enhanced architecture is designed to be backward compatible
- Existing MessageHandler functionality is preserved
- TCPServer maintains the same public interface
- NPCController additions are non-breaking

### Future Enhancements
- **Camera Streaming**: SceneLoadingManager prepared for camera feed to tablet
- **Advanced Tablet UI**: TabletCommunicationData structures ready for complex UI
- **Scenario Editor**: ScriptableObject architecture supports custom editors
- **Performance Monitoring**: Built-in logging and validation for debugging

## Troubleshooting

### Common Issues

1. **MessageHandler Not Found**:
   - Ensure each scene has a MessageHandler component
   - Check TCPServer logs for discovery messages

2. **ScriptableObject Validation Errors**:
   - Use `ValidateConfiguration()` method to check setup
   - Ensure all required fields are populated

3. **Scene Loading Issues**:
   - Verify scene names match exactly in ScriptableObject
   - Check that scenes are added to Build Settings

4. **Tablet Communication Problems**:
   - Verify TCP server is running (check logs)
   - Ensure tablet is connected to correct IP/port
   - Check message formatting in logs

### Debug Logging

All components use consistent logging prefixes:
- `[TCPServer]`: Network and singleton operations
- `[MessageHandler]`: Message processing
- `[SceneLoadingManager]`: Loading operations
- `[CameraControl]`: Camera operations
- `[SO_ScenarioData]`: ScriptableObject validation

## Conclusion

This enhanced architecture provides a robust foundation for VR scenario management with improved tablet integration. The ScriptableObject-based approach ensures maintainable, type-safe configuration while the singleton TCP server provides reliable cross-scene communication.

The system is designed to be extensible and can accommodate future requirements such as advanced tablet UI features, camera streaming, and complex scenario orchestration.
