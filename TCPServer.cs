using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class MessageEvent : UnityEvent<String>
{
}

public class TCPServer : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private MessageEvent messageEvent = null;
    public UnityEvent OnClientConnected;

    [Header("Settings")]
    public int port;

    private TcpListener tcpListener;
    private Thread tcpListenerThread;
    private TcpClient connectedTcpClient;
    private bool isRunning;

    private readonly Queue<string> messageQueue = new Queue<string>();
    private readonly List<Action> mainThreadActions = new List<Action>();

    void Start()
    {
        OnClientConnected.AddListener(() => Debug.Log("[TCPServer] OnClientConnected event fired."));
        isRunning = true;
        tcpListenerThread = new Thread(ListenForClients);
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
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
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
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
            return connectedTcpClient.Client.RemoteEndPoint.ToString();
        }
        return "No client connected";
    }
}
