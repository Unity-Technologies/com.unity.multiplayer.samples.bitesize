using System;
using System.Collections.Generic;
using Unity.Properties;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Services
{
    public class TextChatManager : MonoBehaviour
    {
        [SerializeField]
        UIDocument m_UIDocument;

        [SerializeField]
        VisualTreeAsset m_Asset;

        ListView m_MessageView;
        TextField m_MessageInputField;
        Button m_SendButton;

        bool isChatActive = true;
        InputAction toggleChatAction;

        [SerializeField]
        readonly List<ChatMessage> m_Messages = new List<ChatMessage>();
        VisualElement m_Root;
        VisualElement m_TextChatView;

        void Awake()
        {

            // Initialize the new input action
            toggleChatAction = new InputAction("ToggleChat", binding: "<Keyboard>/slash");
            toggleChatAction.performed += ctx => ToggleChat();
            toggleChatAction.Enable();
        }

        void Start()
        {
            m_Root = m_UIDocument.rootVisualElement.Q<VisualElement>("textchat-container");
            m_Asset.CloneTree(m_Root);
            m_TextChatView = m_Root.Q<VisualElement>("text-chat");
            m_MessageView = m_Root.Q<ListView>("message-list");
            m_SendButton = m_Root.Q<Button>("submit");
            m_MessageInputField = m_Root.Q<TextField>("input-text");
            m_Root.Q<Button>("visibilty-button").clicked += ToggleChat;

            m_MessageView.dataSource = this;
            var dataBinding =  new DataBinding(){dataSourcePath = new PropertyPath("m_Messages")};
            dataBinding.bindingMode = BindingMode.TwoWay;
            m_MessageView.SetBinding("itemsSource",dataBinding);
            m_SendButton.clicked += SendMessage;
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
            BindSessionEvents(isChatActive);
            m_TextChatView.RemoveFromClassList("text-chat--visible");
            if(isChatActive)
                m_TextChatView.AddToClassList("text-chat--visible");
        }

        private async void SendMessage()
        {
            if (!string.IsNullOrEmpty(m_MessageInputField.text))
            {
                await VivoxService.Instance.SendChannelTextMessageAsync(VivoxManager.Instance.SessionName, m_MessageInputField.value);
                m_MessageInputField.value = "";
            }
        }

        public async void Initialize()
        {
            BindSessionEvents(true);
            var lastMessages = await VivoxService.Instance.GetChannelTextMessageHistoryAsync(VivoxManager.Instance.SessionName, 100, null);
            foreach (var vivoxMessage in lastMessages)
            {
                m_Messages.Add(CreateChatMessage(vivoxMessage));
            }
        }

        void BindSessionEvents(bool doBind)
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

        void OnChannelMessageReceived(VivoxMessage message)
        {
            m_Messages.Add(CreateChatMessage(message));
        }

        ChatMessage CreateChatMessage(VivoxMessage vivoxMessage)
        {
            if (vivoxMessage.FromSelf)
            {
                return new ChatMessage("me:", vivoxMessage.MessageText);
            }

            return new ChatMessage(vivoxMessage.SenderDisplayName.Split('#')[0]+":", vivoxMessage.MessageText);
        }
    }

    struct ChatMessage
    {
        public string Name;
        public string Message;

        public ChatMessage(string name, string message)
        {
            Name = name;
            Message = message;
        }
    }
}

