using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using OVGU.VAR.VRResist;

[Serializable]
public class MessageEvent : UnityEvent<String>
{
}

/// <summary>
/// Singleton TCPServer that persists across scene changes
/// Automatically discovers and connects to MessageHandler in each scene
/// </summary>
public class TCPServer : MonoBehaviour
{
    #region Singleton Implementation

    private static TCPServer _instance;
    public static TCPServer Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TCPServer>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("TCPServer");
                    _instance = go.AddComponent<TCPServer>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    #endregion

    [Header("Events")]
    [SerializeField] public MessageEvent messageEvent = null;
    public UnityEvent OnClientConnected;
    public UnityEvent<string> OnSceneChanged;

    [Header("Settings")]
    public int port = 8125;

    [Header("Scene Management")]
    [SerializeField]
    [Tooltip("Current MessageHandler in the active scene")]
    private MessageHandler currentMessageHandler;


    // Network components
    private TcpListener tcpListener;
    private Thread tcpListenerThread;
    private TcpClient connectedTcpClient;
    private bool isRunning;

    // Message handling
    private readonly Queue<string> messageQueue = new Queue<string>();
    private readonly List<Action> mainThreadActions = new List<Action>();

    // Scene management
    private bool isInitialized = false;

    void Awake()
    {
        // Singleton enforcement
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeTCPServer();
        }
        else if (_instance != this)
        {
            Debug.LogWarning("[TCPServer] Duplicate TCPServer instance found. Destroying duplicate.");
            Destroy(gameObject);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        if (_instance == this && !isInitialized)
        {
            InitializeTCPServer();
        }
    }

    /// <summary>
    /// Initialize TCP server and scene management
    /// </summary>
    private void InitializeTCPServer()
    {
        if (isInitialized) return;

        Debug.Log("[TCPServer] Initializing TCP Server singleton...");

        // Setup events
        OnClientConnected.AddListener(() => Debug.Log("[TCPServer] OnClientConnected event fired."));

        // Start TCP listener
        isRunning = true;
        tcpListenerThread = new Thread(ListenForClients);
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();

        isInitialized = true;
        Debug.Log("[TCPServer] TCP Server singleton initialized successfully.");

        // Discover initial scene components

    }

    void Update()
    {
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
            {
                mainThreadActions[0].Invoke();
                mainThreadActions.RemoveAt(0);
            }
        }

        lock (messageQueue)
        {
            while (messageQueue.Count > 0)
            {
                string msg = messageQueue.Dequeue();
                Debug.Log("[TCPServer] Processing message from queue: " + msg);
                messageEvent.Invoke(msg);
            }
        }
    }

    private void ListenForClients()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            Debug.Log($"[TCPServer] Server is listening on port {port}");

            while (isRunning)
            {
                Debug.Log("[TCPServer] Waiting for a client to connect...");
                connectedTcpClient = tcpListener.AcceptTcpClient();

                lock (mainThreadActions)
                {
                    mainThreadActions.Add(() => OnClientConnected.Invoke());
                }

                Debug.Log($"[TCPServer] Client connected from {connectedTcpClient.Client.RemoteEndPoint}");
                HandleClientCommunication();
                Debug.Log("[TCPServer] Client disconnected. Waiting for new connection...");
            }
        }
        catch (SocketException ex)
        {
            if (!isRunning)
                Debug.Log("[TCPServer] Listener stopped successfully.");
            else
                Debug.LogError($"[TCPServer] SocketException: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TCPServer] Exception in ListenForClients: {ex.Message}");
        }
    }

    private void HandleClientCommunication()
    {
        if (connectedTcpClient == null) return;

        try
        {
            using (var stream = connectedTcpClient.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                while (isRunning && connectedTcpClient.Connected)
                {
                    string message = reader.ReadLine();
                    if (message == null)
                    {
                        Debug.Log("[TCPServer] Client gracefully disconnected.");
                        break;
                    }
                    lock (messageQueue)
                    {
                        Debug.Log("[TCPServer] Received message: " + message);
                        messageQueue.Enqueue(message);
                    }
                }
            }
        }
        catch (IOException ex)
        {
            Debug.Log($"[TCPServer] Client connection closed (IOException): {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TCPServer] Exception in HandleClientCommunication: {ex.Message}");
        }
        finally
        {
            if (connectedTcpClient != null)
            {
                connectedTcpClient.Close();
                connectedTcpClient = null;
            }
        }
    }

    public void SendMessageToClient(string message)
    {
        if (connectedTcpClient == null || !connectedTcpClient.Connected)
        {
            Debug.LogWarning("[TCPServer] Cannot send message, no client connected.");
            return;
        }

        try
        {
            NetworkStream stream = connectedTcpClient.GetStream();
            if (stream.CanWrite)
            {
                byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                stream.Write(data, 0, data.Length);
                stream.Flush();
                Debug.Log("[TCPServer] Sent message: " + message);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TCPServer] Failed to send message: {ex.Message}");
        }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (tcpListener != null)
        {
            tcpListener.Stop();
        }
        if (connectedTcpClient != null)
        {
            connectedTcpClient.Close();
        }
        if (tcpListenerThread != null && tcpListenerThread.IsAlive)
        {
            tcpListenerThread.Join();
        }
        Debug.Log("[TCPServer] Application quitting. Server shut down.");
    }

    public string GetCurrentIP()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());

        // First pass: Look for non-loopback, non-link-local IPv4 addresses
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                string ipString = ip.ToString();

                // Skip loopback addresses (127.x.x.x)
                if (ipString.StartsWith("127."))
                    continue;

                // Skip link-local addresses (169.254.x.x)
                if (ipString.StartsWith("169.254."))
                    continue;

                // Skip APIPA addresses and other non-routable ranges if needed
                // This is a valid network IP address
                Debug.Log($"[TCPServer] Found network IP: {ipString}");
                return ipString;
            }
        }

        // Second pass: If no network IP found, accept any IPv4 except loopback
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                string ipString = ip.ToString();
                if (!ipString.StartsWith("127."))
                {
                    Debug.LogWarning($"[TCPServer] Using fallback IP (may be link-local): {ipString}");
                    return ipString;
                }
            }
        }

        // Last resort: return loopback
        Debug.LogWarning("[TCPServer] No network IP found, falling back to loopback (127.0.0.1)");
        return "127.0.0.1";
    }

    public int GetCurrentPort()
    {
        return port;
    }

    public string GetConnectedClientInfo()
    {
        if (connectedTcpClient != null && connectedTcpClient.Connected)
        {
            if (connectedTcpClient.Client.RemoteEndPoint is IPEndPoint endpoint)
            {
                string clientIP = endpoint.Address.ToString();
                Debug.LogWarning("[TCPServer] Providing connected client info.: " + clientIP);
                return clientIP;
            }
            // Fallback for non-IP endpoints, though unlikely for TCP
            return connectedTcpClient.Client.RemoteEndPoint.ToString();
        }
        Debug.LogWarning("[TCPServer] No client connected.");
        return "No client connected";
    }

    #region Scene Management

    /// <summary>
    /// Called when a new scene is loaded
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[TCPServer] Scene loaded: {scene.name}");


        // Notify tablet about scene change
        OnSceneChanged?.Invoke(scene.name);

        // Send basic scene change notification to tablet
        currentMessageHandler = FindObjectOfType<MessageHandler>();


        if (currentMessageHandler != null)
        {
            Debug.Log("[TCPServer] Found MessageHandler in new scene.");
            currentMessageHandler.sendMessageEvent.AddListener(SendMessageToClient);
            messageEvent.AddListener(currentMessageHandler.OnReceive);
        }
        else
        {
            Debug.LogWarning("[TCPServer] No MessageHandler found in new scene.");
        }
    }


    /// <summary>
    /// Send basic scenario information to tablet
    /// </summary>
    private void SendBasicScenarioInfo(string scenarioName, string sceneName)
    {
        try
        {
            // Create a simple message with scenario info
            var scenarioInfo = new
            {
                type = "scenarioChange",
                scenarioName = scenarioName,
                sceneName = sceneName,
                timestamp = System.DateTime.Now.ToString()
            };

            string json = JsonUtility.ToJson(scenarioInfo);
            var message = new EventMessage("scenarioData", new string[] { json });
            SendMessageToClient(JsonUtility.ToJson(message));

            Debug.Log($"[TCPServer] Sent basic scenario info to tablet: {scenarioName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TCPServer] Failed to send scenario info to tablet: {e.Message}");
        }
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Get current MessageHandler
    /// </summary>
    public MessageHandler CurrentMessageHandler => currentMessageHandler;

    /// <summary>
    /// Get current scenario data
    /// </summary>

    /// <summary>
    /// Check if TCP server is initialized
    /// </summary>
    public bool IsInitialized => isInitialized;

    #endregion
}
