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

    private void Start()
    {
        OnConnected.AddListener(SendInitialRequest);
        Debug.Log("[WSClient] WebSocket Client started. Waiting for discovery...");
    }

    private void SendInitialRequest(string ip)
    {
        Debug.Log("[WSClient] Sending initial requests...");
        EventMessage refreshMsg = new EventMessage("request", new string[] { "refresh" });
        SendEventMessage(refreshMsg);

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
                OnMessageReceive.Invoke(msg);
            }
            catch (Exception e)
            {
                Debug.Log("[WebsocketClient] There is a problem with the incoming message" + $"[{e.Message}]");
            }

            //    messageQueue.RemoveAt(0);
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
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WSClient] Could not get local IP: {ex.Message}");
        }
        return "127.0.0.1";
    }


    [System.Serializable]
    private class EventMessageWrapper
    {
        public string type;
        public string[] content;
    }
}
