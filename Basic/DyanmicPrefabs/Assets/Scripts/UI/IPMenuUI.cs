using System;
using System.Text.RegularExpressions;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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

        void Awake()
        {
            SetupIPInputUI();

            // register buttons to methods using callbacks for when they're clicked 
            m_ButtonHost.clickable.clicked += StartHost;
            m_ButtonServer.clickable.clicked += StartServer;
            m_ButtonClient.clickable.clicked += StartClient;
            m_ButtonDisconnect.clickable.clicked += Disconnect;
        }

        void OnDestroy()
        {
            // un-register buttons from methods using callbacks for when they're clicked 
            m_ButtonHost.clickable.clicked -= StartHost;
            m_ButtonServer.clickable.clicked -= StartServer;
            m_ButtonClient.clickable.clicked -= StartClient;
            m_ButtonDisconnect.clickable.clicked -= Disconnect;

            NetworkManager.Singleton.OnClientDisconnectCallback -= SingletonOnOnClientDisconnectCallback;
        }

        void Start()
        {
            SetUIElementVisibility(m_IPMenuUIRoot, true);
            SetUIElementVisibility(m_ConnectionTypeUIRoot, false);

            NetworkManager.Singleton.OnClientDisconnectCallback += SingletonOnOnClientDisconnectCallback;
        }

        void SingletonOnOnClientDisconnectCallback(ulong obj)
        {
            // todo: add logic for server to not do it when a different client disconnects
            SetUIElementVisibility(m_IPMenuUIRoot, true);
            SetUIElementVisibility(m_ConnectionTypeUIRoot, false);
        }

        void StartHost()
        {
            SetUtpConnectionData();
            var result = NetworkManager.Singleton.StartHost();
            if (result)
            {
                SetUIElementVisibility(m_IPMenuUIRoot, false);
                SetUIElementVisibility(m_ConnectionTypeUIRoot, true);
                m_ConnectionTypeLabel.text = "Host";
            }
        }

        void StartClient()
        {
            SetUtpConnectionData();
            var result = NetworkManager.Singleton.StartClient();
            if (result)
            {
                SetUIElementVisibility(m_IPMenuUIRoot, false);
                SetUIElementVisibility(m_ConnectionTypeUIRoot, true);
                m_ConnectionTypeLabel.text = "Client";
            }
        }

        void StartServer()
        {
            SetUtpConnectionData();
            var result = NetworkManager.Singleton.StartServer();
            if (result)
            {
                SetUIElementVisibility(m_IPMenuUIRoot, false);
                SetUIElementVisibility(m_ConnectionTypeUIRoot, true);
                m_ConnectionTypeLabel.text = "Server";
            }
        }

        void Disconnect()
        {
            NetworkManager.Singleton.Shutdown();
            SetUIElementVisibility(m_IPMenuUIRoot, true);
            SetUIElementVisibility(m_ConnectionTypeUIRoot, false);
        }

        void SetUIElementVisibility(VisualElement element, bool isVisible)
        {
            element.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void SetUtpConnectionData()
        {
            var sanitizedIPText = Sanitize(m_IPInputField.text);
            var sanitizedPortText = Sanitize(m_PortInputField.text);

            ushort.TryParse(sanitizedPortText, out var port);

            var utp = (UnityTransport) NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(sanitizedIPText, port);
        }

        /// <summary>
        /// Sanitize user port InputField box allowing only alphanumerics and '.'
        /// </summary>
        /// <param name="dirtyString"> string to sanitize. </param>
        /// <returns> Sanitized text string. </returns>
        static string Sanitize(string dirtyString)
        {
            return Regex.Replace(dirtyString, "[^A-Za-z0-9.]", "");
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
