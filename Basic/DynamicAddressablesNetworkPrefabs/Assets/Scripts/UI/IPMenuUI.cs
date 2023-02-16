using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    public class IPMenuUI : MonoBehaviour
    {
        // UI Documents
        [SerializeField]
        UIDocument m_IPMenuUIDocument;
        [SerializeField]
        UIDocument m_ConnectionTypeUIDocument;

        // UI Roots
        VisualElement m_IPMenuUIRoot;
        VisualElement m_ConnectionTypeUIRoot;

        // UI Elements
        TextField m_IPInputField;
        TextField m_PortInputField;
        Button m_ButtonHost;
        Button m_ButtonServer;
        Button m_ButtonClient;
        Button m_ButtonDisconnect;
        Label m_ConnectionTypeLabel;

        public string IpAddress { get; private set; } = "127.0.0.1";
        public ushort Port { get; private set; } = 9998;
                
        public event Action HostButtonPressed;
        
        public event Action ClientButtonPressed;
        
        public event Action DisconnectButtonPressed;

        void Awake()
        {
            SetupIPInputUI();

            // register UI elements to methods using callbacks for when they're clicked 
            m_ButtonHost.clickable.clicked += OnHostButtonPressed;
            m_ButtonClient.clickable.clicked += OnClientButtonPressed;
            m_ButtonDisconnect.clickable.clicked += OnDisconnectButtonPressed;
            m_IPInputField.RegisterValueChangedCallback(OnIpAddressChanged);
            m_PortInputField.RegisterValueChangedCallback(OnPortChanged);
        }

        void OnDestroy()
        {
            // un-register UI elements from methods using callbacks for when they're clicked 
            m_ButtonHost.clickable.clicked -= OnHostButtonPressed;
            m_ButtonClient.clickable.clicked -= OnClientButtonPressed;
            m_ButtonDisconnect.clickable.clicked -= OnDisconnectButtonPressed;
            m_IPInputField.UnregisterValueChangedCallback(OnIpAddressChanged);
            m_PortInputField.UnregisterValueChangedCallback(OnPortChanged);
        }

        void Start()
        {
            ResetUI();
            m_IPInputField.value = IpAddress;
            m_PortInputField.value = Port.ToString();
        }
        
        void OnHostButtonPressed()
        {
            HostButtonPressed?.Invoke();
        }
        
        void OnClientButtonPressed()
        {
            ClientButtonPressed?.Invoke();
        }
        
        void OnDisconnectButtonPressed()
        {
            DisconnectButtonPressed?.Invoke();
        }

        public void HostStarted()
        {
            SwitchToInGameUI("Host");
        }

        public void ClientStarted()
        {
            SwitchToInGameUI("Client");
        }
        
        public void HideIPConnectionMenu()
        {
            SetUIElementVisibility(m_IPMenuUIRoot, false);
        }

        void ServerStarted()
        {
            SwitchToInGameUI("Server");
        }

        void SwitchToInGameUI(string connectionType)
        {
            SetUIElementVisibility(m_IPMenuUIRoot, false);
            SetUIElementVisibility(m_ConnectionTypeUIRoot, true);
            m_ConnectionTypeLabel.text = connectionType;
        }

        public void DisconnectRequested()
        {
            ResetUI();
        }

        public void ResetUI()
        {
            SetUIElementVisibility(m_IPMenuUIRoot, true);
            SetUIElementVisibility(m_ConnectionTypeUIRoot, false);
        }

        void OnIpAddressChanged(ChangeEvent<string> ipAddress)
        {
            SanitizeAndSetIpAddress(ipAddress.newValue);
        }

        void OnPortChanged(ChangeEvent<string> port)
        {
            SanitizeAndSetPort(port.newValue);
        }

        void SanitizeAndSetPort(string portToSanitize)
        {
            var sanitizedPort = Sanitize(portToSanitize);
            m_PortInputField.value = sanitizedPort;
            ushort.TryParse(sanitizedPort, out var parsedPort);
            Port = parsedPort;
            m_PortInputField.value = Port.ToString();
        }

        void SanitizeAndSetIpAddress(string ipAddressToSanitize)
        {
            IpAddress = Sanitize(ipAddressToSanitize);
            m_IPInputField.value = IpAddress;
        }

        /// <summary>
        /// Sanitize user port InputField box allowing only alphanumerics and '.'
        /// </summary>
        /// <param name="stringToBeSanitized"> string to sanitize. </param>
        /// <returns> Sanitized text string. </returns>
        string Sanitize(string stringToBeSanitized)
        {
            return Regex.Replace(stringToBeSanitized, "[^A-Za-z0-9.]", "");
        }

        void SetUIElementVisibility(VisualElement element, bool isVisible)
        {
            element.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void SetupIPInputUI()
        {
            m_IPMenuUIRoot = m_IPMenuUIDocument.rootVisualElement;
            m_ConnectionTypeUIRoot = m_ConnectionTypeUIDocument.rootVisualElement;
            m_IPInputField = m_IPMenuUIRoot.Q<TextField>("IPAddressField");
            m_PortInputField = m_IPMenuUIRoot.Q<TextField>("PortField");
            m_ButtonHost = m_IPMenuUIRoot.Q<Button>("HostButton");
            m_ButtonServer = m_IPMenuUIRoot.Q<Button>("ServerButton");
            m_ButtonClient = m_IPMenuUIRoot.Q<Button>("ClientButton");
            m_ButtonDisconnect = m_ConnectionTypeUIRoot.Q<Button>("DisconnectButton");
            m_ConnectionTypeLabel = m_ConnectionTypeUIRoot.Q<Label>("ConnectionType");
        }
    }
}
