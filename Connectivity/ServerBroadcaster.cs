using UnityEngine;
using System;
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

        try
        {
            // Create UDP client with explicit binding to avoid port conflicts
            udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;

            Debug.Log($"[ServerBroadcaster] UDP client created for broadcasting on port {broadcastPort}");

            isBroadcasting = true;
            broadcastThread = new Thread(BroadcastPresence);
            broadcastThread.IsBackground = true;
            broadcastThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError($"[ServerBroadcaster] Failed to create UDP client: {e.Message}");
        }
    }

    private void BroadcastPresence()
    {
        // Use multicast address for better cross-network discovery
        IPAddress multicastAddress = IPAddress.Parse("224.0.0.251"); // mDNS multicast address
        IPEndPoint multicastEndPoint = new IPEndPoint(multicastAddress, broadcastPort);

        // Also keep broadcast for local network compatibility
        IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);

        while (isBroadcasting)
        {
            string serverIp = tcpServer.GetCurrentIP();
            int serverPort = tcpServer.GetCurrentPort();
            string message = $"SERVER_HERE:{serverIp}:{serverPort}";
            byte[] data = Encoding.UTF8.GetBytes(message);

            try
            {
                // Try multicast first (better for cross-network)
                try
                {
                    udpClient.Send(data, data.Length, multicastEndPoint);

                }
                catch (SocketException e)
                {
                    Debug.LogWarning($"[ServerBroadcaster] Multicast failed: {e.Message}");
                }

                // Fallback to broadcast for local network
                try
                {
                    udpClient.Send(data, data.Length, broadcastEndPoint);

                }
                catch (SocketException e)
                {
                    Debug.LogWarning($"[ServerBroadcaster] Broadcast failed: {e.Message}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ServerBroadcaster] Discovery failed: {e.Message}");
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
