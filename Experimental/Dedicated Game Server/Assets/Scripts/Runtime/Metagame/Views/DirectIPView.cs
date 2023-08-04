using System.Text.RegularExpressions;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    internal class DirectIPView : View<MetagameApplication>
    {
        Button m_JoinButton;
        Button m_QuitButton;
        TextField m_IPTextField;
        TextField m_PortTextField;

        void Awake()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;
            m_JoinButton = root.Q<Button>("joinButton");
            m_QuitButton = root.Q<Button>("quitButton");
        }
        
        void OnEnable()
        {
            m_JoinButton.RegisterCallback<ClickEvent>(OnClickJoin);
            m_QuitButton.RegisterCallback<ClickEvent>(OnClickQuit);
            m_IPTextField.RegisterValueChangedCallback(OnIpAddressChanged);
            m_PortTextField.RegisterValueChangedCallback(OnPortChanged);
        }

        void OnDisable()
        {
            m_JoinButton.UnregisterCallback<ClickEvent>(OnClickJoin);
            m_QuitButton.UnregisterCallback<ClickEvent>(OnClickQuit);
            m_IPTextField.UnregisterValueChangedCallback(OnIpAddressChanged);
            m_PortTextField.UnregisterValueChangedCallback(OnPortChanged);
        }

        void OnIpAddressChanged(ChangeEvent<string> ipAddress)
        {
            SanitizeAndSetIpAddress(ipAddress.newValue);
        }
        
        void OnPortChanged(ChangeEvent<string> port)
        {
            SanitizeAndSetPort(port.newValue);
        }

        void SanitizeAndSetIpAddress(string ipAddressToSanitize)
        {
            var ipAddress = Sanitize(ipAddressToSanitize);
            m_IPTextField.value = ipAddress;
        }

        void SanitizeAndSetPort(string portToSanitize)
        {
            var sanitizedPort = Sanitize(portToSanitize);
            ushort.TryParse(sanitizedPort, out var parsedPort);
            m_PortTextField.value = parsedPort.ToString();
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

        void OnClickQuit(ClickEvent evt)
        {
            Broadcast(new ExitIPConnectionEvent());
        }

        void OnClickJoin(ClickEvent evt)
        {
            if (ushort.TryParse(m_PortTextField.value, out var port))
            {
                Broadcast(new JoinThroughDirectIPEvent { ipAddress = m_IPTextField.value, port = port });
            }
        }
    }
}
