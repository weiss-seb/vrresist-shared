using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// WebSocket client for connecting to a server, sending and receiving messages.
/// </summary>
public class WebSocketClient : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private Thread clientReceiveThread;
    private bool isConnected = false;


    private readonly Queue<string> messageQueue = new Queue<string>();

    public UnityEvent<string> OnMessageReceive;
    public UnityEvent<string> OnConnected;
    public UnityEvent OnConnectionFailed;

    public void Connect(string serverIp, int serverPort)
    {
        Debug.Log($"[WSClient] Attempting to connect to {serverIp}:{serverPort}...");
        try
        {
            client = new TcpClient();
            client.Connect(serverIp, serverPort);
            stream = client.GetStream();
            isConnected = true;

            clientReceiveThread = new Thread(new ThreadStart(ListenForData));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();

            Debug.Log("[WSClient] Connection successful.");

            // Automatically send webcam IP request for streaming setup
            SendCameraRequest();

            OnConnected?.Invoke(serverIp);
        }
        catch (SocketException e)
        {
            Debug.LogError($"[WSClient] Connection failed (SocketException): {e.Message}");
            OnConnectionFailed?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[WSClient] Connection failed (Exception): {e.Message}");
            OnConnectionFailed?.Invoke();
        }
    }

    private void ListenForData()
    {
        try
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                while (isConnected && client.Connected)
                {
                    string message = reader.ReadLine();
                    if (message == null)
                    {
                        Debug.Log("[WSClient] Server gracefully disconnected.");
                        break;
                    }
                    lock (messageQueue)
                    {
                        Debug.Log("[WSClient] Received message: " + message);
                        messageQueue.Enqueue(message);
                    }
                }
            }
        }
        catch (IOException ex)
        {
            Debug.Log($"[WSClient] Connection closed (IOException): {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WSClient] Error in ListenForData: {ex.Message}");
        }
        finally
        {
            isConnected = false;
        }
    }

    public void Disconnect()
    {
        Debug.Log("[WSClient] Disconnecting...");
        isConnected = false;

        if (clientReceiveThread != null && clientReceiveThread.IsAlive)
        {
            clientReceiveThread.Join();
        }
        if (stream != null)
        {
            stream.Close();
            stream = null;
        }
        if (client != null)
        {
            client.Close();
            client = null;
        }
        Debug.Log("[WSClient] Disconnected.");
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }


    private void Send(string message)
    {
        if (!isConnected || stream == null || !stream.CanWrite)
        {
            Debug.LogWarning("[WSClient] Cannot send message, not connected.");
            return;
        }
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            if (stream.CanWrite)
            {
                stream.Write(data, 0, data.Length);
                stream.Flush();
                Debug.Log("[WSClient] Sent message: " + message);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[WSClient] Error sending message: {e.Message}");
            isConnected = false; // Assume connection is lost
        }
    }

    public void SendChatMessage(string message)
    {
        Debug.Log("[WSClient] Preparing chat message: " + message);
        EventMessage msg = new EventMessage("chat", new string[] { message });
        SendEventMessage(msg);
    }

    public void SendCameraRequest()
    {
        string webcamIp = GetLocalIp();
        EventMessage ipMsg = new EventMessage("webcamIp", new string[] { webcamIp });
        SendEventMessage(ipMsg);
    }


    void Update()
    {
        while (messageQueue.Count > 0)
        {
            //     Debug.Log("[Server] " + messageQueue[0]);
            try
            {
                string msg = messageQueue.Dequeue();
                EventMessage eventMessage = JsonUtility.FromJson<EventMessage>(msg);

                // Handle serverIp message for webcam streaming
                if (eventMessage.type == "serverIp" && eventMessage.content.Length > 0)
                {
                    HandleServerIpMessage(eventMessage.content[0]);
                }

                OnMessageReceive.Invoke(msg);
            }
            catch (Exception e)
            {
                Debug.Log("[WebsocketClient] There is a problem with the incoming message" + $"[{e.Message}]");
            }

            //    messageQueue.RemoveAt(0);
        }
    }

    /// <summary>
    /// Handle server IP message for webcam streaming configuration
    /// </summary>
    private void HandleServerIpMessage(string serverIP)
    {
        Debug.Log($"[WSClient] Received server IP for webcam streaming: {serverIP}");

        // Find all TextureReceiver components and configure them
        var textureReceivers = FindObjectsOfType<TextureSendReceive.TextureReceiver>();

        foreach (var receiver in textureReceivers)
        {
            receiver.SetServerIP(serverIP);
            Debug.Log($"[WSClient] Configured TextureReceiver with server IP: {serverIP}");
        }

        // Find ExampleReceiver components and trigger stream initialization
        var exampleReceivers = FindObjectsOfType<TextureSendReceive.ExampleReceiver>();

        foreach (var exampleReceiver in exampleReceivers)
        {
            exampleReceiver.OnStartCameraStream();
            Debug.Log($"[WSClient] Started camera stream for ExampleReceiver");
        }

        if (textureReceivers.Length == 0 && exampleReceivers.Length == 0)
        {
            Debug.LogWarning("[WSClient] No TextureReceiver or ExampleReceiver components found for webcam streaming");
        }
    }

    public void SendEventMessage(EventMessage msg)
    {
        string s = JsonUtility.ToJson(msg);
        Send(s);
    }

    public string GetLocalIp()
    {
        try
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

                    // This is a valid network IP address
                    Debug.Log($"[WSClient] Found network IP: {ipString}");
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
                        Debug.LogWarning($"[WSClient] Using fallback IP (may be link-local): {ipString}");
                        return ipString;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WSClient] Could not get local IP: {ex.Message}");
        }

        // Last resort: return loopback
        Debug.LogWarning("[WSClient] No network IP found, falling back to loopback (127.0.0.1)");
        return "127.0.0.1";
    }


    [System.Serializable]
    private class EventMessageWrapper
    {
        public string type;
        public string[] content;
    }
}
