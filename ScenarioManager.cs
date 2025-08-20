using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Data structure for a single study scenario
    /// Contains all NPC configurations, audio clips, and positions for one scenario
    /// </summary>
    [System.Serializable]
    public class ScenarioData
    {
        [Header("Scenario Information")]
        public int scenarioId;
        public string scenarioName;
        [TextArea(2, 4)]
        public string scenarioDescription;
        [TextArea(3, 6)]
        [Tooltip("Information text that will be displayed on the HMD during this scenario")]
        public string scenarioInfoText;

        [Header("Scene Configuration")]
        [Tooltip("Name of the Unity scene to load for this scenario")]
        public string sceneName;

        [Header("NPC Starting Positions")]
        public Vector3 chefarztStartPosition;
        public Vector3 kollegeStartPosition;
        public Vector3 patientStartPosition;

        [Header("Participant Starting Location")]
        public Vector3 participantStartPosition;

        [Header("Available Walk Positions")]
        public string[] availableWalkPositions = {
            "bedLeft1", "bedLeft2", "bedRight1", "doorInside", "doorOutside", "outside"
        };

        [Header("Chefarzt Audio Configuration")]
        [Tooltip("Audio clip names (identifiers) - actual AudioClip files are stored on HMD NPCController")]
        public string[] chefarztAudioClips;
        [Tooltip("German labels for UI buttons corresponding to audio clip names")]
        public string[] chefarztAudioLabels;

        [Header("Kollege Audio Configuration")]
        [Tooltip("Audio clip names (identifiers) - actual AudioClip files are stored on HMD NPCController")]
        public string[] kollegeAudioClips;
        [Tooltip("German labels for UI buttons corresponding to audio clip names")]
        public string[] kollegeAudioLabels;

        [Header("Patient Audio Configuration")]
        [Tooltip("Audio clip names (identifiers) - actual AudioClip files are stored on HMD NPCController")]
        public string[] patientAudioClips;
        [Tooltip("German labels for UI buttons corresponding to audio clip names")]
        public string[] patientAudioLabels;


        /// <summary>
        /// Get audio clips for a specific NPC
        /// </summary>
        public string[] GetAudioClipsForNPC(string npcName)
        {
            switch (npcName.ToLower())
            {
                case "chefarzt": return chefarztAudioClips;
                case "kollege": return kollegeAudioClips;
                case "patient": return patientAudioClips;
                default: return new string[0];
            }
        }

        /// <summary>
        /// Get audio labels for a specific NPC
        /// </summary>
        public string[] GetAudioLabelsForNPC(string npcName)
        {
            switch (npcName.ToLower())
            {
                case "chefarzt": return chefarztAudioLabels;
                case "kollege": return kollegeAudioLabels;
                case "patient": return patientAudioLabels;
                default: return new string[0];
            }
        }

        /// <summary>
        /// Get starting position for a specific NPC
        /// </summary>
        public Vector3 GetStartingPositionForNPC(string npcName)
        {
            switch (npcName.ToLower())
            {
                case "chefarzt": return chefarztStartPosition;
                case "kollege": return kollegeStartPosition;
                case "patient": return patientStartPosition;
                default: return Vector3.zero;
            }
        }
    }

    /// <summary>
    /// Manages multiple study scenarios and handles scenario switching
    /// Provides easy way for developers to add new scenarios
    /// </summary>
    public class ScenarioManager : MonoBehaviour
    {
        [Header("Scenario Configuration")]
        [SerializeField] ScenarioData[] scenarios = new ScenarioData[5];

        [Header("Events")]
        public System.Action<ScenarioData> OnScenarioChanged;

        [Header("Debug")]
        [SerializeField] bool enableDetailedLogging = true;

        private int currentScenarioIndex = 0;

        void Start()
        {
            InitializeDefaultScenarios();
        }

        /// <summary>
        /// Initialize default scenarios if not configured
        /// </summary>
        void InitializeDefaultScenarios()
        {
            if (scenarios == null || scenarios.Length == 0)
            {
                scenarios = new ScenarioData[5];
            }

            // Initialize empty scenarios if null
            for (int i = 0; i < scenarios.Length; i++)
            {
                if (scenarios[i] == null)
                {
                    scenarios[i] = CreateDefaultScenario(i + 1);
                }
            }

            if (enableDetailedLogging)
                Debug.Log($"[ScenarioManager] Initialized {scenarios.Length} scenarios");
        }

        /// <summary>
        /// Create a default scenario template
        /// </summary>
        ScenarioData CreateDefaultScenario(int scenarioNumber)
        {
            var scenario = new ScenarioData();
            scenario.scenarioId = scenarioNumber - 1; // 0-based ID
            scenario.scenarioName = $"Scenario {scenarioNumber}";
            scenario.scenarioDescription = $"Default scenario {scenarioNumber} - configure in ScenarioManager";
            scenario.scenarioInfoText = $"Sie befinden sich in Scenario {scenarioNumber}.\nBitte folgen Sie den Anweisungen des Studienleiters.";
            scenario.sceneName = $"Scenario{scenarioNumber}Scene";

            // Default starting positions (can be customized)
            scenario.chefarztStartPosition = new Vector3(0, 0, 0);
            scenario.kollegeStartPosition = new Vector3(2, 0, 0);
            scenario.patientStartPosition = new Vector3(-2, 0, 0);

            // Default audio clips (should be customized per scenario)
            scenario.chefarztAudioClips = new string[] {
                "begruessung", "behandlungFortfuehren", "patientBeruhigen"
            };
            scenario.chefarztAudioLabels = new string[] {
                "Begrüßung", "Behandlung fortführen", "Patient beruhigen"
            };

            scenario.kollegeAudioClips = new string[] {
                "begruessungKollege", "sorgeAeussern", "nachZustandFragen"
            };
            scenario.kollegeAudioLabels = new string[] {
                "Begrüßung", "Sorge äußern", "Nach Zustand fragen"
            };

            scenario.patientAudioClips = new string[] {
                "begruessungPatient", "emotionaleReaktion", "nachPrognoseFragen"
            };
            scenario.patientAudioLabels = new string[] {
                "Begrüßung", "Emotionale Reaktion", "Nach Prognose fragen"
            };

            return scenario;
        }

        /// <summary>
        /// Get current scenario
        /// </summary>
        public ScenarioData GetCurrentScenario()
        {
            if (scenarios != null && currentScenarioIndex >= 0 && currentScenarioIndex < scenarios.Length)
            {
                return scenarios[currentScenarioIndex];
            }
            return null;
        }

        /// <summary>
        /// Get scenario by index
        /// </summary>
        public ScenarioData GetScenario(int index)
        {
            if (scenarios != null && index >= 0 && index < scenarios.Length)
            {
                return scenarios[index];
            }
            return null;
        }

        /// <summary>
        /// Get all scenario names for dropdown population
        /// </summary>
        public string[] GetScenarioNames()
        {
            if (scenarios == null) return new string[0];

            string[] names = new string[scenarios.Length];
            for (int i = 0; i < scenarios.Length; i++)
            {
                names[i] = scenarios[i]?.scenarioName ?? $"Scenario {i + 1}";
            }
            return names;
        }

        /// <summary>
        /// Switch to a specific scenario by index
        /// </summary>
        public void SwitchToScenario(int scenarioIndex)
        {
            if (scenarios == null || scenarioIndex < 0 || scenarioIndex >= scenarios.Length)
            {
                Debug.LogError($"[ScenarioManager] Invalid scenario index: {scenarioIndex}");
                return;
            }

            currentScenarioIndex = scenarioIndex;
            var scenario = scenarios[scenarioIndex];

            if (enableDetailedLogging)
                Debug.Log($"[ScenarioManager] Switched to scenario: {scenario.scenarioName}");

            // Notify listeners about scenario change
            OnScenarioChanged?.Invoke(scenario);

            // Send scenario change message to HMD
            SendScenarioChangeToHMD(scenario);
        }

        /// <summary>
        /// Send scenario change message to HMD
        /// </summary>
        void SendScenarioChangeToHMD(ScenarioData scenario)
        {
            var webSocketClient = FindObjectOfType<WebSocketClient>();
            if (webSocketClient != null)
            {
                // Send scenario change message with starting positions
                var message = new EventMessage("SCENARIO_CHANGE", new string[] {
                    scenario.scenarioName,
                    JsonUtility.ToJson(scenario)
                });

                webSocketClient.SendEventMessage(message);

                if (enableDetailedLogging)
                    Debug.Log($"[ScenarioManager] Sent scenario change to HMD: {scenario.scenarioName}");
            }
        }

        /// <summary>
        /// Add a new scenario (for runtime scenario creation)
        /// </summary>
        public void AddScenario(ScenarioData newScenario)
        {
            var newScenarios = new ScenarioData[scenarios.Length + 1];
            for (int i = 0; i < scenarios.Length; i++)
            {
                newScenarios[i] = scenarios[i];
            }
            newScenarios[scenarios.Length] = newScenario;
            scenarios = newScenarios;

            if (enableDetailedLogging)
                Debug.Log($"[ScenarioManager] Added new scenario: {newScenario.scenarioName}");
        }

        /// <summary>
        /// Get total number of scenarios
        /// </summary>
        public int GetScenarioCount()
        {
            return scenarios?.Length ?? 0;
        }

        /// <summary>
        /// Validate all scenarios (check for missing data)
        /// </summary>
        [ContextMenu("Validate All Scenarios")]
        public void ValidateScenarios()
        {
            if (scenarios == null)
            {
                Debug.LogWarning("[ScenarioManager] No scenarios configured!");
                return;
            }

            for (int i = 0; i < scenarios.Length; i++)
            {
                var scenario = scenarios[i];
                if (scenario == null)
                {
                    Debug.LogWarning($"[ScenarioManager] Scenario {i} is null!");
                    continue;
                }

                Debug.Log($"[ScenarioManager] Scenario {i}: {scenario.scenarioName}");
                Debug.Log($"  - Chefarzt audio clips: {scenario.chefarztAudioClips?.Length ?? 0}");
                Debug.Log($"  - Kollege audio clips: {scenario.kollegeAudioClips?.Length ?? 0}");
                Debug.Log($"  - Patient audio clips: {scenario.patientAudioClips?.Length ?? 0}");
                Debug.Log($"  - Available positions: {scenario.availableWalkPositions?.Length ?? 0}");
            }
        }
    }
}
