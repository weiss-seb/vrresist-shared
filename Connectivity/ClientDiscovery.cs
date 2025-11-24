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
                Debug.Log($"[ClientDiscovery] Starting UDP listener on port {broadcastPort}");
                udpClient = new UdpClient(broadcastPort);
                udpClient.Client.ReceiveTimeout = 5000; // 5 second timeout to prevent indefinite blocking

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
                Debug.Log("[ClientDiscovery] Listening for server broadcasts...");

                while (isDiscovering)
                {
                    try
                    {
                        byte[] data = udpClient.Receive(ref remoteEndPoint);
                        string message = Encoding.UTF8.GetString(data);

                        Debug.Log($"[ClientDiscovery] Received UDP message: {message} from {remoteEndPoint}");

                        if (message.StartsWith("SERVER_HERE:"))
                        {
                            Debug.Log($"[ClientDiscovery] Discovered server: {message} from {remoteEndPoint}");
                            string[] parts = message.Split(':');
                            if (parts.Length == 3)
                            {
                                discoveredIp = parts[1];
                                if (int.TryParse(parts[2], out discoveredPort))
                                {
                                    Debug.Log($"[ClientDiscovery] Server found at {discoveredIp}:{discoveredPort}");
                                    serverFound = true;
                                    isDiscovering = false; // Stop listening once found
                                    break;
                                }
                            }
                        }
                    }
                    catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
                    {
                        // Timeout is expected, continue listening
                        Debug.Log("[ClientDiscovery] UDP receive timeout, continuing to listen...");
                    }
                }
            }
            catch (SocketException e)
            {
                if (isDiscovering)
                {
                    Debug.LogError($"[ClientDiscovery] Discovery failed: {e.Message}");
                    Debug.LogError($"[ClientDiscovery] Socket error code: {e.SocketErrorCode}");
                }
            }
            catch (Exception e)
            {
                if (isDiscovering) Debug.LogError($"[ClientDiscovery] Unexpected error: {e.Message}");
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
                        Debug.Log("[ClientDiscovery] Left multicast group");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ClientDiscovery] Failed to leave multicast group: {e.Message}");
                    }
                    udpClient.Close();
                    Debug.Log("[ClientDiscovery] UDP client closed");
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
