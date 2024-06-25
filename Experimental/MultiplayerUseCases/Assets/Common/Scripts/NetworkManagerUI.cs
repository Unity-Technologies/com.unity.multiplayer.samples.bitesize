using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using UnityEditor.UIElements;
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
        VisualElement m_ServerVisualHighlight;
        VisualElement m_HostVisualHighlight;
        VisualElement m_ClientVisualHighlight;
        TextField m_AddressInputField;
        TextField m_PortInputField;
        Button m_ServerButton;
        Button m_HostButton;
        Button m_ClientButton;
        Button m_DisconnectButton;
        Button m_QuitSceneButton;
        Button[] m_Buttons;

        [SerializeField] Color m_ButtonHighlightedColor;
        [SerializeField] Color m_DisabledButtonColor;
        [SerializeField] Color m_ButtonOutlineHighlightedColor;
        [SerializeField] Color m_ButtonOutlineDisabledColor;
        [SerializeField] Color m_ButtonOutlineColor;
        [SerializeField] string m_SelectionScreenSceneName;
        [SerializeField] GameObject m_ServerOnlyOverlay;

        void Awake()
        {
            var uiDocument = GetComponent<UIDocument>();
            m_Root = uiDocument.rootVisualElement;

            m_ServerVisualHighlight = m_Root.Q<VisualElement>("ServerVisualHighlight");
            m_HostVisualHighlight = m_Root.Q<VisualElement>("HostVisualHighlight");
            m_ClientVisualHighlight = m_Root.Q<VisualElement>("ClientVisualHighlight");
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
            EnableAndHighlightButtons(m_ServerButton, false);
            SetNetworkPortAndAddress(ushort.Parse(m_PortInputField.value), m_AddressInputField.value, k_DefaultServerListenAddress);
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.ServerListenAddress = m_AddressInputField.value;
            NetworkManager.Singleton.StartServer();
            m_ServerOnlyOverlay.gameObject.SetActive(true);
            m_DisconnectButton.SetEnabled(true);

            /*Button[] buttons = new Button[] {m_ClientButton, m_HostButton};
            EnableButtons(buttons, false);
            m_ServerVisualHighlight.visible = true;*/
        }

        void StartHost()
        {
            SetNetworkPortAndAddress(ushort.Parse(m_PortInputField.value), m_AddressInputField.value, k_DefaultServerListenAddress);
            NetworkManager.Singleton.StartHost();
            Button[] buttons = new Button[] { m_ClientButton, m_ServerButton };
            EnableButtons(buttons, false);
            m_DisconnectButton.SetEnabled(true);
            m_HostVisualHighlight.visible = true;
        }

        void StartClient()
        {
            SetNetworkPortAndAddress(ushort.Parse(m_PortInputField.value), m_AddressInputField.value, k_DefaultServerListenAddress);
            NetworkManager.Singleton.StartClient();
            Button[ ] buttons = new Button[]{m_HostButton, m_ServerButton};
            EnableButtons(buttons, false);
            m_DisconnectButton.SetEnabled(true);
            m_ClientVisualHighlight.visible = true;
        }

        void Disconnect()
        {
            EnableAndHighlightButtons(null, true);
            NetworkManager.Singleton.Shutdown();
            m_ServerOnlyOverlay.gameObject.SetActive(false);
            m_ServerVisualHighlight.visible = false;
            m_HostVisualHighlight.visible = false;
            m_ClientVisualHighlight.visible = false;
            Button[ ] buttons = new Button[]{m_HostButton, m_ServerButton, m_ClientButton};
            EnableButtons(buttons, true);
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
            if (string.IsNullOrEmpty(address) || !NetworkEndPoint.TryParse(address, port, out NetworkEndPoint networkEndPoint))
            {
                Debug.LogError($"IP address '{address}' or port '{port}', are not valid. Reverting IP address to {k_DefaultIP} and reverting port to {k_DefaultPort}");
                address = k_DefaultIP;
                port = k_DefaultPort;
                m_AddressInputField.SetValueWithoutNotify(k_DefaultIP);
                m_PortInputField.SetValueWithoutNotify(k_DefaultPort.ToString());
            }

            var transport = GetComponent<UnityTransport>();
            if (transport == null) //happens during Play Mode Tests
            {
                return;
            }
            transport.SetConnectionData(address, port, serverListenAddress);
        }

        void EnableButtons(Button[] buttonsToEnable, bool enabled)
        {
            foreach (var button in buttonsToEnable )
            {
                button.SetEnabled(enabled);
            }
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
            StyleColor colors = button.style.backgroundColor;
            colors = highlight ? m_ButtonHighlightedColor
                : m_DisabledButtonColor;
            button.style.backgroundColor = colors;
            button.SetEnabled(enable);

            var buttonOutline = button.style.borderBottomColor;
            buttonOutline= enable ? m_ButtonOutlineColor : m_ButtonOutlineDisabledColor;
            buttonOutline = highlight ? m_ButtonOutlineHighlightedColor : buttonOutline;
        }
    }
}
