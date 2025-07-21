# Step 9: Multi-Scenario Support System

## Overview

The VR study system now supports multiple scenarios with dynamic UI population through a dropdown selection interface. This allows researchers to easily switch between different study configurations without code changes.

## New Components

### 1. ScenarioManager.cs
**Purpose:** Manages multiple study scenarios and handles scenario switching

**Key Features:**
- **Configurable Scenarios:** Up to 5 scenarios (easily expandable)
- **Starting Positions:** Each scenario defines NPC starting positions
- **Audio Configuration:** Scenario-specific audio clips and labels for each NPC
- **Dynamic Updates:** Sends scenario changes to HMD via WebSocket
- **Developer-Friendly:** Easy to add new scenarios through Unity Inspector

**Configuration:**
```csharp
[Header("Scenario Configuration")]
[SerializeField] ScenarioData[] scenarios = new ScenarioData[5];

[Header("Events")]
public System.Action<ScenarioData> OnScenarioChanged;
```

### 2. ScenarioData Structure
**Purpose:** Data container for a single study scenario

**Contains:**
- **Scenario Information:** Name and description
- **NPC Starting Positions:** Vector3 positions for all NPCs
- **Available Walk Positions:** Array of position names
- **Audio Configuration:** Audio clip names (identifiers) and German labels for each NPC

**Important:** Audio clip names are identifiers only - actual AudioClip files are stored on the HMD in NPCController components. The tablet sends only the clip names, and the HMD plays the corresponding audio files.

**Supported NPCs:**
- Chefarzt (Chief Doctor)
- Kollege (Colleague)
- Patient
- Doctor
- Anesthesiologist

### 3. Enhanced TabletEventControl.cs
**Purpose:** Tablet interface with scenario-aware UI population

**New Features:**
- **Scenario Dropdown:** TMP_Dropdown for scenario selection
- **Dynamic UI Updates:** UI buttons update based on selected scenario
- **Scenario Subscription:** Listens to ScenarioManager events
- **Button Reconfiguration:** Automatically updates button listeners and labels

## Workflow

### 1. Scenario Selection
```
User selects scenario from dropdown
↓
ScenarioManager.SwitchToScenario(index)
↓
OnScenarioChanged event fired
↓
TabletEventControl.OnScenarioChanged()
↓
UI updated with new scenario data
```

### 2. UI Population Process
```
Scenario changed
↓
Update audio clips arrays
↓
Update available positions
↓
Clear existing button listeners
↓
Re-setup all buttons with new data
↓
Update button text labels
```

### 3. HMD Communication
```
Scenario selected on tablet
↓
SCENARIO_CHANGE message sent to HMD
↓
MessageHandler.HandleScenarioChange()
↓
NPCs moved to starting positions
```

## Setup Instructions

### For Tablet Scene:

1. **Add ScenarioManager Component:**
   ```
   GameObject → Create Empty → "ScenarioManager"
   Add Component → ScenarioManager
   ```

2. **Configure Scenarios in Inspector:**
   ```
   Scenarios (Array Size: 5)
   ├── Scenario 0: Emergency Room
   │   ├── Scenario Name: "Notaufnahme"
   │   ├── Scenario Description: "Emergency room scenario"
   │   ├── Chefarzt Start Position: (0, 0, 0)
   │   ├── Kollege Start Position: (2, 0, 0)
   │   ├── Patient Start Position: (-2, 0, 0)
   │   ├── Audio Clips: ["begruessung", "diagnose", ...]
   │   └── Audio Labels: ["Begrüßung", "Diagnose", ...]
   └── ... (4 more scenarios)
   ```

3. **Setup TabletEventControl:**
   ```
   [Header("System References")]
   Web Socket Client: Drag WebSocketClient GameObject
   Scenario Manager: Drag ScenarioManager GameObject
   
   [Header("Scenario Selection")]
   Scenario Dropdown: Drag TMP_Dropdown from UI
   ```

4. **Create UI Dropdown:**
   ```
   UI → Dropdown - TextMeshPro
   Configure dropdown styling
   Assign to TabletEventControl.scenarioDropdown
   ```

### For HMD Scene:

1. **Update MessageHandler:**
   - Already supports SCENARIO_CHANGE messages
   - Automatically moves NPCs to starting positions
   - No additional setup required

## Audio Architecture

### How Audio Works in the System

**🎵 Audio Storage:** All actual AudioClip files are stored on the HMD side in each NPC's NPCController component.

**📱 Tablet Role:** The tablet only stores and sends audio clip names (identifiers) - never the actual audio files.

**🔄 Communication Flow:**
```
1. Tablet button clicked → sends audio clip name (e.g., "begruessung")
2. HMD receives NPC_TALK message with clip name
3. NPCController.speak(clipName) finds AudioClip by name
4. Audio plays on HMD with proper 3D positioning
```

**📋 Scenario Configuration:**
- `chefarztAudioClips`: Array of clip names (e.g., ["begruessung", "diagnose"])
- `chefarztAudioLabels`: German UI labels (e.g., ["Begrüßung", "Diagnose"])
- Clip names must match AudioClip.name in NPCController.audioClips list

**✅ Benefits:**
- **Performance:** No audio data transmitted over network
- **Quality:** Full-quality audio files stay on HMD
- **Flexibility:** Easy to change audio files without updating tablet
- **Scalability:** Unlimited audio clips per scenario

## Message Protocol

### Tablet → HMD Messages

**Scenario Change:**
```json
{
  "type": "SCENARIO_CHANGE",
  "content": [
    "Scenario Name",
    "{\"scenarioName\":\"...\",\"chefarztStartPosition\":{...},...}"
  ]
}
```

**NPC Commands (unchanged):**
```json
{
  "type": "NPC_WALK",
  "content": ["chefarzt", "bedLeft1"]
}

{
  "type": "NPC_TALK", 
  "content": ["chefarzt", "begruessung"]
}
```

## Scenario Configuration Examples

### Scenario 1: Emergency Room
```csharp
scenarioName = "Notaufnahme";
scenarioDescription = "Emergency room with critical patient";

// Starting positions
chefarztStartPosition = new Vector3(0, 0, 0);
kollegeStartPosition = new Vector3(2, 0, 0);
patientStartPosition = new Vector3(-2, 0, 0);

// Audio clips
chefarztAudioClips = {
    "begruessung", "diagnoseStellen", "behandlungEinleiten"
};
chefarztAudioLabels = {
    "Begrüßung", "Diagnose stellen", "Behandlung einleiten"
};
```

### Scenario 2: Surgery Preparation
```csharp
scenarioName = "OP-Vorbereitung";
scenarioDescription = "Pre-operative preparation scenario";

// Starting positions
chefarztStartPosition = new Vector3(1, 0, 1);
kollegeStartPosition = new Vector3(-1, 0, 1);
patientStartPosition = new Vector3(0, 0, -1);
anesthesiologistStartPosition = new Vector3(0, 0, 2);

// Audio clips
chefarztAudioClips = {
    "opVorbereitung", "patientAufklaerung", "teamBriefing"
};
chefarztAudioLabels = {
    "OP-Vorbereitung", "Patientenaufklärung", "Team-Briefing"
};
```

## Developer Guide

### Adding New Scenarios

1. **Increase Array Size:**
   ```csharp
   [SerializeField] ScenarioData[] scenarios = new ScenarioData[6]; // Increase from 5 to 6
   ```

2. **Configure in Inspector:**
   - Set scenario name and description
   - Define NPC starting positions
   - Configure audio clips and labels
   - Set available walk positions

3. **Test Scenario:**
   - Run tablet scene
   - Select new scenario from dropdown
   - Verify UI updates correctly
   - Test HMD communication

### Adding New NPCs

1. **Update ScenarioData:**
   ```csharp
   [Header("New NPC Audio Configuration")]
   public string[] newNPCAudioClips;
   public string[] newNPCAudioLabels;
   public Vector3 newNPCStartPosition;
   ```

2. **Update Helper Methods:**
   ```csharp
   public string[] GetAudioClipsForNPC(string npcName)
   {
       switch (npcName.ToLower())
       {
           case "newnpc": return newNPCAudioClips;
           // ... existing cases
       }
   }
   ```

3. **Update TabletEventControl:**
   ```csharp
   [Header("New NPC Action Buttons")]
   [SerializeField] Button[] newNPCWalkButtons;
   [SerializeField] Button[] newNPCTalkButtons;
   ```

### Customizing Audio Clips

1. **Per-Scenario Configuration:**
   ```csharp
   // Scenario 1: Formal medical language
   chefarztAudioClips = {"begruessung", "diagnose", "behandlung"};
   
   // Scenario 2: Emergency situation
   chefarztAudioClips = {"schnelleBegruessung", "notfallDiagnose", "sofortBehandlung"};
   ```

2. **Dynamic Audio Loading:**
   ```csharp
   // Future enhancement: Load audio clips from Resources folder
   string[] LoadAudioClipsForScenario(string scenarioName, string npcName)
   {
       return Resources.LoadAll<AudioClip>($"Audio/{scenarioName}/{npcName}")
                      .Select(clip => clip.name)
                      .ToArray();
   }
   ```

## Testing Checklist

### Scenario Manager Testing:
- [ ] All 5 scenarios configured with unique names
- [ ] Starting positions set for all NPCs in each scenario
- [ ] Audio clips and labels configured for each NPC
- [ ] Scenario validation passes (use context menu)
- [ ] OnScenarioChanged event fires correctly

### Tablet UI Testing:
- [ ] Dropdown populates with scenario names
- [ ] Scenario selection triggers UI update
- [ ] Button labels update to German text
- [ ] Button listeners reconfigure correctly
- [ ] WebSocket messages sent to HMD

### HMD Integration Testing:
- [ ] SCENARIO_CHANGE messages received
- [ ] NPCs move to correct starting positions
- [ ] Audio clips work with new scenario data
- [ ] No errors in console during scenario switch

### Multi-Scenario Testing:
- [ ] Switch between all 5 scenarios
- [ ] UI updates correctly for each scenario
- [ ] NPC commands work in all scenarios
- [ ] Starting positions are scenario-specific
- [ ] Audio clips are scenario-appropriate

## Performance Considerations

### Memory Management:
- **Scenario Data:** Stored in serialized arrays (minimal memory impact)
- **UI Updates:** Only update when scenario changes
- **Button Listeners:** Properly cleared and reassigned
- **WebSocket Messages:** Minimal JSON payload

### Optimization Tips:
- **Lazy Loading:** Load audio clips only when needed
- **Caching:** Cache frequently used scenario data
- **Batch Updates:** Update UI elements in batches
- **Memory Cleanup:** Dispose unused resources

## Troubleshooting

### Common Issues:

1. **Dropdown Not Populating:**
   - Check ScenarioManager assignment in TabletEventControl
   - Verify scenarios array is configured
   - Check for null scenario names

2. **UI Not Updating:**
   - Verify OnScenarioChanged subscription
   - Check button array assignments
   - Ensure UpdateUIForScenario is called

3. **HMD Not Receiving Messages:**
   - Check WebSocketClient connection
   - Verify SCENARIO_CHANGE message format
   - Check MessageHandler namespace

4. **NPCs Not Moving:**
   - Verify starting positions are not Vector3.zero
   - Check NPC GameObject assignments in MessageHandler
   - Ensure HandleScenarioChange is called

### Debug Tools:

1. **Enable Detailed Logging:**
   ```csharp
   enableDetailedLogging = true; // in ScenarioManager and TabletEventControl
   ```

2. **Scenario Validation:**
   ```csharp
   [ContextMenu("Validate All Scenarios")]
   public void ValidateScenarios() // in ScenarioManager
   ```

3. **Monitor WebSocket Messages:**
   - Check Unity Console for sent/received messages
   - Verify JSON format and content

## Future Enhancements

### Planned Features:
1. **Scenario Recording:** Record and replay scenario sessions
2. **Dynamic Scenario Loading:** Load scenarios from external files
3. **Scenario Templates:** Pre-built scenario templates
4. **Multi-language Support:** Switch between German and English
5. **Scenario Analytics:** Track scenario usage and performance
6. **Custom Scenario Builder:** Visual scenario creation tool

### Advanced Configurations:
1. **Conditional Logic:** Scenario-specific event triggers
2. **Time-based Events:** Scheduled scenario events
3. **Interactive Elements:** Scenario-specific interactive objects
4. **Environmental Changes:** Lighting, sound, atmosphere per scenario
5. **Difficulty Scaling:** Adaptive scenario complexity

## Architecture Benefits

### ✅ **Flexibility:**
- Easy scenario switching without code changes
- Configurable through Unity Inspector
- Extensible for new scenarios and NPCs

### ✅ **User Experience:**
- Intuitive dropdown interface
- German localization
- Real-time UI updates

### ✅ **Maintainability:**
- Clean separation of scenario data
- Event-driven architecture
- Comprehensive error handling

### ✅ **Scalability:**
- Support for unlimited scenarios
- Easy NPC addition
- Modular component design

## Conclusion

The multi-scenario support system provides a robust foundation for conducting varied VR studies. Researchers can easily configure different scenarios through the Unity Inspector and switch between them using an intuitive tablet interface. The system maintains clean architecture principles while providing the flexibility needed for diverse research requirements.

**Status: SCENARIO SYSTEM COMPLETE** ✅

The system now supports dynamic scenario switching with:
- 5 configurable scenarios
- Scenario-specific NPC starting positions
- Dynamic UI population based on selected scenario
- Seamless HMD communication
- German localized interface
- Developer-friendly configuration tools
