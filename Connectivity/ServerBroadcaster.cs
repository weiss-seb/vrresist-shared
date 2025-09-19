using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

[RequireComponent(typeof(TCPServer))]
public class ServerBroadcaster : MonoBehaviour
{
    public int broadcastPort = 7778;
    private UdpClient udpClient;
    private Thread broadcastThread;
    private bool isBroadcasting = false;

    private TCPServer tcpServer;

    void Start()
    {
        tcpServer = GetComponent<TCPServer>();
        if (tcpServer == null)
        {
            Debug.LogError("ServerBroadcaster requires a TCPServer component on the same GameObject.");
            return;
        }

        udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;

        isBroadcasting = true;
        broadcastThread = new Thread(BroadcastPresence);
        broadcastThread.IsBackground = true;
        broadcastThread.Start();
    }

    private void BroadcastPresence()
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);

        while (isBroadcasting)
        {
            string serverIp = tcpServer.GetCurrentIP();
            int serverPort = tcpServer.GetCurrentPort();
            string message = $"SERVER_HERE:{serverIp}:{serverPort}";
            byte[] data = Encoding.UTF8.GetBytes(message);

            try
            {
                udpClient.Send(data, data.Length, endPoint);
                //Todo: Find a way to stop broadcasting when connection is established
                //Debug.Log($"Broadcasting presence: {message}");
            }
            catch (SocketException e)
            {
                Debug.LogError($"Broadcast failed: {e.Message}");
            }

            Thread.Sleep(2000); // Broadcast every 2 seconds
        }
    }

    void OnApplicationQuit()
    {
        isBroadcasting = false;
        if (udpClient != null)
        {
            udpClient.Close();
        }
        if (broadcastThread != null && broadcastThread.IsAlive)
        {
            broadcastThread.Join(); // Wait for the thread to finish
        }
    }
}
