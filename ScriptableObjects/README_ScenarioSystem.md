# ScriptableObject Scenario System - Usage Guide

## Overview

This system allows you to preconfigure scenario data in Unity's Editor using ScriptableObjects and automatically load that data into scene components when the scene starts. This solves the problem of trying to store GameObject references in ScriptableObjects (which doesn't work) by using string identifiers instead.

## How It Works

### 1. **SO_ScenarioData** (ScriptableObject)
- Stores all scenario configuration data
- Uses **string names** instead of GameObject references
- Can be created and edited in Unity Editor
- Persistent across scenes and sessions

### 2. **ScenarioLoader** (MonoBehaviour)
- Bridges ScriptableObject data to scene GameObjects
- Finds GameObjects by name/tag when scene loads
- Applies configuration to EventTriggerSystem
- Sets NPC starting positions

### 3. **EventTriggerSystem** (MonoBehaviour)
- Receives the configured data from ScenarioLoader
- Uses the data for NPC interactions and scenario logic

## Step-by-Step Usage

### Step 1: Create a ScriptableObject Asset

1. **In Unity Editor:**
   - Right-click in Project window
   - Go to `Create > ScriptableObjects > ScenarioData`
   - Name your asset (e.g., "HospitalScenario")

2. **Configure the ScriptableObject (ALL FIELDS MUST BE CONFIGURED):**
   ```
   Scenario Information:
   - Scenario ID: 0
   - Scenario Name: "Hospital Emergency"
   - Description: "Medical emergency scenario..."
   - Info Text: "Sie befinden sich in einem Krankenhauszimmer..."
   - Scene Name: "HospitalScene"
   
   Character Setup (Configure based on YOUR scene):
   - Character Names: Add names that match your scene's GameObjects
     Example: ["PatientNPC", "DoctorNPC", "NurseNPC"]
   - Character Tags: Add tags if using tag-based finding (optional)
     Example: ["Patient", "Doctor", "Nurse"]
   - Available Positions: Add waypoint names from your scene
     Example: ["bedLeft1", "bedLeft2", "doorInside", "outside"]
   - Camera Names: Add camera names from your scene
     Example: ["MainCamera", "OverviewCam", "CloseupCam"]
   
   NPC Starting Positions (Set based on your scene layout):
   - Chefarzt Start Position: (2, 0, 1)
   - Kollege Start Position: (-2, 0, 1)  
   - Patient Start Position: (0, 0, -1)
   
   Audio Configuration (Configure per NPC):
   - Chefarzt Audio Clips: ["begruessung", "behandlung", ...]
   - Chefarzt Audio Labels: ["Begrüßung", "Behandlung", ...]
   - (Same for Kollege and Patient)
   ```

   **⚠️ IMPORTANT: No default values are provided. You MUST configure all arrays based on your specific scene setup.**

### Step 2: Set Up Your Scene

1. **Add ScenarioLoader to your scene:**
   - Create empty GameObject named "ScenarioLoader"
   - Add `ScenarioLoader` component
   - Assign your SO_ScenarioData asset to the "Scenario Data" field
   - Optionally assign EventTriggerSystem reference (or leave empty for auto-find)

2. **Name your GameObjects correctly:**
   - Make sure character GameObjects match the names in `characterNames`
   - OR use tags that match `characterTags`
   - Make sure waypoint GameObjects match names in `availablePositions`
   - Make sure camera GameObjects match names in `cameraNames`

### Step 3: Scene Loading Process

When the scene starts:

1. **ScenarioLoader.Start()** runs automatically
2. **FindCharacters()** - Searches for GameObjects by name/tag
3. **FindWaypoints()** - Searches for waypoint GameObjects
4. **FindCameras()** - Searches for camera GameObjects  
5. **ApplyToEventTriggerSystem()** - Sets references in EventTriggerSystem
6. **SetNPCStartingPositions()** - Moves NPCs to configured positions

## Example Scene Setup

```
Scene Hierarchy:
├── ScenarioLoader (with ScenarioLoader component)
├── EventTriggerSystem (with EventTriggerSystem component)
├── Characters/
│   ├── Patient (GameObject with NPCController)
│   ├── Colleague (GameObject with NPCController)  
│   └── HeadDoctor (GameObject with NPCController)
├── Waypoints/
│   ├── bedLeft1
│   ├── bedLeft2
│   ├── doorInside
│   └── outside
└── Cameras/
    ├── Camera1
    ├── Camera2
    └── Camera3
```

## Key Benefits

### ✅ **Editor-Friendly**
- Configure scenarios visually in Unity Inspector
- No code changes needed for new scenarios
- Easy to duplicate and modify existing scenarios

### ✅ **Version Control**
- ScriptableObject assets are properly version-controlled
- Team members can share scenario configurations
- Changes are tracked in Git

### ✅ **Runtime Flexibility**
- Can switch scenarios at runtime using `ScenarioLoader.ReloadScenario()`
- ScriptableObjects can be loaded from Resources or Addressables
- Easy to create scenario selection systems

### ✅ **Type Safety**
- No more "magic strings" scattered through code
- Centralized configuration reduces errors
- Validation methods help catch missing references

## Advanced Usage

### Creating Scenarios at Runtime

```csharp
// Create new scenario data
SO_ScenarioData newScenario = ScriptableObject.CreateInstance<SO_ScenarioData>();
newScenario.scenarioName = "Dynamic Scenario";
newScenario.characterNames = new string[] { "Patient", "Doctor" };

// Apply to scene
ScenarioLoader loader = FindObjectOfType<ScenarioLoader>();
loader.ReloadScenario(newScenario);
```

### Accessing Scenario Data

```csharp
// From EventTriggerSystem
if (scenarioData != null)
{
    string[] patientAudio = scenarioData.GetAudioClipsForNPC("patient");
    Vector3 startPos = scenarioData.GetStartingPositionForNPC("chefarzt");
}

// From ScenarioLoader
ScenarioLoader loader = FindObjectOfType<ScenarioLoader>();
GameObject patient = loader.GetCharacter("Patient");
GameObject waypoint = loader.GetWaypoint("bedLeft1");
```

### Validation

```csharp
// Validate scenario configuration
if (scenarioData.ValidateConfiguration())
{
    Debug.Log("Scenario is properly configured");
}

// Validate scene loading
ScenarioLoader loader = FindObjectOfType<ScenarioLoader>();
loader.ValidateScenarioLoading(); // Right-click context menu in Inspector
```

## Troubleshooting

### Common Issues:

1. **"Character not found by name"**
   - Check GameObject names match exactly (case-sensitive)
   - Try using tags as fallback
   - Ensure GameObjects are active in scene

2. **"Waypoint not found"**
   - Verify waypoint GameObject names match `availablePositions`
   - Check for typos in names
   - Make sure waypoints are not disabled

3. **"EventTriggerSystem not found"**
   - Add EventTriggerSystem component to scene
   - Or manually assign reference in ScenarioLoader

4. **NPCs not moving to start positions**
   - Check if NPC GameObjects have Transform components
   - Verify start positions are valid coordinates
   - Enable detailed logging to see position assignments

### Debug Tips:

- Enable "Detailed Logging" in ScenarioLoader for verbose output
- Use "Validate Scenario Loading" context menu item
- Check Console for warning/error messages
- Use Scene view to verify GameObject positions

## Migration from Old System

If migrating from the old ScenarioData class:

1. Create SO_ScenarioData assets for existing scenarios
2. Replace ScenarioData references with SO_ScenarioData
3. Add ScenarioLoader to scenes
4. Update any code that directly accessed ScenarioData fields
5. Test scenario loading and NPC positioning

The new system is designed to be backward-compatible where possible, but some manual migration may be required for complex scenarios.
