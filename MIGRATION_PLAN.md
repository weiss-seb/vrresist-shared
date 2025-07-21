# Migration Plan - Removing Redundant Components

## Overview
After implementing SO_ScenarioData.cs, we have redundant components that should be removed to simplify the architecture and reduce maintenance burden.

## Components to Remove

### 1. ScenarioManager.cs - REMOVE
**Reason**: Completely redundant with SO_ScenarioData approach

**What it does that we no longer need**:
- Manages array of ScenarioData objects → Now handled by individual SO_ScenarioData assets
- Provides scenario switching logic → Now handled by TCPServer.LoadSceneWithScenario()
- Sends messages via WebSocketClient → Now handled by TCPServer

**Migration Steps**:
1. Delete `Assets/Scripts/WorkingImageTransfer/ScenarioManager.cs`
2. Remove any ScenarioManager references from scenes
3. Create individual SO_ScenarioData assets for each scenario instead

### 2. SceneLoadingManager.cs - REMOVE (Optional)
**Reason**: TCPServer already handles scene loading, loading screens may be unnecessary for VR

**What it does**:
- Async scene loading with progress → TCPServer.LoadSceneWithScenario() handles this
- Loading screen UI → May be overkill for VR scenarios
- Progress notifications to tablet → Can be simplified

**Migration Steps**:
1. Delete `Assets/Scripts/WorkingImageTransfer/SceneLoadingManager.cs`
2. Remove SceneLoadingManager references from TCPServer.cs
3. Simplify scene loading to direct SceneManager.LoadScene() calls

## Simplified Architecture After Migration

### Core Components (Keep):
1. **SO_ScenarioData.cs** - ScriptableObject for scenario configuration
2. **TCPServer.cs** - Singleton server with scene loading
3. **MessageHandler.cs** - Enhanced message processing
4. **TabletCommunicationData.cs** - Tablet communication structures
5. **CameraControl.cs** - Enhanced camera management
6. **NPCController.cs** - Enhanced NPC management

### Workflow After Migration:
1. **Create Scenarios**: Right-click → Create → ScriptableObjects → ScenarioData
2. **Configure Scenarios**: Set up all data in the ScriptableObject inspector
3. **Load Scenarios**: `TCPServer.Instance.LoadSceneWithScenario(scenarioData)`
4. **Scene Communication**: Automatic MessageHandler discovery and connection

## Benefits of Simplified Architecture

### 1. Reduced Complexity
- Single source of truth for scenario data (SO_ScenarioData)
- One scene loading mechanism (TCPServer)
- Fewer components to configure and maintain

### 2. Better Maintainability
- ScriptableObjects are version-controlled and shareable
- No duplicate data structures to keep in sync
- Clear separation of concerns

### 3. Unity Best Practices
- Uses Unity's native ScriptableObject system
- Editor-friendly workflow
- Asset-based configuration

### 4. Simplified Scene Setup
- Only need MessageHandler in each scene
- No ScenarioManager or SceneLoadingManager to configure
- Automatic component discovery

## Implementation Steps

### Step 1: Create ScriptableObject Assets
For each existing scenario, create a new SO_ScenarioData asset:

```
Assets/ScriptableObjects/Scenarios/
├── HospitalScenario.asset
├── EmergencyScenario.asset
├── RecoveryScenario.asset
└── ...
```

### Step 2: Update TCPServer (Remove SceneLoadingManager references)
Remove the complex loading manager integration and simplify to direct scene loading.

### Step 3: Remove Old Components
1. Delete ScenarioManager.cs
2. Delete SceneLoadingManager.cs
3. Remove references from other scripts

### Step 4: Test Simplified Workflow
1. Create test SO_ScenarioData assets
2. Test scene loading via TCPServer
3. Verify tablet communication works
4. Test MessageHandler discovery in new scenes

## Code Changes Required

### TCPServer.cs Updates:
- Remove SceneLoadingManager references
- Simplify LoadSceneWithScenario() to use direct SceneManager.LoadScene()
- Remove complex loading manager discovery

### MessageHandler.cs Updates:
- Remove any ScenarioManager references (if any exist)
- Ensure compatibility with SO_ScenarioData

## Migration Checklist

- [ ] Create SO_ScenarioData assets for all existing scenarios
- [ ] Update any scripts that reference ScenarioManager
- [ ] Remove SceneLoadingManager references from TCPServer
- [ ] Delete ScenarioManager.cs
- [ ] Delete SceneLoadingManager.cs (optional)
- [ ] Test scenario loading with simplified architecture
- [ ] Update documentation to reflect simplified workflow
- [ ] Remove ScenarioManager/SceneLoadingManager from all scenes

## Result

After migration, the architecture will be:
- **Simpler**: Fewer components to manage
- **More Maintainable**: Single source of truth for scenario data
- **Unity-Native**: Uses ScriptableObjects properly
- **Flexible**: Easy to add new scenarios without code changes
- **Robust**: Persistent TCPServer with automatic scene discovery

The core functionality remains the same, but with significantly reduced complexity and better maintainability.
