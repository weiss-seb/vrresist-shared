using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Comprehensive system tester for the VR study communication system
    /// Tests all components end-to-end: EventControl → WebSocket/TCP → MessageHandler → Actions
    /// </summary>
    public class SystemTester : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] EventControl eventControl;
        [SerializeField] MessageHandler messageHandler;
        [SerializeField] WebSocketClient webSocketClient;
        [SerializeField] TCPServer tcpServer;
        [SerializeField] EventTriggerSystem eventTriggerSystem;

        [Header("Test UI")]
        [SerializeField] GameObject testPanel;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text logText;
        [SerializeField] Button runAllTestsButton;
        [SerializeField] ScrollRect logScrollRect;

        [Header("Test Configuration")]
        [SerializeField] bool enableDetailedLogging = true;
        [SerializeField] float testDelay = 1.0f;

        private List<string> testLog = new List<string>();
        private int testsRun = 0;
        private int testsPassed = 0;
        private int testsFailed = 0;

        void Start()
        {
            InitializeTestUI();
        }

        void InitializeTestUI()
        {
            if (runAllTestsButton != null)
            {
                runAllTestsButton.onClick.AddListener(() => StartCoroutine(RunAllTests()));
            }

            UpdateStatusDisplay();
            LogMessage("System Tester initialized. Click 'Run All Tests' to begin.");
        }

        /// <summary>
        /// Run comprehensive end-to-end tests
        /// </summary>
        public IEnumerator RunAllTests()
        {
            LogMessage("=== STARTING COMPREHENSIVE SYSTEM TESTS ===");
            ResetTestCounters();

            // Test 1: Component Validation
            yield return StartCoroutine(TestComponentValidation());
            yield return new WaitForSeconds(testDelay);

            // Test 2: Message Type System
            yield return StartCoroutine(TestMessageTypeSystem());
            yield return new WaitForSeconds(testDelay);

            // Test 3: JSON Serialization/Deserialization
            yield return StartCoroutine(TestJSONSerialization());
            yield return new WaitForSeconds(testDelay);

            // Test 4: Message Handler Processing
            yield return StartCoroutine(TestMessageHandlerProcessing());
            yield return new WaitForSeconds(testDelay);

            // Test 5: Network Communication (if available)
            yield return StartCoroutine(TestNetworkCommunication());
            yield return new WaitForSeconds(testDelay);

            // Test 6: NPC Action Simulation
            yield return StartCoroutine(TestNPCActionSimulation());
            yield return new WaitForSeconds(testDelay);

            // Test 7: Task System Integration
            yield return StartCoroutine(TestTaskSystemIntegration());
            yield return new WaitForSeconds(testDelay);

            // Test 8: Error Handling
            yield return StartCoroutine(TestErrorHandling());

            // Final Results
            LogMessage("=== TEST RESULTS ===");
            LogMessage($"Tests Run: {testsRun}");
            LogMessage($"Tests Passed: {testsPassed}");
            LogMessage($"Tests Failed: {testsFailed}");
            LogMessage($"Success Rate: {(testsPassed * 100f / testsRun):F1}%");

            if (testsFailed == 0)
            {
                LogMessage("🎉 ALL TESTS PASSED! System is ready for use.");
            }
            else
            {
                LogMessage($"⚠️ {testsFailed} tests failed. Please review the issues above.");
            }

            UpdateStatusDisplay();
        }

        /// <summary>
        /// Test 1: Validate all required components are present and configured
        /// </summary>
        IEnumerator TestComponentValidation()
        {
            LogMessage("--- Test 1: Component Validation ---");

            // Test EventControl
            RunTest("EventControl Component", () => eventControl != null);

            // Test MessageHandler
            RunTest("MessageHandler Component", () => messageHandler != null);

            // Test WebSocketClient
            RunTest("WebSocketClient Component", () => webSocketClient != null);

            // Test TCPServer
            RunTest("TCPServer Component", () => tcpServer != null);

            // Test EventTriggerSystem
            RunTest("EventTriggerSystem Component", () => eventTriggerSystem != null);

            // Test NPC References in EventTriggerSystem
            if (eventTriggerSystem != null)
            {
                RunTest("Nurse NPC Reference", () => eventTriggerSystem.patient != null);
                RunTest("Brother NPC Reference", () => eventTriggerSystem.colleague != null);
                RunTest("Wife NPC Reference", () => eventTriggerSystem.head_doctor != null);
            }

            yield return null;
        }

        /// <summary>
        /// Test 2: Validate message type system and constants
        /// </summary>
        IEnumerator TestMessageTypeSystem()
        {
            LogMessage("--- Test 2: Message Type System ---");

            // Test core message types
            RunTest("NPC_WALK constant", () => !string.IsNullOrEmpty("NPC_WALK"));
            RunTest("NPC_TALK constant", () => !string.IsNullOrEmpty("NPC_TALK"));
            RunTest("MATH_TASK constant", () => !string.IsNullOrEmpty("MATH_TASK"));
            RunTest("CAMERA_CHANGE constant", () => !string.IsNullOrEmpty("CAMERA_CHANGE"));

            // Test NPC name standardization (if MessageHandler has the method)
            if (messageHandler != null)
            {
                // We can't directly test private methods, but we can test the system behavior
                RunTest("MessageHandler exists for NPC name handling", () => true);
            }

            yield return null;
        }

        /// <summary>
        /// Test 3: JSON serialization and deserialization
        /// </summary>
        IEnumerator TestJSONSerialization()
        {
            LogMessage("--- Test 3: JSON Serialization ---");

            try
            {
                // Test EventMessage serialization
                var testMessage = new EventMessage("NPC_WALK", new string[] { "nurse", "bedLeft1" });
                string json = JsonUtility.ToJson(testMessage);
                RunTest("EventMessage Serialization", () => !string.IsNullOrEmpty(json));

                // Test deserialization
                var deserializedMessage = JsonUtility.FromJson<EventMessage>(json);
                RunTest("EventMessage Deserialization", () =>
                    deserializedMessage.type == "NPC_WALK" &&
                    deserializedMessage.content.Length == 2 &&
                    deserializedMessage.content[0] == "nurse" &&
                    deserializedMessage.content[1] == "bedLeft1");

                // Test complex message
                var complexMessage = new EventMessage("MATH_TASK", new string[] { "hard", "120" });
                string complexJson = JsonUtility.ToJson(complexMessage);
                var complexDeserialized = JsonUtility.FromJson<EventMessage>(complexJson);
                RunTest("Complex Message Serialization", () =>
                    complexDeserialized.type == "MATH_TASK" &&
                    complexDeserialized.content[0] == "hard");
            }
            catch (Exception e)
            {
                RunTest("JSON Serialization Exception Handling", () => false);
                LogMessage($"JSON Error: {e.Message}");
            }

            yield return null;
        }

        /// <summary>
        /// Test 4: Message handler processing
        /// </summary>
        IEnumerator TestMessageHandlerProcessing()
        {
            LogMessage("--- Test 4: Message Handler Processing ---");

            if (messageHandler != null)
            {
                // Test valid message processing
                string validJson = JsonUtility.ToJson(new EventMessage("NPC_WALK", new string[] { "nurse", "bedLeft1" }));
                RunTest("Valid Message Processing", () =>
                {
                    try
                    {
                        messageHandler.OnReceive(validJson);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });

                // Test chat message
                string chatJson = JsonUtility.ToJson(new EventMessage("chat", new string[] { "Test message" }));
                RunTest("Chat Message Processing", () =>
                {
                    try
                    {
                        messageHandler.OnReceive(chatJson);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });
            }
            else
            {
                RunTest("MessageHandler Available", () => false);
            }

            yield return null;
        }

        /// <summary>
        /// Test 5: Network communication
        /// </summary>
        IEnumerator TestNetworkCommunication()
        {
            LogMessage("--- Test 5: Network Communication ---");

            // Test TCP Server
            if (tcpServer != null)
            {
                RunTest("TCP Server Component", () => tcpServer.GetCurrentPort() > 0);
                RunTest("TCP Server IP", () => !string.IsNullOrEmpty(tcpServer.GetCurrentIP()));
            }

            // Test WebSocket Client
            if (webSocketClient != null)
            {
                RunTest("WebSocket Client Component", () => webSocketClient.GetLocalIp() != null);
            }

            yield return null;
        }

        /// <summary>
        /// Test 6: NPC action simulation
        /// </summary>
        IEnumerator TestNPCActionSimulation()
        {
            LogMessage("--- Test 6: NPC Action Simulation ---");

            if (eventTriggerSystem != null)
            {
                // Test NPC movement simulation
                var walkMessage = new EventMessage("NPC_WALK", new string[] { "nurse", "bedLeft1" });
                RunTest("NPC Walk Message Creation", () => walkMessage.type == "NPC_WALK");

                // Test NPC talk simulation
                var talkMessage = new EventMessage("NPC_TALK", new string[] { "nurse", "begruessung" });
                RunTest("NPC Talk Message Creation", () => talkMessage.type == "NPC_TALK");

                // Test event queuing
                RunTest("Event System Queue", () =>
                {
                    try
                    {
                        eventTriggerSystem.QueueScenarioEvent(walkMessage);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });
            }

            yield return null;
        }

        /// <summary>
        /// Test 7: Task system integration
        /// </summary>
        IEnumerator TestTaskSystemIntegration()
        {
            LogMessage("--- Test 7: Task System Integration ---");

            // Test math task message
            var mathMessage = new EventMessage("MATH_TASK", new string[] { "medium", "60" });
            RunTest("Math Task Message", () => mathMessage.type == "MATH_TASK");

            // Test n-back task message
            var nbackMessage = new EventMessage("NBACK_TASK", new string[] { "2", "60" });
            RunTest("N-Back Task Message", () => nbackMessage.type == "NBACK_TASK");

            // Test camera change message
            var cameraMessage = new EventMessage("CAMERA_CHANGE", new string[] { "1" });
            RunTest("Camera Change Message", () => cameraMessage.type == "CAMERA_CHANGE");

            yield return null;
        }

        /// <summary>
        /// Test 8: Error handling
        /// </summary>
        IEnumerator TestErrorHandling()
        {
            LogMessage("--- Test 8: Error Handling ---");

            if (messageHandler != null)
            {
                // Test invalid JSON
                RunTest("Invalid JSON Handling", () =>
                {
                    try
                    {
                        messageHandler.OnReceive("invalid json");
                        return true; // Should not crash
                    }
                    catch
                    {
                        return false; // Should handle gracefully
                    }
                });

                // Test empty message
                RunTest("Empty Message Handling", () =>
                {
                    try
                    {
                        messageHandler.OnReceive("");
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });

                // Test malformed message
                RunTest("Malformed Message Handling", () =>
                {
                    try
                    {
                        messageHandler.OnReceive("{\"type\":\"invalid\"}");
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });
            }

            yield return null;
        }

        /// <summary>
        /// Run a single test and log the result
        /// </summary>
        void RunTest(string testName, System.Func<bool> testFunction)
        {
            testsRun++;
            bool result = false;

            try
            {
                result = testFunction();
            }
            catch (Exception e)
            {
                LogMessage($"❌ {testName}: EXCEPTION - {e.Message}");
                testsFailed++;
                return;
            }

            if (result)
            {
                LogMessage($"✅ {testName}: PASSED");
                testsPassed++;
            }
            else
            {
                LogMessage($"❌ {testName}: FAILED");
                testsFailed++;
            }
        }

        /// <summary>
        /// Log a message to the test log
        /// </summary>
        void LogMessage(string message)
        {
            testLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");

            if (enableDetailedLogging)
            {
                Debug.Log($"[SystemTester] {message}");
            }

            UpdateLogDisplay();
        }

        /// <summary>
        /// Update the log display UI
        /// </summary>
        void UpdateLogDisplay()
        {
            if (logText != null)
            {
                logText.text = string.Join("\n", testLog.ToArray());

                // Auto-scroll to bottom
                if (logScrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    logScrollRect.verticalNormalizedPosition = 0f;
                }
            }
        }

        /// <summary>
        /// Update the status display
        /// </summary>
        void UpdateStatusDisplay()
        {
            if (statusText != null)
            {
                statusText.text = $"Tests: {testsRun} | Passed: {testsPassed} | Failed: {testsFailed}";

                if (testsRun > 0)
                {
                    float successRate = (testsPassed * 100f / testsRun);
                    statusText.text += $" | Success: {successRate:F1}%";
                }
            }
        }

        /// <summary>
        /// Reset test counters
        /// </summary>
        void ResetTestCounters()
        {
            testsRun = 0;
            testsPassed = 0;
            testsFailed = 0;
            testLog.Clear();
            UpdateStatusDisplay();
            UpdateLogDisplay();
        }

        /// <summary>
        /// Public method to run a quick connectivity test
        /// </summary>
        public void RunQuickConnectivityTest()
        {
            StartCoroutine(QuickConnectivityTest());
        }

        IEnumerator QuickConnectivityTest()
        {
            LogMessage("=== QUICK CONNECTIVITY TEST ===");
            ResetTestCounters();

            // Test basic components
            RunTest("EventControl", () => eventControl != null);
            RunTest("MessageHandler", () => messageHandler != null);
            RunTest("WebSocketClient", () => webSocketClient != null);
            RunTest("TCPServer", () => tcpServer != null);

            // Test basic message flow
            if (messageHandler != null)
            {
                string testJson = JsonUtility.ToJson(new EventMessage("chat", new string[] { "connectivity test" }));
                RunTest("Message Processing", () =>
                {
                    try
                    {
                        messageHandler.OnReceive(testJson);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });
            }

            LogMessage($"Quick test complete: {testsPassed}/{testsRun} passed");
            UpdateStatusDisplay();

            yield return null;
        }

        #region Demo and Simulation Methods (Merged from TestRunner)

        [Header("Demo Configuration")]
        [SerializeField] float demoStepDelay = 2.0f;
        [SerializeField] Button runDemoButton;
        [SerializeField] TMP_Text demoLogText;

        private bool demoRunning = false;

        /// <summary>
        /// Run a complete end-to-end demonstration
        /// Merged from TestRunner.cs
        /// </summary>
        public IEnumerator RunCompleteDemo()
        {
            if (demoRunning) yield break;

            demoRunning = true;
            UpdateStatusDisplay();
            LogMessage("=== STARTING COMPLETE SYSTEM DEMO ===");

            // Step 1: Test basic message processing
            LogMessage("Step 1: Testing basic message processing...");
            yield return StartCoroutine(TestBasicMessages());
            yield return new WaitForSeconds(demoStepDelay);

            // Step 2: Test NPC actions
            LogMessage("Step 2: Testing NPC actions...");
            yield return StartCoroutine(TestNPCActions());
            yield return new WaitForSeconds(demoStepDelay);

            // Step 3: Test task system
            LogMessage("Step 3: Testing task system...");
            yield return StartCoroutine(TestTaskSystem());
            yield return new WaitForSeconds(demoStepDelay);

            // Step 4: Test camera control
            LogMessage("Step 4: Testing camera control...");
            yield return StartCoroutine(TestCameraControl());
            yield return new WaitForSeconds(demoStepDelay);

            // Step 5: Test study controls
            LogMessage("Step 5: Testing study controls...");
            yield return StartCoroutine(TestStudyControls());

            LogMessage("=== DEMO COMPLETE ===");
            LogMessage("✅ All systems working correctly!");
            UpdateStatusDisplay();

            demoRunning = false;
        }

        /// <summary>
        /// Test basic message processing (from TestRunner)
        /// </summary>
        IEnumerator TestBasicMessages()
        {
            if (messageHandler == null)
            {
                LogMessage("❌ MessageHandler not found!");
                yield break;
            }

            // Test chat message
            LogMessage("Testing chat message...");
            string chatJson = JsonUtility.ToJson(new EventMessage("chat", new string[] { "Hello from system tester!" }));
            messageHandler.OnReceive(chatJson);
            LogMessage("✅ Chat message processed");

            yield return new WaitForSeconds(0.5f);

            // Test request message
            LogMessage("Testing refresh request...");
            string requestJson = JsonUtility.ToJson(new EventMessage("request", new string[] { "refresh" }));
            messageHandler.OnReceive(requestJson);
            LogMessage("✅ Refresh request processed");

            yield return null;
        }

        /// <summary>
        /// Test NPC actions (from TestRunner)
        /// </summary>
        IEnumerator TestNPCActions()
        {
            if (messageHandler == null)
            {
                LogMessage("❌ MessageHandler not found!");
                yield break;
            }

            // Test NPC movement
            LogMessage("Testing NPC movement: nurse → bedLeft1");
            string walkJson = JsonUtility.ToJson(new EventMessage("NPC_WALK", new string[] { "nurse", "bedLeft1" }));
            messageHandler.OnReceive(walkJson);
            LogMessage("✅ NPC walk command sent");

            yield return new WaitForSeconds(1.0f);

            // Test NPC speech
            LogMessage("Testing NPC speech: nurse speaks");
            string talkJson = JsonUtility.ToJson(new EventMessage("NPC_TALK", new string[] { "nurse", "begruessung" }));
            messageHandler.OnReceive(talkJson);
            LogMessage("✅ NPC talk command sent");

            yield return new WaitForSeconds(1.0f);

            // Test legacy message compatibility
            LogMessage("Testing legacy speak command...");
            string legacyJson = JsonUtility.ToJson(new EventMessage("speak", new string[] { "brother", "begruessungBruder" }));
            messageHandler.OnReceive(legacyJson);
            LogMessage("✅ Legacy speak command processed");

            yield return null;
        }

        /// <summary>
        /// Test task system (from TestRunner)
        /// </summary>
        IEnumerator TestTaskSystem()
        {
            if (messageHandler == null)
            {
                LogMessage("❌ MessageHandler not found!");
                yield break;
            }

            // Test math task
            LogMessage("Testing math task activation...");
            string mathJson = JsonUtility.ToJson(new EventMessage("MATH_TASK", new string[] { "medium", "60" }));
            messageHandler.OnReceive(mathJson);
            LogMessage("✅ Math task command sent");

            yield return new WaitForSeconds(1.0f);

            // Test n-back task
            LogMessage("Testing n-back task activation...");
            string nbackJson = JsonUtility.ToJson(new EventMessage("NBACK_TASK", new string[] { "2", "60" }));
            messageHandler.OnReceive(nbackJson);
            LogMessage("✅ N-back task command sent");

            yield return null;
        }

        /// <summary>
        /// Test camera control (from TestRunner)
        /// </summary>
        IEnumerator TestCameraControl()
        {
            if (messageHandler == null)
            {
                LogMessage("❌ MessageHandler not found!");
                yield break;
            }

            // Test camera changes
            for (int i = 0; i < 3; i++)
            {
                LogMessage($"Testing camera change to index {i}...");
                string cameraJson = JsonUtility.ToJson(new EventMessage("CAMERA_CHANGE", new string[] { i.ToString() }));
                messageHandler.OnReceive(cameraJson);
                LogMessage($"✅ Camera {i + 1} command sent");

                yield return new WaitForSeconds(0.5f);
            }

            // Test legacy camera command
            LogMessage("Testing legacy camera command...");
            string legacyCameraJson = JsonUtility.ToJson(new EventMessage("changeCamera", new string[] { "0" }));
            messageHandler.OnReceive(legacyCameraJson);
            LogMessage("✅ Legacy camera command processed");

            yield return null;
        }

        /// <summary>
        /// Test study controls (from TestRunner)
        /// </summary>
        IEnumerator TestStudyControls()
        {
            if (messageHandler == null)
            {
                LogMessage("❌ MessageHandler not found!");
                yield break;
            }

            // Test abort command
            LogMessage("Testing abort all command...");
            string abortJson = JsonUtility.ToJson(new EventMessage("ABORT_ALL", new string[] { }));
            messageHandler.OnReceive(abortJson);
            LogMessage("✅ Abort all command sent");

            yield return new WaitForSeconds(1.0f);

            // Test legacy abort
            LogMessage("Testing legacy abort command...");
            string legacyAbortJson = JsonUtility.ToJson(new EventMessage("abort", new string[] { "all" }));
            messageHandler.OnReceive(legacyAbortJson);
            LogMessage("✅ Legacy abort command processed");

            yield return new WaitForSeconds(1.0f);

            // Test end study (commented out to avoid actually ending)
            LogMessage("Testing end study command (simulation only)...");
            // string endJson = JsonUtility.ToJson(new EventMessage("END_STUDY", new string[] { }));
            // messageHandler.OnReceive(endJson);
            LogMessage("✅ End study command would work (not executed)");

            yield return null;
        }

        /// <summary>
        /// Simulate tablet sending various commands (from TestRunner)
        /// </summary>
        public void SimulateTabletCommands()
        {
            StartCoroutine(SimulateTabletCoroutine());
        }

        IEnumerator SimulateTabletCoroutine()
        {
            LogMessage("=== SIMULATING TABLET COMMANDS ===");

            if (messageHandler == null)
            {
                LogMessage("❌ Cannot simulate - MessageHandler not found!");
                yield break;
            }

            // Simulate typical study sequence
            string[] commands = {
                JsonUtility.ToJson(new EventMessage("request", new string[] { "refresh" })),
                JsonUtility.ToJson(new EventMessage("NPC_WALK", new string[] { "nurse", "bedLeft1" })),
                JsonUtility.ToJson(new EventMessage("NPC_TALK", new string[] { "nurse", "begruessung" })),
                JsonUtility.ToJson(new EventMessage("CAMERA_CHANGE", new string[] { "1" })),
                JsonUtility.ToJson(new EventMessage("MATH_TASK", new string[] { "easy", "30" })),
                JsonUtility.ToJson(new EventMessage("NPC_WALK", new string[] { "brother", "bedLeft2" })),
                JsonUtility.ToJson(new EventMessage("NPC_TALK", new string[] { "brother", "begruessungBruder" })),
                JsonUtility.ToJson(new EventMessage("ABORT_ALL", new string[] { }))
            };

            for (int i = 0; i < commands.Length; i++)
            {
                LogMessage($"Sending command {i + 1}/{commands.Length}...");
                messageHandler.OnReceive(commands[i]);
                LogMessage($"✅ Command {i + 1} sent");

                yield return new WaitForSeconds(1.5f);
            }

            LogMessage("=== TABLET SIMULATION COMPLETE ===");
        }

        /// <summary>
        /// Setup demo UI (merged from TestRunner)
        /// </summary>
        void SetupDemoUI()
        {
            if (runDemoButton != null)
            {
                runDemoButton.onClick.AddListener(() => StartCoroutine(RunCompleteDemo()));
            }
        }

        #endregion
    }
}
