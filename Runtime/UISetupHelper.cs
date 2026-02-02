using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Helper script to automate UI setup for Task Display System and Event Control
    /// This script provides utilities to create and configure UI elements programmatically
    /// </summary>
    public class UISetupHelper : MonoBehaviour
    {
        [Header("Auto-Setup Configuration")]
        [SerializeField] bool autoSetupOnStart = false;
        [SerializeField] Canvas targetCanvas;

        [Header("Task Display Setup")]
        [SerializeField] bool createTaskDisplayUI = true;
        [SerializeField] Vector2 taskCanvasPosition = new Vector2(100, -100);
        [SerializeField] Vector2 taskCanvasSize = new Vector2(400, 300);

        [Header("Event Control Setup")]
        [SerializeField] bool createEventControlUI = true;
        [SerializeField] Vector2 eventControlPosition = new Vector2(-400, 0);
        [SerializeField] Vector2 eventControlSize = new Vector2(300, 600);

        [Header("Button Configuration")]
        [SerializeField] Vector2 buttonSize = new Vector2(120, 30);
        [SerializeField] float buttonSpacing = 35f;
        [SerializeField] Color buttonNormalColor = Color.white;
        [SerializeField] Color buttonHighlightColor = new Color(0.9f, 0.9f, 0.9f);

        [Header("Text Configuration")]
        [SerializeField] int titleFontSize = 18;
        [SerializeField] int normalFontSize = 14;
        [SerializeField] int buttonFontSize = 12;
        [SerializeField] Color textColor = Color.white;

        void Start()
        {
            if (autoSetupOnStart)
            {
                SetupUI();
            }
        }

        /// <summary>
        /// Main setup method - creates all UI elements
        /// </summary>
        [ContextMenu("Setup UI")]
        public void SetupUI()
        {
            Debug.Log("[UISetupHelper] Starting UI setup...");

            if (targetCanvas == null)
            {
                targetCanvas = FindObjectOfType<Canvas>();
                if (targetCanvas == null)
                {
                    Debug.LogError("[UISetupHelper] No Canvas found! Please assign a target canvas.");
                    return;
                }
            }

            if (createTaskDisplayUI)
            {
                CreateTaskDisplayUI();
            }

            if (createEventControlUI)
            {
                CreateEventControlUI();
            }

            Debug.Log("[UISetupHelper] UI setup complete!");
        }

        /// <summary>
        /// Create Task Display UI elements
        /// </summary>
        void CreateTaskDisplayUI()
        {
            Debug.Log("[UISetupHelper] Creating Task Display UI...");

            // Create main TaskCanvas
            GameObject taskCanvas = CreateUIObject("TaskCanvas", targetCanvas.transform);
            RectTransform taskCanvasRect = taskCanvas.GetComponent<RectTransform>();
            taskCanvasRect.anchoredPosition = taskCanvasPosition;
            taskCanvasRect.sizeDelta = taskCanvasSize;

            // Add background panel
            Image taskCanvasBg = taskCanvas.AddComponent<Image>();
            taskCanvasBg.color = new Color(0, 0, 0, 0.7f);

            // Create TaskTitle
            GameObject taskTitle = CreateTextElement("TaskTitle", taskCanvas.transform, "Nächste Aufgabe:", titleFontSize);
            PositionElement(taskTitle, new Vector2(0, 120), new Vector2(380, 30));

            // Create TaskText (Description)
            GameObject taskText = CreateTextElement("TaskText", taskCanvas.transform, "Zurzeit keine weiteren Aufgaben", normalFontSize);
            PositionElement(taskText, new Vector2(0, 50), new Vector2(380, 100));
            taskText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;

            // Create TaskTimer
            GameObject taskTimer = CreateTextElement("TaskTimer", taskCanvas.transform, "00:00", normalFontSize);
            PositionElement(taskTimer, new Vector2(-150, -50), new Vector2(100, 30));
            taskTimer.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            taskTimer.SetActive(false); // Hidden by default

            // Create TaskStatus
            GameObject taskStatus = CreateTextElement("TaskStatus", taskCanvas.transform, "Wartend", normalFontSize);
            PositionElement(taskStatus, new Vector2(150, -50), new Vector2(100, 30));
            taskStatus.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // Create TaskProgressBar
            GameObject progressBar = CreateProgressBar("TaskProgressBar", taskCanvas.transform);
            PositionElement(progressBar, new Vector2(0, -90), new Vector2(300, 20));
            progressBar.SetActive(false); // Hidden by default

            // Create TaskStatusIcon
            GameObject statusIcon = CreateImageElement("TaskStatusIcon", taskCanvas.transform, Color.white);
            PositionElement(statusIcon, new Vector2(-180, 120), new Vector2(20, 20));

            // Create TaskListPanel (optional)
            GameObject taskListPanel = CreateUIObject("TaskListPanel", taskCanvas.transform);
            PositionElement(taskListPanel, new Vector2(0, -130), new Vector2(380, 100));
            taskListPanel.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            taskListPanel.SetActive(false); // Hidden by default

            // Create TaskListScrollRect
            GameObject scrollRect = CreateScrollRect("TaskListScrollRect", taskListPanel.transform);
            PositionElement(scrollRect, Vector2.zero, new Vector2(380, 100));

            // Create TaskListContainer
            GameObject listContainer = CreateUIObject("TaskListContainer", scrollRect.transform.Find("Viewport/Content"));
            listContainer.AddComponent<VerticalLayoutGroup>();
            listContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Debug.Log("[UISetupHelper] Task Display UI created successfully!");
        }

        /// <summary>
        /// Create Event Control UI elements
        /// </summary>
        void CreateEventControlUI()
        {
            Debug.Log("[UISetupHelper] Creating Event Control UI...");

            // Create main EventControlCanvas
            GameObject eventControlCanvas = CreateUIObject("EventControlCanvas", targetCanvas.transform);
            RectTransform eventControlRect = eventControlCanvas.GetComponent<RectTransform>();
            eventControlRect.anchoredPosition = eventControlPosition;
            eventControlRect.sizeDelta = eventControlSize;

            // Add background
            Image eventControlBg = eventControlCanvas.AddComponent<Image>();
            eventControlBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            float yPos = 250f;

            // Create NPC Actions Panel
            GameObject npcPanel = CreatePanel("NPCActionsPanel", eventControlCanvas.transform, new Vector2(0, yPos), new Vector2(280, 150));
            CreateSectionTitle("NPC Actions", npcPanel.transform, new Vector2(0, 60));
            yPos -= 180f;

            // Create NPC sections
            CreateNPCSection("NurseSection", npcPanel.transform, "Nurse", new Vector2(-90, 20));
            CreateNPCSection("BrotherSection", npcPanel.transform, "Brother", new Vector2(0, 20));
            CreateNPCSection("WifeSection", npcPanel.transform, "Wife", new Vector2(90, 20));

            // Create Task Actions Panel
            GameObject taskPanel = CreatePanel("TaskActionsPanel", eventControlCanvas.transform, new Vector2(0, yPos), new Vector2(280, 120));
            CreateSectionTitle("Task Actions", taskPanel.transform, new Vector2(0, 45));
            CreateTaskActionButtons(taskPanel.transform);
            yPos -= 140f;

            // Create Camera Actions Panel
            GameObject cameraPanel = CreatePanel("CameraActionsPanel", eventControlCanvas.transform, new Vector2(0, yPos), new Vector2(280, 80));
            CreateSectionTitle("Camera Control", cameraPanel.transform, new Vector2(0, 25));
            CreateCameraButtons(cameraPanel.transform);
            yPos -= 100f;

            // Create Study Control Panel
            GameObject studyPanel = CreatePanel("StudyControlPanel", eventControlCanvas.transform, new Vector2(0, yPos), new Vector2(280, 80));
            CreateSectionTitle("Study Control", studyPanel.transform, new Vector2(0, 25));
            CreateStudyControlButtons(studyPanel.transform);

            Debug.Log("[UISetupHelper] Event Control UI created successfully!");
        }

        /// <summary>
        /// Create NPC section with walk and talk buttons
        /// </summary>
        void CreateNPCSection(string sectionName, Transform parent, string npcName, Vector2 position)
        {
            GameObject section = CreateUIObject(sectionName, parent);
            PositionElement(section, position, new Vector2(80, 100));

            // Create label
            GameObject label = CreateTextElement($"{npcName}Label", section.transform, npcName, 10);
            PositionElement(label, new Vector2(0, 35), new Vector2(80, 20));

            // Create walk buttons (simplified - just 2 for demo)
            CreateButton($"{npcName}Walk1", section.transform, "Walk 1", new Vector2(0, 10), new Vector2(70, 20));
            CreateButton($"{npcName}Walk2", section.transform, "Walk 2", new Vector2(0, -10), new Vector2(70, 20));

            // Create talk buttons (simplified - just 1 for demo)
            CreateButton($"{npcName}Talk1", section.transform, "Talk", new Vector2(0, -30), new Vector2(70, 20));
        }

        /// <summary>
        /// Create task action buttons
        /// </summary>
        void CreateTaskActionButtons(Transform parent)
        {
            CreateButton("ShowMathTaskButton", parent, "Show Math", new Vector2(-70, 10), new Vector2(120, 25));
            CreateButton("ShowNBackTaskButton", parent, "Show N-Back", new Vector2(70, 10), new Vector2(120, 25));
            CreateButton("HideMathTaskButton", parent, "Hide Math", new Vector2(-70, -20), new Vector2(120, 25));
            CreateButton("HideNBackTaskButton", parent, "Hide N-Back", new Vector2(70, -20), new Vector2(120, 25));
        }

        /// <summary>
        /// Create camera control buttons
        /// </summary>
        void CreateCameraButtons(Transform parent)
        {
            for (int i = 0; i < 4; i++)
            {
                float xPos = -105f + (i * 70f);
                CreateButton($"CameraButton{i + 1}", parent, $"Cam {i + 1}", new Vector2(xPos, -10), new Vector2(60, 25));
            }
        }

        /// <summary>
        /// Create study control buttons
        /// </summary>
        void CreateStudyControlButtons(Transform parent)
        {
            CreateButton("AbortAllButton", parent, "Abort All", new Vector2(-70, -10), new Vector2(80, 25));
            CreateButton("EndStudyButton", parent, "End Study", new Vector2(0, -10), new Vector2(80, 25));
            CreateButton("RefreshButton", parent, "Refresh", new Vector2(70, -10), new Vector2(80, 25));
        }

        #region UI Creation Utilities

        /// <summary>
        /// Create basic UI GameObject
        /// </summary>
        GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        /// <summary>
        /// Create text element with TextMeshPro
        /// </summary>
        GameObject CreateTextElement(string name, Transform parent, string text, int fontSize)
        {
            GameObject textObj = CreateUIObject(name, parent);
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = textColor;
            textComponent.alignment = TextAlignmentOptions.Center;
            return textObj;
        }

        /// <summary>
        /// Create image element
        /// </summary>
        GameObject CreateImageElement(string name, Transform parent, Color color)
        {
            GameObject imageObj = CreateUIObject(name, parent);
            Image imageComponent = imageObj.AddComponent<Image>();
            imageComponent.color = color;
            return imageObj;
        }

        /// <summary>
        /// Create button element
        /// </summary>
        GameObject CreateButton(string name, Transform parent, string text, Vector2 position, Vector2 size)
        {
            GameObject buttonObj = CreateUIObject(name, parent);
            PositionElement(buttonObj, position, size);

            // Add Image component for background
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = buttonNormalColor;

            // Add Button component
            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = buttonHighlightColor;
            button.colors = colors;

            // Add text child
            GameObject textObj = CreateTextElement("Text", buttonObj.transform, text, buttonFontSize);
            PositionElement(textObj, Vector2.zero, size);

            return buttonObj;
        }

        /// <summary>
        /// Create panel with background
        /// </summary>
        GameObject CreatePanel(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject panel = CreateUIObject(name, parent);
            PositionElement(panel, position, size);

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);

            return panel;
        }

        /// <summary>
        /// Create section title
        /// </summary>
        void CreateSectionTitle(string title, Transform parent, Vector2 position)
        {
            GameObject titleObj = CreateTextElement($"{title}Title", parent, title, titleFontSize);
            PositionElement(titleObj, position, new Vector2(200, 25));
            titleObj.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        }

        /// <summary>
        /// Create progress bar
        /// </summary>
        GameObject CreateProgressBar(string name, Transform parent)
        {
            GameObject progressBar = CreateUIObject(name, parent);

            // Background
            Image background = progressBar.AddComponent<Image>();
            background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Fill area
            GameObject fillArea = CreateUIObject("Fill Area", progressBar.transform);
            PositionElement(fillArea, Vector2.zero, Vector2.zero);
            fillArea.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            fillArea.GetComponent<RectTransform>().anchorMax = Vector2.one;

            // Fill
            GameObject fill = CreateUIObject("Fill", fillArea.transform);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0, 1, 0, 0.8f);
            fillImage.type = Image.Type.Filled;

            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;

            return progressBar;
        }

        /// <summary>
        /// Create scroll rect
        /// </summary>
        GameObject CreateScrollRect(string name, Transform parent)
        {
            GameObject scrollRect = CreateUIObject(name, parent);

            ScrollRect scroll = scrollRect.AddComponent<ScrollRect>();
            scrollRect.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            // Viewport
            GameObject viewport = CreateUIObject("Viewport", scrollRect.transform);
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;

            // Content
            GameObject content = CreateUIObject("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            contentRect.anchoredPosition = Vector2.zero;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.vertical = true;
            scroll.horizontal = false;

            return scrollRect;
        }

        /// <summary>
        /// Position UI element
        /// </summary>
        void PositionElement(GameObject element, Vector2 position, Vector2 size)
        {
            RectTransform rect = element.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        #endregion

        #region Editor Utilities

#if UNITY_EDITOR
        /// <summary>
        /// Create Task Item Prefab
        /// </summary>
        [ContextMenu("Create Task Item Prefab")]
        public void CreateTaskItemPrefab()
        {
            GameObject prefab = CreateUIObject("TaskItemPrefab", null);
            PositionElement(prefab, Vector2.zero, new Vector2(300, 30));
            
            // Background
            Image background = prefab.AddComponent<Image>();
            background.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            
            // Text
            GameObject text = CreateTextElement("Text", prefab.transform, "Task Description", normalFontSize);
            PositionElement(text, Vector2.zero, new Vector2(300, 30));
            text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
            
            // Save as prefab
            string prefabPath = "Assets/Prefabs/TaskItemPrefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            DestroyImmediate(prefab);
            
            Debug.Log($"[UISetupHelper] Task Item Prefab created at: {prefabPath}");
        }
        
        /// <summary>
        /// Auto-assign components to TaskDisplaySystem
        /// </summary>
        [ContextMenu("Auto-Assign TaskDisplaySystem")]
        public void AutoAssignTaskDisplaySystem()
        {
            TaskDisplaySystem taskDisplay = FindObjectOfType<TaskDisplaySystem>();
            if (taskDisplay == null)
            {
                Debug.LogWarning("[UISetupHelper] TaskDisplaySystem not found!");
                return;
            }
            
            // Use reflection to assign fields
            var fields = typeof(TaskDisplaySystem).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(GameObject))
                {
                    GameObject found = GameObject.Find(field.Name);
                    if (found != null)
                    {
                        field.SetValue(taskDisplay, found);
                        Debug.Log($"[UISetupHelper] Assigned {field.Name}");
                    }
                }
                else if (field.FieldType == typeof(TextMeshProUGUI))
                {
                    TextMeshProUGUI found = FindObjectOfType<TextMeshProUGUI>();
                    // Add more specific logic here if needed
                }
            }
            
            Debug.Log("[UISetupHelper] TaskDisplaySystem auto-assignment complete!");
        }
#endif

        #endregion
    }
}
