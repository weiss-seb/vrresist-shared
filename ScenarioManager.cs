using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OVGU.VAR.VRResist
{

    /// <summary>
    /// Manages multiple study scenarios and handles scenario switching
    /// Provides easy way for developers to add new scenarios
    /// </summary>
    public class ScenarioManager : MonoBehaviour
    {
        [Header("Scenario Configuration")]
        [SerializeField] SO_ScenarioData[] scenarios = new SO_ScenarioData[5];

        [Header("Events")]
        public System.Action<SO_ScenarioData> OnScenarioChanged;

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
                scenarios = new SO_ScenarioData[5];
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
        SO_ScenarioData CreateDefaultScenario(int scenarioNumber)
        {
            var scenario = ScriptableObject.CreateInstance<SO_ScenarioData>();
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
        public SO_ScenarioData GetCurrentScenario()
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
        public SO_ScenarioData GetScenario(int index)
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
        void SendScenarioChangeToHMD(SO_ScenarioData scenario)
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
        public void AddScenario(SO_ScenarioData newScenario)
        {
            var newScenarios = new SO_ScenarioData[scenarios.Length + 1];
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
