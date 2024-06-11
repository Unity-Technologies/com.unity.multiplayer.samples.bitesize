using System.Linq;
using System.Text.RegularExpressions;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.Netcode.Samples.MultiplayerUseCases.Common
{
    [RequireComponent(typeof(UIDocument))]
    public class NetworkManagerUI : MonoBehaviour
    {
        const string k_DefaultIP = "127.0.0.1";
        const string k_DefaultServerListenAddress = "0.0.0.0"; //note: this is not safe for real world usage and would limit you to IPv4-only addresses, but this goes out the scope of this sample.
        const ushort k_DefaultPort = 7979;

        static class UIElementNames
        {
            public const string addressInputField = "IPAddressField";
            public const string portInputField = "PortField";
            public const string serverButton = "ServerButton";
            public const string hostButton = "HostButton";
            public const string clientButton = "ClientButton";
            public const string disconnectButton = "DisconnectButton";
            public const string quitSceneButton = "QuitSceneButton";
        }

        VisualElement m_Root;
        TextField m_AddressInputField;
        TextField m_PortInputField;
        Button m_ServerButton;
        Button m_HostButton;
        Button m_ClientButton;
        Button m_DisconnectButton;
        Button m_QuitSceneButton;

        [SerializeField] string m_SelectionScreenSceneName;
        [SerializeField] GameObject m_ServerOnlyOverlay;

        void Awake()
        {
            var uiDocument = GetComponent<UIDocument>();
            m_Root = uiDocument.rootVisualElement;

            m_AddressInputField = m_Root.Q<TextField>(UIElementNames.addressInputField);
            m_AddressInputField.SetValueWithoutNotify(k_DefaultIP);

            m_PortInputField = m_Root.Q<TextField>(UIElementNames.portInputField);
            m_PortInputField.SetValueWithoutNotify(k_DefaultPort.ToString());

            m_ServerButton = m_Root.Q<Button>(UIElementNames.serverButton);
            m_ServerButton.RegisterCallback<ClickEvent>(StartServer);

            m_HostButton = m_Root.Q<Button>(UIElementNames.hostButton);
            m_HostButton.RegisterCallback<ClickEvent>(StartHost);

            m_ClientButton = m_Root.Q<Button>(UIElementNames.clientButton);
            m_ClientButton.RegisterCallback<ClickEvent>(StartClient);

            m_DisconnectButton = m_Root.Q<Button>(UIElementNames.disconnectButton);
            m_DisconnectButton.RegisterCallback<ClickEvent>(Disconnect);

            m_QuitSceneButton = m_Root.Q<Button>(UIElementNames.quitSceneButton);
            m_QuitSceneButton.RegisterCallback<ClickEvent>(QuitScene);
        }

        void Start()
        {
            m_ServerOnlyOverlay.gameObject.SetActive(false);
        }

        void StartServer(ClickEvent evt)
        {
            SetNetworkPortAndAddress(k_DefaultPort, m_AddressInputField.value, k_DefaultServerListenAddress);
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.ServerListenAddress = m_AddressInputField.value;
            NetworkManager.Singleton.StartServer();
            m_ServerOnlyOverlay.gameObject.SetActive(true);
        }

        void StartHost(ClickEvent evt)
        {
            SetNetworkPortAndAddress(k_DefaultPort, m_AddressInputField.value, k_DefaultServerListenAddress);
            NetworkManager.Singleton.StartHost();
        }

        void StartClient(ClickEvent evt)
        {
            SetNetworkPortAndAddress(k_DefaultPort, m_AddressInputField.value, k_DefaultServerListenAddress);
            NetworkManager.Singleton.StartClient();
        }

        void Disconnect(ClickEvent evt)
        {
            NetworkManager.Singleton.Shutdown();
            m_ServerOnlyOverlay.gameObject.SetActive(false);
        }

        void QuitScene(ClickEvent evt)
        {
            Disconnect(evt);
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

    }
}
