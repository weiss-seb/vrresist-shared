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
                // if not, set default values
                inputField.text = "10.51.14.177:8125";
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
            //textureReceiver.IP = serverIp;

            // save ip and port to playerprefs
            try
            {
                PlayerPrefs.SetString(KEY_IPADDRESS, "192.168.178.39");
                PlayerPrefs.SetInt(KEY_PORT, port);
                PlayerPrefs.Save();
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