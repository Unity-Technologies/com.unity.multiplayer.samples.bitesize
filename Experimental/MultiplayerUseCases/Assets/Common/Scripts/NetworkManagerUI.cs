using System.Collections.Generic;
using Unity.Netcode.Transports.UTP;
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


        [SerializeField] string m_SelectionScreenSceneName;
        [SerializeField] GameObject m_ServerOnlyOverlay;

        void Awake()
        {
            var uiDocument = GetComponent<UIDocument>();
            m_Root = uiDocument.rootVisualElement;

            m_AddressInputField = m_Root.Q<TextField>("IPAddressField");
            m_AddressInputField.SetValueWithoutNotify(k_DefaultIP);

            m_PortInputField = m_Root.Q<TextField>("PortField");
            m_PortInputField.SetValueWithoutNotify(k_DefaultPort.ToString());

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
            SetNetworkPortAndAddress(k_DefaultPort, m_AddressInputField.value, k_DefaultServerListenAddress);
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.ServerListenAddress = m_AddressInputField.value;
            NetworkManager.Singleton.StartServer();
            m_ServerOnlyOverlay.gameObject.SetActive(true);
            m_ClientButton.SetEnabled(false);
            m_HostButton.SetEnabled(false);
            m_DisconnectButton.SetEnabled(true);
        }

        void StartHost()
        {
            SetNetworkPortAndAddress(k_DefaultPort, m_AddressInputField.value, k_DefaultServerListenAddress);
            NetworkManager.Singleton.StartHost();
            m_ClientButton.SetEnabled(false);
            m_ServerButton.SetEnabled(false);
            m_DisconnectButton.SetEnabled(true);
            //m_HostButton accessing highlighted color somehow?;
            //EnableButtons(new List<Button>(m_ClientButton, m_ServerButton), false);
        }

        void StartClient()
        {
            SetNetworkPortAndAddress(k_DefaultPort, m_AddressInputField.value, k_DefaultServerListenAddress);
            NetworkManager.Singleton.StartClient();
            m_HostButton.SetEnabled(false);
            m_ServerButton.SetEnabled(false);
            m_DisconnectButton.SetEnabled(true);
        }

        void Disconnect()
        {
            NetworkManager.Singleton.Shutdown();
            m_ServerOnlyOverlay.gameObject.SetActive(false);
            m_HostButton.SetEnabled(true);
            m_ServerButton.SetEnabled(true);
            m_ClientButton.SetEnabled(true);
            m_DisconnectButton.SetEnabled(false);
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

        void SetNetworkPortAndAddress(ushort port, string address, string serverListenAddress)
        {
            var transport = GetComponent<UnityTransport>();
            if (transport == null) //happens during Play Mode Tests
            {
                return;
            }
            transport.SetConnectionData(address, port, serverListenAddress);
        }

        void EnableButtons(List<Button> buttonsToEnable, bool enabled)
        {
            foreach (var button in buttonsToEnable )
            {
                button.SetEnabled(enabled);
            }
        }

    }
}
