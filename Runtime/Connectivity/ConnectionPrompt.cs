using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TextureSendReceive;

namespace OVGU.VAR.VRResist
{
    /// <summary>
    /// Handles the connection prompt for the WebSocket client.
    /// </summary>
    [RequireComponent(typeof(WebSocketClient))]
    public class ConnectionPrompt : MonoBehaviour
    {
        [SerializeField] WebSocketClient webSocketClient;
        [SerializeField] TMPro.TMP_InputField inputField;
        [SerializeField] Button connectButton;
        [SerializeField] TMPro.TMP_Text errorMessage;

        [SerializeField] TextureReceiver textureReceiver;

        string serverIp = "";
        int port;

        // key constants
        const string KEY_IPADDRESS = "IPAddress";
        const string KEY_PORT = "Port";

        private void Start()
        {
            // Load playerprefs if exist
            //inputField.text = PlayerPrefs.GetString(KEY_IPADDRESS) + ":" + PlayerPrefs.GetInt(KEY_PORT).ToString();
            //check if playerprefs exist
            if (PlayerPrefs.HasKey(KEY_IPADDRESS) && PlayerPrefs.HasKey(KEY_PORT))
            {
                inputField.text = PlayerPrefs.GetString(KEY_IPADDRESS) + ":" + PlayerPrefs.GetInt(KEY_PORT).ToString();
            }
            else
            {
                // Use discovered IP instead of hardcoded value
                string discoveredIP = GetDiscoveredServerIP();
                inputField.text = discoveredIP + ":8125";
            }

            // intially hide error message
            errorMessage.gameObject.SetActive(false);
            connectButton.onClick.AddListener(Send);

            // Websocket Listeners
            webSocketClient.OnConnected.AddListener(OnSuccessResponse);
            webSocketClient.OnConnectionFailed.AddListener(OnFailedResponse);

            // DEBUG:

        }

        private void Send()
        {
            // trying to connect to VR app
            try
            {
                Debug.Log(inputField.text);
                (string ipAddress, int port) = ExtractIpAndPort("http://" + inputField.text);
                serverIp = ipAddress;

                webSocketClient.Connect(ipAddress, port);
            }
            catch
            {
                Debug.Log("[ConnectionPrompt] Problem while trying to connect to VR app");
            }
        }

        private void OnSuccessResponse(string ip)
        {
            // hide connection prompt
            gameObject.SetActive(false);

            // save ip and port to playerprefs using actual connected IP
            try
            {
                PlayerPrefs.SetString(KEY_IPADDRESS, serverIp);
                PlayerPrefs.SetInt(KEY_PORT, port);
                PlayerPrefs.Save();
                Debug.Log($"[ConnectionPrompt] Saved connection details: {serverIp}:{port}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConnectionPrompt] Error saving player prefs: {e.Message}");
            }

        }

        private void OnFailedResponse()
        {
            errorMessage.text = "Couldn't connect to VR App";
            errorMessage.gameObject.SetActive(true);
        }

        /// <summary>
        /// Discover server IP using the same logic as WebSocketClient
        /// </summary>
        private string GetDiscoveredServerIP()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());

                // First pass: Look for non-loopback, non-link-local IPv4 addresses
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        string ipString = ip.ToString();

                        // Skip loopback addresses (127.x.x.x)
                        if (ipString.StartsWith("127."))
                            continue;

                        // Skip link-local addresses (169.254.x.x)
                        if (ipString.StartsWith("169.254."))
                            continue;

                        // This is a valid network IP address
                        Debug.Log($"[ConnectionPrompt] Found network IP for default: {ipString}");
                        return ipString;
                    }
                }

                // Second pass: If no network IP found, accept any IPv4 except loopback
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        string ipString = ip.ToString();
                        if (!ipString.StartsWith("127."))
                        {
                            Debug.LogWarning($"[ConnectionPrompt] Using fallback IP for default: {ipString}");
                            return ipString;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConnectionPrompt] Could not discover server IP: {ex.Message}");
            }

            // Last resort: return a reasonable default
            Debug.LogWarning("[ConnectionPrompt] No network IP found, using default placeholder");
            return "192.168.1.100"; // Common network range as placeholder
        }

        static (string ipAddress, int port) ExtractIpAndPort(string urlString)
        {
            Uri uri = new Uri(urlString);
            string ipAddress = uri.Host;
            int port = uri.Port;

            if (port == -1)
            {
                port = 80;
            }

            return (ipAddress, port);
        }

    }
}
