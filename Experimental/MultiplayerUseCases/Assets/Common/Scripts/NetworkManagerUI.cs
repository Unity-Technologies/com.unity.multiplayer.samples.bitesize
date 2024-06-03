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
        static readonly Regex s_IPRegex = new Regex("\\b(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}\\b");
        const string k_DefaultIP = "127.0.0.1";
        const string k_DefaultServerListenAddress = "0.0.0.0"; //note: this is not safe for real world usage and would limit you to IPv4-only addresses, but this goes out the scope of this sample.

        static class UIElementNames
        {
            public const string addressInputField = "IPAddressTextField";
            public const string portInputField = "PorttextField";
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

        [SerializeField] Color32 m_HighlightedButtonColor;
        [SerializeField] Color32 m_DisabledButtonColor;
        [SerializeField] string m_SelectionScreenSceneName;
        [SerializeField] GameObject m_ServerOnlyOverlay;
        [SerializeField] TMPro.TMP_InputField m_IPAddressInputField;
        //[SerializeField] RectTransform m_LayoutToRebuild;
        //Button[] m_Buttons;

        void Awake()
        {
            var uiDocument = GetComponent<UIDocument>();
            m_Root = uiDocument.rootVisualElement;

            //m_AddressInputField = m_Root.Q<TextField>(UIElementNames.addressInputField);
            //m_AddressInputField.SetValueWithoutNotify(k_DefaultIP);

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


            //m_Buttons = new Button[] { m_ServerButton, m_HostButton, m_ClientButton };
            //m_IPAddressInputField.text = k_DefaultIP;
            //m_IPAddressInputField.onSubmit.AddListener(ValidateIP);
            //m_IPAddressInputField.onEndEdit.AddListener(ValidateIP);

            m_AddressInputField.value = k_DefaultIP;
            m_AddressInputField.SetValueWithoutNotify(k_DefaultIP);
        }

        void Start()
        {

            //SetButtonStateAndColor(m_DisconnectButton, false, false);
            m_ServerOnlyOverlay.gameObject.SetActive(false);
            //LayoutRebuilder.ForceRebuildLayoutImmediate(m_LayoutToRebuild); //nested layout groups need to be rebuilt at startup to work properly in UGUI.
        }

        void StartServer(ClickEvent evt)
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.ServerListenAddress = m_AddressInputField.value;
            NetworkManager.Singleton.StartServer();
            //EnableAndHighlightButtons(m_ServerButton, false);
            //SetButtonStateAndColor(m_DisconnectButton, false, true);
            m_ServerOnlyOverlay.gameObject.SetActive(true);
        }

        void StartHost(ClickEvent evt)
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.ServerListenAddress = m_AddressInputField.value;
            NetworkManager.Singleton.StartHost();
            //EnableAndHighlightButtons(m_HostButton, false);
            //SetButtonStateAndColor(m_DisconnectButton, false, true);
        }

        void StartClient(ClickEvent evt)
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = m_AddressInputField.value;
            NetworkManager.Singleton.StartClient();
            //EnableAndHighlightButtons(m_ClientButton, false);
            //SetButtonStateAndColor(m_DisconnectButton, false, true);
        }

        void Disconnect(ClickEvent evt)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                foreach (var item in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.ToList())
                {
                    item.Despawn(false);
                }
            }
            NetworkManager.Singleton.Shutdown();
            //EnableAndHighlightButtons(null, true);
            //SetButtonStateAndColor(m_DisconnectButton, false, false);
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

        /*void EnableAndHighlightButtons(Button buttonToHighlight, bool enable)
        {
            foreach (var button in m_Buttons)
            {
                SetButtonStateAndColor(button, button == buttonToHighlight, enable);
            }
        }*/

        /*void SetButtonStateAndColor(Button button, bool highlight, bool enable)
        {
            ColorBlock colors = button.colors;
            colors.disabledColor = highlight ? m_HighlightedButtonColor
                                             : m_DisabledButtonColor;
            button.colors = colors;
            button.interactable = enable;
        }*/

        void ValidateIP(string newIP)
        {
            if (string.IsNullOrEmpty(newIP) || !s_IPRegex.IsMatch(newIP))
            {
                Debug.LogError($"'{newIP}' is not a valid IP address, reverting to {k_DefaultIP}");
                m_IPAddressInputField.text = k_DefaultIP;
            }
        }

    }
}
