using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Client discovery system that listens for server broadcasts and connects to the server.
    /// </summary>  
    [RequireComponent(typeof(WebSocketClient))]
    public class ClientDiscovery : MonoBehaviour
    {
        public int broadcastPort = 7778;
        private UdpClient udpClient;
        private Thread discoveryThread;
        private bool isDiscovering = false;

        private WebSocketClient webSocketClient;

        private string discoveredIp;
        private int discoveredPort;
        private volatile bool serverFound = false;

        void Start()
        {
            webSocketClient = GetComponent<WebSocketClient>();
            if (webSocketClient == null)
            {
                Debug.LogError("ClientDiscovery requires a WebSocketClient component on the same GameObject.");
                return;
            }

            isDiscovering = true;
            discoveryThread = new Thread(ListenForBroadcast);
            discoveryThread.IsBackground = true;
            discoveryThread.Start();
        }

        private void ListenForBroadcast()
        {
            try
            {
                udpClient = new UdpClient(broadcastPort);

                // Join multicast group for better cross-network discovery
                try
                {
                    IPAddress multicastAddress = IPAddress.Parse("224.0.0.251");
                    udpClient.JoinMulticastGroup(multicastAddress);
                    Debug.Log("[ClientDiscovery] Joined multicast group for discovery");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ClientDiscovery] Failed to join multicast group: {e.Message}");
                }

                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, broadcastPort);

                while (isDiscovering)
                {
                    byte[] data = udpClient.Receive(ref remoteEndPoint);
                    string message = Encoding.UTF8.GetString(data);

                    if (message.StartsWith("SERVER_HERE:"))
                    {
                        Debug.Log($"[ClientDiscovery] Discovered server: {message} from {remoteEndPoint}");
                        string[] parts = message.Split(':');
                        if (parts.Length == 3)
                        {
                            discoveredIp = parts[1];
                            if (int.TryParse(parts[2], out discoveredPort))
                            {
                                serverFound = true;
                                isDiscovering = false; // Stop listening once found
                            }
                        }
                    }
                }
            }
            catch (SocketException e)
            {
                // This can happen when the client is closed.
                if (isDiscovering) Debug.LogError($"[ClientDiscovery] Discovery failed: {e.Message}");
            }
            finally
            {
                if (udpClient != null)
                {
                    try
                    {
                        // Leave multicast group before closing
                        IPAddress multicastAddress = IPAddress.Parse("224.0.0.251");
                        udpClient.DropMulticastGroup(multicastAddress);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ClientDiscovery] Failed to leave multicast group: {e.Message}");
                    }
                    udpClient.Close();
                }
            }
        }

        void Update()
        {
            if (serverFound)
            {
                serverFound = false; // Prevent re-triggering
                Debug.Log($"Server found at {discoveredIp}:{discoveredPort}. Connecting...");
                webSocketClient.Connect(discoveredIp, discoveredPort);
            }
        }

        void OnApplicationQuit()
        {
            isDiscovering = false;
            if (udpClient != null)
            {
                // Closing the client will cause the Receive call to throw a SocketException
                // which will gracefully exit the thread.
                udpClient.Close();
            }
            if (discoveryThread != null && discoveryThread.IsAlive)
            {
                discoveryThread.Join();
            }
        }
    }
}
