using System;
using System.Collections.Generic;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Services
{
    public class TextChatManager : MonoBehaviour
    {
        [SerializeField]
        VisualTreeAsset m_Asset;

        ListView m_MessageView;
        TextField m_MessageInputField;
        Button m_SendButton;

        bool isChatActive = true;
        InputAction toggleChatAction;

        [SerializeField, HideInInspector]
        List<string> m_Messages = new List<string>();



        void Awake()
        {

            // Initialize the new input action
            toggleChatAction = new InputAction("ToggleChat", binding: "<Keyboard>/slash");
            toggleChatAction.performed += ctx => ToggleChat();
            toggleChatAction.Enable();
        }

        void Start()
        {
            var uiDoc = GetComponent<UIDocument>();
            m_MessageView = new ListView();
            m_MessageInputField = new TextField();
            m_SendButton = new Button();

            uiDoc.rootVisualElement.Add(m_MessageView);
            uiDoc.rootVisualElement.Add(m_MessageInputField);
            uiDoc.rootVisualElement.Add(m_SendButton);

            m_MessageView.makeItem = () =>
            {
                return new Label();
            };

            m_MessageView.bindItem = (element, i) =>
            {
                ((Label)(element)).text = m_Messages[i];
            };

            m_MessageView.dataSource = m_Messages;

            m_SendButton.clicked += SendMessage;
           // BindSessionEvents(true);
        }

        void OnDestroy()
        {
            toggleChatAction.Disable();
            m_SendButton.clicked -= SendMessage;
            BindSessionEvents(false);
        }

        private void ToggleChat()
        {
            isChatActive = !isChatActive;
        }

        private async void SendMessage()
        {
            if (!string.IsNullOrEmpty(m_MessageInputField.text))
            {
                await VivoxService.Instance.SendChannelTextMessageAsync(VivoxManager.Instance.SessionName, m_MessageInputField.value);
                m_MessageInputField.value = "";
            }
        }

        public void BindSessionEvents(bool doBind)
        {
            if (doBind)
            {
                VivoxService.Instance.ChannelMessageReceived += OnChannelMessageReceived;
            }
            else
            {
                VivoxService.Instance.ChannelMessageReceived -= OnChannelMessageReceived;
            }
        }

        private void OnChannelMessageReceived(VivoxMessage message)
        {
            var senderDisplayName = message.SenderDisplayName;
            var messageText = message.MessageText;
            m_Messages.Add($"{senderDisplayName}: {messageText}");
            foreach (var se in m_Messages)
            {
                Debug.Log(se+"\n");
            }
        }
    }
}

