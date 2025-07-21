# Step 10: Scene-Based Scenario System Implementation

## Overview
The VR study system now supports scene-based scenarios where each scenario loads a unique Unity scene with its own location, NPCs, and environment. This provides much more flexibility and immersion compared to the previous position-based approach.

## System Architecture

### Core Components

#### 1. ScenarioSceneManager
- **Location**: `Assets/Scripts/WorkingImageTransfer/ScenarioSceneManager.cs`
- **Purpose**: Manages loading different Unity scenes for each scenario
- **Features**:
  - Asynchronous scene loading with progress display
  - Loading screen management
  - Scenario info text display
  - Status communication with tablet
  - Scene validation tools

#### 2. Updated ScenarioManager (Tablet)
- **Location**: `Assets/Scripts/WorkingImageTransfer/ScenarioManager.cs`
- **Purpose**: Enhanced to include scene configuration
- **New Fields**:
  - `scenarioId`: Unique identifier for each scenario
  - `sceneName`: Name of Unity scene to load
  - Scene configuration in Unity Inspector

#### 3. Updated MessageHandler (HMD)
- **Location**: `Assets/Scripts/WorkingImageTransfer/MessageHandler.cs`
- **Purpose**: Routes scenario changes to ScenarioSceneManager
- **Changes**:
  - Removed direct NPC positioning
  - Added ScenarioSceneManager integration
  - Scene-based scenario handling

## Setup Instructions

### 1. HMD Scene Setup

#### A. Waiting Scene (Main HMD Scene)
```
1. Create or use existing main HMD scene as "waiting scene"
2. Add ScenarioSceneManager component to a GameObject
3. Configure ScenarioSceneManager in Inspector:
   - Assign Loading Screen GameObject
   - Assign Loading Text (TMP_Text)
   - Assign Scenario Info Text (TMP_Text)
   - Assign TCPServer reference
   - Configure scenario scene data array (5 scenarios)
```

#### B. Individual Scenario Scenes
```
For each scenario (1-5):
1. Create new Unity scene: "Scenario1Scene", "Scenario2Scene", etc.
2. Design unique location/environment for each scenario
3. Place NPCs in appropriate starting positions
4. Add necessary components (EventTriggerSystem, etc.)
5. Add scenes to Build Settings (File > Build Settings)
```

### 2. Tablet Scene Setup

#### A. ScenarioManager Configuration
```
1. Open ScenarioManager in Unity Inspector
2. For each scenario, configure:
   - Scenario ID (0-4)
   - Scenario Name
   - Scene Name (must match Unity scene name)
   - Scenario Info Text (German instructions)
   - NPC starting positions (for reference)
   - Audio clip configurations
```

### 3. Scene Configuration Examples

#### Scenario 1: Emergency Room
```csharp
scenarioId = 0
scenarioName = "Notfall-Szenario"
sceneName = "Scenario1Scene"
scenarioInfoText = "Notfall-Szenario: Ein kritischer Patient wurde eingeliefert.\nArbeiten Sie mit dem medizinischen Team zusammen.\nBefolgen Sie die Anweisungen des Chefarztes."
```

#### Scenario 2: Surgery Preparation
```csharp
scenarioId = 1
scenarioName = "OP-Vorbereitung"
sceneName = "Scenario2Scene"
scenarioInfoText = "OP-Vorbereitung: Bereiten Sie den Patienten für die Operation vor.\nÜberprüfen Sie alle medizinischen Geräte.\nKommunizieren Sie mit dem Anästhesisten."
```

## Communication Protocol

### Scenario Change Message
```json
{
  "type": "SCENARIO_CHANGE",
  "content": [
    "Scenario Name",
    "{\"scenarioId\":0,\"scenarioName\":\"Emergency Room\",\"scenarioInfoText\":\"Instructions...\",\"sceneName\":\"Scenario1Scene\"}"
  ]
}
```

### Status Messages (HMD → Tablet)
```json
// Loading started
{"type": "SCENE_LOADING", "content": ["Scenario Name"]}

// Loading completed
{"type": "SCENE_LOADED", "content": ["Scenario Name"]}

// Error occurred
{"type": "SCENE_ERROR", "content": ["Error message"]}
```

## Loading Flow

### 1. User Selects Scenario (Tablet)
```
1. User selects scenario from dropdown
2. ScenarioManager sends SCENARIO_CHANGE message
3. Message includes scenario data as JSON
```

### 2. Scene Loading (HMD)
```
1. MessageHandler receives SCENARIO_CHANGE
2. Parses scenario data from JSON
3. Calls ScenarioSceneManager.LoadScenarioScene()
4. Shows loading screen with progress
5. Loads Unity scene asynchronously
6. Displays scenario info text
7. Sends status updates to tablet
```

### 3. Error Handling
```
- Scene not found in build settings
- JSON parsing errors
- Network communication issues
- Loading timeouts
```

## UI Components

### Loading Screen Elements
```
- Loading Screen GameObject (Canvas/Panel)
- Loading Text (TMP_Text) - shows progress
- Progress indicator (optional)
- Background/branding elements
```

### Scenario Info Display
```
- Scenario Info Text (TMP_Text)
- Positioned for VR viewing
- Auto-activated after scene loads
- German language support
```

## Development Workflow

### Adding New Scenarios
```
1. Create new Unity scene
2. Design environment and place NPCs
3. Add scene to Build Settings
4. Configure in ScenarioManager (tablet)
5. Update ScenarioSceneManager array (HMD)
6. Test scenario switching
```

### Testing Scenarios
```
1. Use ScenarioSceneManager.ValidateScenarioScenes()
2. Check Unity Console for validation results
3. Test scene loading in Play mode
4. Verify tablet-HMD communication
```

## Technical Details

### Scene Loading Performance
```
- Asynchronous loading prevents frame drops
- Progress display keeps user informed
- Scene preloading possible for faster switching
- Memory management for large scenes
```

### Memory Considerations
```
- Previous scenes are unloaded automatically
- Shared assets remain in memory
- Consider scene size for VR performance
- Use object pooling for NPCs if needed
```

## Troubleshooting

### Common Issues

#### Scene Not Found
```
Error: "Scene 'ScenarioXScene' not found in build settings"
Solution: Add scene to Build Settings (File > Build Settings)
```

#### Loading Stuck
```
Error: Scene loading progress stops at 90%
Solution: Check for missing dependencies or corrupted scene
```

#### Info Text Not Displaying
```
Error: Scenario info text not showing
Solution: Verify TMP_Text component assignment in ScenarioSceneManager
```

### Debug Tools

#### Validation Context Menu
```
Right-click ScenarioSceneManager → "Validate Scenario Scenes"
Shows which scenes exist in build settings
```

#### Detailed Logging
```
Enable "enableDetailedLogging" in ScenarioSceneManager
Provides comprehensive debug information
```

## Future Enhancements

### Possible Improvements
```
- Scene preloading for instant switching
- Dynamic scene generation
- Scenario-specific audio environments
- Advanced loading animations
- Scene transition effects
```

### Scalability
```
- Support for more than 5 scenarios
- Nested scenario categories
- Conditional scenario loading
- User-generated scenarios
```

## File Structure
```
Assets/Scripts/WorkingImageTransfer/
├── ScenarioSceneManager.cs          # HMD scene management
├── ScenarioManager.cs               # Tablet scenario config
├── MessageHandler.cs                # Updated message routing
└── README_Step10_Scene_Based_Scenarios.md
```

## Summary

The scene-based scenario system provides:
- ✅ Unique environments for each scenario
- ✅ Flexible scenario configuration
- ✅ Smooth loading experience
- ✅ Comprehensive error handling
- ✅ Developer-friendly tools
- ✅ Scalable architecture

This system enables researchers to create immersive, location-specific scenarios that enhance the realism and effectiveness of VR studies.
