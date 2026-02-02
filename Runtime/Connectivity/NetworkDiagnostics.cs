using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Linq;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Network diagnostics tool to help debug connectivity issues
    /// </summary>
    public class NetworkDiagnostics : MonoBehaviour
    {
        [Header("Diagnostic Controls")]
        [SerializeField] private bool runDiagnosticsOnStart = true;
        [SerializeField] private bool logNetworkInterfaces = true;
        [SerializeField] private bool testPortAvailability = true;

        void Start()
        {
            if (runDiagnosticsOnStart)
            {
                RunNetworkDiagnostics();
            }
        }

        [ContextMenu("Run Network Diagnostics")]
        public void RunNetworkDiagnostics()
        {
            Debug.Log("=== NETWORK DIAGNOSTICS START ===");

            LogHostInformation();

            if (logNetworkInterfaces)
            {
                LogNetworkInterfaces();
            }

            if (testPortAvailability)
            {
                TestPortAvailability();
            }

            Debug.Log("=== NETWORK DIAGNOSTICS END ===");
        }

        private void LogHostInformation()
        {
            Debug.Log("--- HOST INFORMATION ---");

            try
            {
                string hostName = Dns.GetHostName();
                Debug.Log($"Host Name: {hostName}");

                var hostEntry = Dns.GetHostEntry(hostName);
                Debug.Log($"Host Aliases: {string.Join(", ", hostEntry.Aliases)}");

                Debug.Log("IP Addresses:");
                foreach (var ip in hostEntry.AddressList)
                {
                    string ipType = ip.AddressFamily == AddressFamily.InterNetwork ? "IPv4" :
                                   ip.AddressFamily == AddressFamily.InterNetworkV6 ? "IPv6" : "Other";

                    string ipCategory = "";
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ipString = ip.ToString();
                        if (ipString.StartsWith("127."))
                            ipCategory = " (Loopback)";
                        else if (ipString.StartsWith("169.254."))
                            ipCategory = " (Link-Local)";
                        else if (ipString.StartsWith("192.168.") || ipString.StartsWith("10.") ||
                                (ipString.StartsWith("172.") && int.Parse(ipString.Split('.')[1]) >= 16 && int.Parse(ipString.Split('.')[1]) <= 31))
                            ipCategory = " (Private)";
                        else
                            ipCategory = " (Public)";
                    }

                    Debug.Log($"  {ip} ({ipType}){ipCategory}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to get host information: {e.Message}");
            }
        }

        private void LogNetworkInterfaces()
        {
            Debug.Log("--- NETWORK INTERFACES ---");

            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var ni in interfaces)
                {
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        Debug.Log($"Interface: {ni.Name} ({ni.NetworkInterfaceType})");
                        Debug.Log($"  Status: {ni.OperationalStatus}");
                        Debug.Log($"  Speed: {ni.Speed / 1000000} Mbps");

                        var ipProps = ni.GetIPProperties();
                        var unicastAddresses = ipProps.UnicastAddresses;

                        foreach (var addr in unicastAddresses)
                        {
                            if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                Debug.Log($"  IPv4: {addr.Address} (Subnet: {addr.IPv4Mask})");
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to get network interfaces: {e.Message}");
            }
        }

        private void TestPortAvailability()
        {
            Debug.Log("--- PORT AVAILABILITY TEST ---");

            int[] portsToTest = { 8125, 7778, 5000 };

            foreach (int port in portsToTest)
            {
                TestTCPPort(port);
                TestUDPPort(port);
            }
        }

        private void TestTCPPort(int port)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                Debug.Log($"TCP Port {port}: AVAILABLE");
                listener.Stop();
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    Debug.LogWarning($"TCP Port {port}: IN USE");
                }
                else
                {
                    Debug.LogError($"TCP Port {port}: ERROR - {e.Message}");
                }
            }
        }

        private void TestUDPPort(int port)
        {
            try
            {
                var udpClient = new UdpClient(port);
                Debug.Log($"UDP Port {port}: AVAILABLE");
                udpClient.Close();
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    Debug.LogWarning($"UDP Port {port}: IN USE");
                }
                else
                {
                    Debug.LogError($"UDP Port {port}: ERROR - {e.Message}");
                }
            }
        }

        [ContextMenu("Test UDP Broadcast")]
        public void TestUDPBroadcast()
        {
            Debug.Log("=== UDP BROADCAST TEST ===");

            try
            {
                using (var udpClient = new UdpClient())
                {
                    udpClient.EnableBroadcast = true;

                    string testMessage = "TEST_BROADCAST";
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(testMessage);

                    var broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, 7778);
                    udpClient.Send(data, data.Length, broadcastEndPoint);

                    Debug.Log($"UDP broadcast sent: {testMessage}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"UDP broadcast test failed: {e.Message}");
            }
        }

        [ContextMenu("Test TCP Connection")]
        public void TestTCPConnection()
        {
            Debug.Log("=== TCP CONNECTION TEST ===");

            // Get the current IP from TCPServer logic
            string serverIP = GetCurrentIP();
            int serverPort = 8125;

            Debug.Log($"Testing TCP connection to {serverIP}:{serverPort}");

            try
            {
                using (var client = new TcpClient())
                {
                    client.Connect(serverIP, serverPort);
                    Debug.Log("TCP connection successful!");
                    client.Close();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"TCP connection test failed: {e.Message}");
            }
        }

        private string GetCurrentIP()
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

                    return ipString;
                }
            }

            return "127.0.0.1";
        }
    }
}
