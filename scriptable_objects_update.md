# Scriptable Objects Update - Architecture Plan

## Overview
This document outlines the architectural changes to implement a persistent TCPServer singleton with ScriptableObject-driven scene configuration and dynamic MessageHandler population.

---

## 🎯 Core Objectives

1. **Persistent TCP Connection**: Convert TCPServer to singleton with DontDestroyOnLoad
2. **Dynamic Scene Population**: MessageHandler auto-populated from SO_ScenarioData
3. **Seamless Scene Transitions**: No connection drops during scene changes
4. **Enhanced UI Synchronization**: Real-time tablet UI updates with scenario data
5. **Loading Screen System**: Visual feedback during scene transitions

---

## 📋 Implementation Plan

### Phase 1: Core Infrastructure Updates

#### 1.1 NPCController Enhancement
- ✅ **TODO**: Add `List<string> audioLabels` field
- ✅ **TODO**: Implement `GetAudioData()` method for MessageHandler integration
- ✅ **TODO**: Add `GetLabelForClip(string clipName)` helper method
- ✅ **TODO**: Update inspector layout with proper tooltips

#### 1.2 SO_ScenarioData Enhancement
- ✅ **TODO**: Add camera references `Camera[] availableCameras`
- ✅ **TODO**: Add loading screen fields (`Sprite loadingImage`, `string loadingTip`)
- ✅ **TODO**: Add position references `string[] availablePositions`
- ✅ **TODO**: Update existing fields with proper headers and tooltips

#### 1.3 CameraControl Integration
- ✅ **TODO**: Add method to get available cameras for scenario data
- ✅ **TODO**: Implement camera streaming preparation for tablet
- ✅ **TODO**: Add camera identification system for remote control

### Phase 2: Network Architecture Overhaul

#### 2.1 TCPServer Singleton Conversion
- ✅ **TODO**: Convert TCPServer to singleton pattern
- ✅ **TODO**: Add DontDestroyOnLoad functionality
- ✅ **TODO**: Implement SceneManager.sceneLoaded event listener
- ✅ **TODO**: Add MessageHandler discovery system
- ✅ **TODO**: Implement scene transition handling

#### 2.2 MessageHandler Dynamic Population
- ✅ **TODO**: Remove hardcoded NPC references
- ✅ **TODO**: Implement auto-discovery from SO_ScenarioData
- ✅ **TODO**: Add NPC validation and error handling
- ✅ **TODO**: Implement dynamic audio data collection
- ✅ **TODO**: Add scene-specific configuration loading

#### 2.3 Data Transmission Protocol
- ✅ **TODO**: Create `TabletScenarioData` serializable class
- ✅ **TODO**: Create `NPCUIData` serializable class
- ✅ **TODO**: Implement filtered JSON generation
- ✅ **TODO**: Add scenario data transmission on scene load
- ✅ **TODO**: Implement progress updates for tablet

### Phase 3: Loading Screen System

#### 3.1 SceneLoadingManager
- ✅ **TODO**: Create SceneLoadingManager component
- ✅ **TODO**: Implement loading UI (image, tip, progress)
- ✅ **TODO**: Add progress tracking for async scene loading
- ✅ **TODO**: Integrate with TCPServer singleton
- ✅ **TODO**: Add tablet progress synchronization

#### 3.2 Loading Screen UI
- ✅ **TODO**: Design loading screen prefab
- ✅ **TODO**: Create progress bar component
- ✅ **TODO**: Add tip text display system
- ✅ **TODO**: Implement fade in/out transitions

### Phase 4: Testing Infrastructure (Future)

#### 4.1 Editor Tool Development (PINNED)
- 📌 **FUTURE**: Create custom editor window for scene testing
- 📌 **FUTURE**: Implement scene list with MessageHandler detection
- 📌 **FUTURE**: Add mock tablet command system
- 📌 **FUTURE**: Create quick scene switching tools

---

## 🏗️ Technical Architecture

### Data Flow Sequence

```
1. Scene Transition Request
   ├── TCPServer receives scene change command
   ├── Show loading screen (HMD + Tablet)
   ├── Send TabletScenarioData to tablet immediately
   └── Start async scene loading

2. Scene Loading Process
   ├── SceneManager.LoadSceneAsync()
   ├── Progress updates → Loading screen + Tablet
   └── Scene loaded event fired

3. Post-Load Setup
   ├── TCPServer.OnSceneLoaded() triggered
   ├── Find MessageHandler in new scene
   ├── MessageHandler.PopulateFromScenario(SO_ScenarioData)
   ├── Collect NPC audio data from scene
   ├── Send updated NPC data to tablet
   └── Hide loading screen

4. System Ready
   ├── Tablet UI updates with new scenario
   ├── All NPC buttons populated
   ├── Camera controls updated
   └── Ready for user interactions
```

### Class Relationships

```
TCPServer (Singleton)
├── SceneLoadingManager
├── MessageHandler (per scene)
│   ├── SO_ScenarioData
│   ├── NPCController[] (discovered)
│   └── CameraControl
└── TabletScenarioData (transmitted)
    └── NPCUIData[] (generated)
```

---

## 📊 Data Structures

### Enhanced SO_ScenarioData
```csharp
[CreateAssetMenu(fileName = "ScenarioData", menuName = "ScriptableObjects/ScenarioData")]
public class SO_ScenarioData : ScriptableObject
{
    [Header("Basic Scenario Info")]
    public string scenarioName;
    public string sceneName;
    public string description;
    public int scenarioId;
    
    [Header("Scene Configuration")]
    public string[] characterNames;      // GameObject names to find
    public string[] availablePositions;  // Waypoint names
    public Camera[] availableCameras;    // Camera references
    
    [Header("Loading Screen")]
    public Sprite loadingImage;
    public string loadingTip;
    
    [Header("Additional Data")]
    public string[] messages;
}
```

### Tablet Communication Data
```csharp
[System.Serializable]
public class TabletScenarioData
{
    public string scenarioName;
    public int scenarioId;
    public string description;
    public NPCUIData[] npcs;
    public string[] availablePositions;
    public string[] availableCameras;
    public string[] availableTasks;
    public float loadingProgress;
}

[System.Serializable]
public class NPCUIData
{
    public string npcName;
    public string[] audioClips;
    public string[] audioLabels;
}
```

---

## 🔧 Implementation Priority

### High Priority (Immediate)
1. ✅ NPCController audio labels enhancement
2. ✅ SO_ScenarioData structure update
3. ✅ TCPServer singleton conversion
4. ✅ MessageHandler dynamic population
5. ✅ Basic loading screen system

### Medium Priority (Next Sprint)
6. ✅ Camera integration and streaming prep
7. ✅ Tablet progress synchronization
8. ✅ Error handling and validation
9. ✅ Performance optimization

### Low Priority (Future)
10. 📌 Editor testing tools
11. 📌 Advanced loading animations
12. 📌 Multi-language support
13. 📌 Analytics integration

---

## 🚨 Critical Considerations

### Performance
- **Quest 2/3 Limitations**: Large scenes require careful memory management
- **Loading Optimization**: Async loading with proper progress tracking
- **Network Efficiency**: Minimize data transmission during transitions

### Error Handling
- **Missing NPCs**: Graceful fallback when characters not found
- **Network Issues**: Robust reconnection during scene transitions
- **Data Validation**: Ensure SO_ScenarioData integrity

### Testing Strategy
- **Scene Isolation**: Each scene must work independently
- **Mock Systems**: Tablet simulation for development
- **Regression Testing**: Ensure existing functionality preserved

---

## 📝 Implementation Notes

### Code Style Guidelines
- Use `[Header]` attributes for inspector organization
- Add `[Tooltip]` for complex fields
- Implement proper null checking
- Use consistent naming conventions
