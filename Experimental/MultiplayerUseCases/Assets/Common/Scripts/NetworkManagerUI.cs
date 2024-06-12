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

            m_AddressInputField = m_Root.Q<TextField>("IPAddressField");
            m_AddressInputField.SetValueWithoutNotify(k_DefaultIP);

            m_PortInputField = m_Root.Q<TextField>("PortField");
            m_PortInputField.SetValueWithoutNotify(k_DefaultPort.ToString());

            m_ServerButton = m_Root.Q<Button>("ServerButton");
            m_ServerButton.RegisterCallback<ClickEvent>(StartServer);

            m_HostButton = m_Root.Q<Button>("HostButton");
            m_HostButton.RegisterCallback<ClickEvent>(StartHost);

            m_ClientButton = m_Root.Q<Button>("ClientButton");
            m_ClientButton.RegisterCallback<ClickEvent>(StartClient);

            m_DisconnectButton = m_Root.Q<Button>("DisconnectButton");
            m_DisconnectButton.RegisterCallback<ClickEvent>(Disconnect);

            m_QuitSceneButton = m_Root.Q<Button>("QuitSceneButton");
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
