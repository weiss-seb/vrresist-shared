using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Handles file transfer between HMD (server) and Tablet (client).
    /// HMD sends log files to the tablet for backup/export.
    /// Auto-creates itself if not present in scene. Persists across scene loads.
    /// </summary>
    public class FileTransfer : MonoBehaviour
    {
        #region Singleton
        private static FileTransfer _instance;
        public static FileTransfer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<FileTransfer>();
                    if (_instance == null)
                    {
                        // Auto-create if not in scene
                        GameObject go = new GameObject("FileTransfer");
                        _instance = go.AddComponent<FileTransfer>();
                        DontDestroyOnLoad(go);
                        Debug.Log("[FileTransfer] Auto-created FileTransfer singleton");
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Message Types
        // Message protocol constants
        public const string MSG_FILE_TRANSFER_REQUEST = "FILE_TRANSFER_REQUEST";
        public const string MSG_FILE_TRANSFER_START = "FILE_TRANSFER_START";
        public const string MSG_FILE_TRANSFER_DATA = "FILE_TRANSFER_DATA";
        public const string MSG_FILE_TRANSFER_END = "FILE_TRANSFER_END";
        public const string MSG_FILE_TRANSFER_COMPLETE = "FILE_TRANSFER_COMPLETE";
        public const string MSG_FILE_TRANSFER_ERROR = "FILE_TRANSFER_ERROR";
        public const string MSG_FILE_TRANSFER_CANCEL = "FILE_TRANSFER_CANCEL";
        #endregion

        #region Events
        [Header("Events")]
        public UnityEvent OnTransferStarted;
        public UnityEvent<float> OnTransferProgress; // 0.0 to 1.0
        public UnityEvent<string> OnTransferComplete; // Success message
        public UnityEvent<string> OnTransferError; // Error message
        #endregion

        #region Settings
        [Header("Settings")]
        [Tooltip("Is this instance running on the HMD (server) or Tablet (client)?")]
        public bool isHMD = true;

        [Header("HMD Settings")]
        [Tooltip("Base path for log files on HMD")]
        public string hmdLogBasePath = "Log";

        [Header("Tablet Settings")]
        [Tooltip("Base path for saving received files on tablet")]
        public string tabletSavePath = "Logs";
        #endregion

        #region State
        private bool isTransferring = false;
        private string currentParticipantId;
        private int totalFiles = 0;
        private int transferredFiles = 0;
        private List<FileTransferData> pendingFiles = new List<FileTransferData>();

        // For receiving files (tablet side)
        private FileTransferData currentReceivingFile;
        private StringBuilder currentFileContent;
        #endregion

        #region Data Classes
        [Serializable]
        public class FileTransferData
        {
            public string relativePath;  // e.g., "Scene1/sceneresults.csv"
            public string content;       // File content (Base64 encoded for binary safety)
            public int fileIndex;        // Current file index
            public int totalFiles;       // Total number of files
        }

        [Serializable]
        public class FileTransferManifest
        {
            public string participantId;
            public int fileCount;
            public string[] filePaths;
        }
        #endregion

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Debug.LogWarning("[FileTransfer] Duplicate instance found, destroying.");
                Destroy(gameObject);
                return;
            }
            
            // Auto-detect if this is HMD or Tablet based on TCPServer presence
            // (TCPServer only exists on HMD, WebSocketClient only on Tablet)
            if (FindObjectOfType<TCPServer>() != null)
            {
                isHMD = true;
                Debug.Log("[FileTransfer] Detected HMD mode (TCPServer found)");
            }
            else if (FindObjectOfType<WebSocketClient>() != null)
            {
                isHMD = false;
                Debug.Log("[FileTransfer] Detected Tablet mode (WebSocketClient found)");
            }
        }

        #region Public API - HMD Side

        /// <summary>
        /// Called automatically when tablet requests files. No confirmation needed.
        /// Can also be called directly to push files to tablet.
        /// </summary>
        public void StartFileTransfer()
        {
            if (!isHMD)
            {
                Debug.LogWarning("[FileTransfer] StartFileTransfer should only be called on HMD!");
                return;
            }

            if (isTransferring)
            {
                Debug.LogWarning("[FileTransfer] Transfer already in progress!");
                return;
            }

            // Get current participant ID
            currentParticipantId = GetCurrentParticipantId();
            if (string.IsNullOrEmpty(currentParticipantId))
            {
                OnTransferError?.Invoke("Keine Teilnehmer-ID gefunden!");
                SendTransferError("No participant ID found");
                return;
            }

            StartCoroutine(SendFilesToTablet());
        }

        /// <summary>
        /// Cancel an ongoing transfer
        /// </summary>
        public void CancelTransfer()
        {
            if (isTransferring)
            {
                isTransferring = false;
                pendingFiles.Clear();
                
                // Notify tablet of cancellation
                SendFileTransferMessage(MSG_FILE_TRANSFER_CANCEL, "Transfer cancelled");
            }
        }

        #endregion

        #region Public API - Tablet Side

        /// <summary>
        /// Call this from tablet UI button to request/download files from HMD.
        /// This is the main entry point for tablet-initiated transfers.
        /// </summary>
        public void RequestFilesFromHMD()
        {
            if (isHMD)
            {
                Debug.LogWarning("[FileTransfer] RequestFilesFromHMD should only be called on Tablet!");
                return;
            }

            if (isTransferring)
            {
                Debug.LogWarning("[FileTransfer] Transfer already in progress!");
                return;
            }

            Debug.Log("[FileTransfer] Requesting files from HMD...");
            var msg = new EventMessage(MSG_FILE_TRANSFER_REQUEST, new string[] { });
            SendMessageToRemote(msg);
        }

        #endregion

        #region Message Handling

        /// <summary>
        /// Process incoming file transfer messages. Call this from MessageHandler/TabletEventControl.
        /// </summary>
        public void HandleFileTransferMessage(EventMessage message)
        {
            if (message == null) return;

            Debug.Log($"[FileTransfer] Handling message: {message.type}");

            switch (message.type)
            {
                case MSG_FILE_TRANSFER_REQUEST:
                    // Tablet requested files - HMD starts sending immediately
                    if (isHMD) StartFileTransfer();
                    break;

                case MSG_FILE_TRANSFER_START:
                    if (!isHMD) HandleTransferStart(message);
                    break;

                case MSG_FILE_TRANSFER_DATA:
                    if (!isHMD) HandleFileData(message);
                    break;

                case MSG_FILE_TRANSFER_END:
                    if (!isHMD) HandleTransferEnd(message);
                    break;

                case MSG_FILE_TRANSFER_COMPLETE:
                    if (isHMD) HandleTransferComplete(message);
                    break;

                case MSG_FILE_TRANSFER_ERROR:
                    HandleTransferError(message);
                    break;

                case MSG_FILE_TRANSFER_CANCEL:
                    HandleTransferCancel();
                    break;
            }
        }

        /// <summary>
        /// Check if a message type is a file transfer message
        /// </summary>
        public static bool IsFileTransferMessage(string messageType)
        {
            return messageType == MSG_FILE_TRANSFER_REQUEST ||
                   messageType == MSG_FILE_TRANSFER_START ||
                   messageType == MSG_FILE_TRANSFER_DATA ||
                   messageType == MSG_FILE_TRANSFER_END ||
                   messageType == MSG_FILE_TRANSFER_COMPLETE ||
                   messageType == MSG_FILE_TRANSFER_ERROR ||
                   messageType == MSG_FILE_TRANSFER_CANCEL;
        }

        #endregion

        #region HMD - Sending Files

        private IEnumerator SendFilesToTablet()
        {
            isTransferring = true;
            OnTransferStarted?.Invoke();

            string basePath = Path.Combine(Application.persistentDataPath, hmdLogBasePath, currentParticipantId);

            if (!Directory.Exists(basePath))
            {
                OnTransferError?.Invoke($"Log-Ordner nicht gefunden: {basePath}");
                isTransferring = false;
                yield break;
            }

            // Collect all files
            var files = Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories);
            totalFiles = files.Length;
            transferredFiles = 0;

            if (totalFiles == 0)
            {
                OnTransferError?.Invoke("Keine Dateien zum Übertragen gefunden!");
                isTransferring = false;
                yield break;
            }

            Debug.Log($"[FileTransfer] Found {totalFiles} files to transfer");

            // Send start message with manifest
            var manifest = new FileTransferManifest
            {
                participantId = currentParticipantId,
                fileCount = totalFiles,
                filePaths = new string[totalFiles]
            };

            for (int i = 0; i < files.Length; i++)
            {
                manifest.filePaths[i] = GetRelativePath(basePath, files[i]);
            }

            SendFileTransferMessage(MSG_FILE_TRANSFER_START, JsonUtility.ToJson(manifest));
            yield return new WaitForSeconds(0.1f);

            // Send each file
            for (int i = 0; i < files.Length; i++)
            {
                if (!isTransferring) yield break; // Cancelled

                string filePath = files[i];
                string relativePath = GetRelativePath(basePath, filePath);

                try
                {
                    string content = File.ReadAllText(filePath);
                    string encodedContent = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));

                    var fileData = new FileTransferData
                    {
                        relativePath = relativePath,
                        content = encodedContent,
                        fileIndex = i + 1,
                        totalFiles = totalFiles
                    };

                    SendFileTransferMessage(MSG_FILE_TRANSFER_DATA, JsonUtility.ToJson(fileData));
                    
                    transferredFiles++;
                    float progress = (float)transferredFiles / totalFiles;
                    OnTransferProgress?.Invoke(progress);

                    Debug.Log($"[FileTransfer] Sent file {transferredFiles}/{totalFiles}: {relativePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[FileTransfer] Error reading file {filePath}: {e.Message}");
                }

                // Small delay between files to prevent overwhelming the connection
                yield return new WaitForSeconds(0.05f);
            }

            // Send end message
            SendFileTransferMessage(MSG_FILE_TRANSFER_END, currentParticipantId);
            Debug.Log("[FileTransfer] All files sent, waiting for confirmation...");
        }

        private void HandleTransferComplete(EventMessage message)
        {
            isTransferring = false;
            string resultMessage = message.content != null && message.content.Length > 0 
                ? message.content[0] 
                : "Transfer abgeschlossen!";
            
            OnTransferComplete?.Invoke(resultMessage);
            Debug.Log($"[FileTransfer] Transfer complete: {resultMessage}");
        }

        #endregion

        #region Tablet - Receiving Files

        private void HandleTransferStart(EventMessage message)
        {
            if (message.content == null || message.content.Length == 0) return;

            try
            {
                var manifest = JsonUtility.FromJson<FileTransferManifest>(message.content[0]);
                currentParticipantId = manifest.participantId;
                totalFiles = manifest.fileCount;
                transferredFiles = 0;

                isTransferring = true;
                OnTransferStarted?.Invoke();

                Debug.Log($"[FileTransfer] Starting to receive {totalFiles} files for participant {currentParticipantId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FileTransfer] Error parsing manifest: {e.Message}");
                SendTransferError("Failed to parse transfer manifest");
            }
        }

        private void HandleFileData(EventMessage message)
        {
            if (message.content == null || message.content.Length == 0) return;

            try
            {
                var fileData = JsonUtility.FromJson<FileTransferData>(message.content[0]);
                
                // Decode content
                byte[] decodedBytes = Convert.FromBase64String(fileData.content);
                string content = Encoding.UTF8.GetString(decodedBytes);

                // Save file
                string savePath = Path.Combine(
                    Application.persistentDataPath, 
                    tabletSavePath, 
                    currentParticipantId, 
                    fileData.relativePath
                );

                // Ensure directory exists
                string directory = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(savePath, content);

                transferredFiles++;
                float progress = (float)transferredFiles / totalFiles;
                OnTransferProgress?.Invoke(progress);

                Debug.Log($"[FileTransfer] Saved file {transferredFiles}/{totalFiles}: {savePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FileTransfer] Error saving file: {e.Message}");
            }
        }

        private void HandleTransferEnd(EventMessage message)
        {
            isTransferring = false;
            
            string resultMessage = $"{transferredFiles} Dateien erfolgreich empfangen!";
            OnTransferComplete?.Invoke(resultMessage);

            // Send confirmation back to HMD
            SendFileTransferMessage(MSG_FILE_TRANSFER_COMPLETE, resultMessage);

            Debug.Log($"[FileTransfer] Transfer complete: {resultMessage}");
        }

        #endregion

        #region Error Handling

        private void HandleTransferError(EventMessage message)
        {
            isTransferring = false;
            string error = message.content != null && message.content.Length > 0 
                ? message.content[0] 
                : "Unknown error";
            
            OnTransferError?.Invoke(error);
            Debug.LogError($"[FileTransfer] Transfer error: {error}");
        }

        private void HandleTransferCancel()
        {
            isTransferring = false;
            OnTransferError?.Invoke("Transfer wurde abgebrochen.");
            Debug.Log("[FileTransfer] Transfer cancelled");
        }

        private void SendTransferError(string error)
        {
            SendFileTransferMessage(MSG_FILE_TRANSFER_ERROR, error);
            OnTransferError?.Invoke(error);
        }

        #endregion

        #region Helpers

        private string GetCurrentParticipantId()
        {
            // Try to get from PlayerPrefs first
            if (PlayerPrefs.HasKey("CurrentParticipantID"))
            {
                return PlayerPrefs.GetInt("CurrentParticipantID").ToString();
            }
            return null;
        }

        private string GetRelativePath(string basePath, string fullPath)
        {
            if (fullPath.StartsWith(basePath))
            {
                string relative = fullPath.Substring(basePath.Length);
                if (relative.StartsWith(Path.DirectorySeparatorChar.ToString()) || 
                    relative.StartsWith(Path.AltDirectorySeparatorChar.ToString()))
                {
                    relative = relative.Substring(1);
                }
                return relative.Replace('\\', '/'); // Normalize to forward slashes
            }
            return Path.GetFileName(fullPath);
        }

        private void SendFileTransferMessage(string type, string content)
        {
            var msg = new EventMessage(type, new string[] { content });
            SendMessageToRemote(msg);
        }

        private void SendMessageToRemote(EventMessage msg)
        {
            if (isHMD)
            {
                // HMD sends via TCPServer
                if (TCPServer.Instance != null)
                {
                    string json = JsonUtility.ToJson(msg);
                    TCPServer.Instance.SendMessageToClient(json);
                }
                else
                {
                    Debug.LogError("[FileTransfer] TCPServer instance not found!");
                }
            }
            else
            {
                // Tablet sends via WebSocketClient
                var wsClient = FindObjectOfType<WebSocketClient>();
                if (wsClient != null)
                {
                    wsClient.SendEventMessage(msg);
                }
                else
                {
                    Debug.LogError("[FileTransfer] WebSocketClient not found!");
                }
            }
        }

        #endregion

        #region Status Properties

        public bool IsTransferring => isTransferring;
        public float TransferProgress => totalFiles > 0 ? (float)transferredFiles / totalFiles : 0f;
        public int TotalFiles => totalFiles;
        public int TransferredFiles => transferredFiles;

        #endregion
    }
}
