using System;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Unity.Netcode.Samples.MultiplayerUseCases.Common
{
    [RequireComponent(typeof(UIDocument))]
    public class NetworkManagerUI : MonoBehaviour
    {
        const string k_DefaultIP = "127.0.0.1";
        const string k_DefaultServerListenAddress = "0.0.0.0"; //note: this is not safe for real world usage and would limit you to IPv4-only addresses, but this goes out the scope of this sample.
        const ushort k_DefaultPort = 7979;

        VisualElement m_Root;
        TextField m_AddressInputField;
        TextField m_PortInputField;
        Button m_ServerButton;
        Button m_HostButton;
        Button m_ClientButton;
        Button m_DisconnectButton;
        Button m_QuitSceneButton;
        Button[] m_Buttons;

        [SerializeField]
        string m_SelectionScreenSceneName;
        [SerializeField]
        GameObject m_ServerOnlyOverlay;

        void Awake()
        {
            var uiDocument = GetComponent<UIDocument>();
            m_Root = uiDocument.rootVisualElement;
            m_AddressInputField = UIElementsUtils.SetupStringField("IPAddressField", string.Empty, k_DefaultIP, OnAddressChanged, m_Root);
            m_PortInputField = UIElementsUtils.SetupStringField("PortField", string.Empty, k_DefaultPort.ToString(), OnPortChanged, m_Root);
            m_ServerButton = UIElementsUtils.SetupButton("ServerButton", StartServer, true, m_Root, "Server", "Starts the Server");
            m_HostButton = UIElementsUtils.SetupButton("HostButton", StartHost, true, m_Root, "Host", "Starts the Host");
            m_ClientButton = UIElementsUtils.SetupButton("ClientButton", StartClient, true, m_Root, "Client", "Starts the Client");
            m_DisconnectButton = UIElementsUtils.SetupButton("DisconnectButton", Disconnect, false, m_Root, "Disconnect", "Disconnects participant from session");
            UIElementsUtils.SetupButton("QuitSceneButton", QuitScene, true, m_Root, "Quit Scene", "Quits scene and brings you back to the scene selection screen");
            m_Buttons = new Button[] { m_ServerButton, m_HostButton, m_ClientButton };
        }

        void Start()
        {
            m_ServerOnlyOverlay.gameObject.SetActive(false);
        }

        void StartServer()
        {
            SetNetworkPortAndAddress(ushort.Parse(m_PortInputField.value), m_AddressInputField.value, k_DefaultServerListenAddress);
            NetworkManager.Singleton.StartServer();
            EnableAndHighlightButtons(m_ServerButton, false);
            SetButtonStateAndColor(m_DisconnectButton, false, true);
            m_ServerOnlyOverlay.gameObject.SetActive(true);
        }

        void StartHost()
        {
            SetNetworkPortAndAddress(ushort.Parse(m_PortInputField.value), m_AddressInputField.value, k_DefaultServerListenAddress);
            NetworkManager.Singleton.StartHost();
            EnableAndHighlightButtons(m_HostButton, false);
            SetButtonStateAndColor(m_DisconnectButton, false, true);
        }

        void StartClient()
        {
            SetNetworkPortAndAddress(ushort.Parse(m_PortInputField.value), m_AddressInputField.value, k_DefaultServerListenAddress);
            NetworkManager.Singleton.StartClient();
            EnableAndHighlightButtons(m_ClientButton, false);
            SetButtonStateAndColor(m_DisconnectButton, false, true);
        }

        void Disconnect()
        {
            NetworkManager.Singleton.Shutdown();
            EnableAndHighlightButtons(null, true);
            SetButtonStateAndColor(m_DisconnectButton, false, false);
            m_ServerOnlyOverlay.gameObject.SetActive(false);
        }

        void QuitScene()
        {
            Disconnect();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(m_SelectionScreenSceneName, LoadSceneMode.Single);
        }

        void OnSceneLoaded(Scene loadedScene, LoadSceneMode arg1)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (loadedScene.name == m_SelectionScreenSceneName)
            {
                if (NetworkManager.Singleton)
                {
                    Destroy(NetworkManager.Singleton.gameObject);
                }
            }
        }

        void OnAddressChanged(ChangeEvent<string> evt)
        {
            string newAddress = evt.newValue;
            ushort currentPort = ushort.Parse(m_PortInputField.value);

            if (string.IsNullOrEmpty(newAddress) || !NetworkEndpoint.TryParse(newAddress, currentPort, out NetworkEndpoint networkEndPoint))
            {
                Debug.LogError($"IP address '{newAddress}', is not valid. Reverting IP address to {k_DefaultIP}");
                m_AddressInputField.value = k_DefaultIP;
                return;
            }

            SetNetworkPortAndAddress(currentPort, newAddress, k_DefaultServerListenAddress);
        }

        void OnPortChanged(ChangeEvent<string> evt)
        {
            if (!ushort.TryParse(evt.newValue, out ushort newPort) || !NetworkEndpoint.TryParse(m_AddressInputField.value, newPort, out NetworkEndpoint networkEndPoint))
            {
                Debug.LogError($"Port '{evt.newValue}' is not valid. Reverting port to {k_DefaultPort}");
                m_PortInputField.value = k_DefaultPort.ToString();
                return;
            }

            SetNetworkPortAndAddress(newPort, m_AddressInputField.value, k_DefaultServerListenAddress);
        }

        static void SetNetworkPortAndAddress(ushort port, string address, string serverListenAddress)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null) //happens during Play Mode Tests
            {
                return;
            }

            transport.SetConnectionData(address, port, serverListenAddress);
        }

        void EnableAndHighlightButtons(Button buttonToHighlight, bool enable)
        {
            foreach (var button in m_Buttons)
            {
                SetButtonStateAndColor(button, button == buttonToHighlight, enable);
            }
        }

        void SetButtonStateAndColor(Button button, bool highlight, bool enable)
        {
            button.SetEnabled(enable);

            if (enable)
            {
                button.RemoveFromClassList("UseCaseButtonHighlight");
                return;
            }

            if (highlight)
            {
                button.AddToClassList("UseCaseButtonHighlight");
            }
        }
    }
}
