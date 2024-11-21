using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Multiplayer.Samples.SocialHub.GameManagement.GameplayEventHandler;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    class TextChatManager : MonoBehaviour
    {
        [SerializeField]
        UIDocument m_UIDocument;

        [SerializeField]
        VisualTreeAsset m_Asset;

        // Serializable for Bindings.
        [SerializeField, HideInInspector]
        List<ChatMessage> m_Messages = new();

        ListView m_MessageView;
        TextField m_MessageInputField;
        Button m_SendButton;
        VisualElement m_Root;
        VisualElement m_TextChatView;

        bool m_IsChatActive;

        void Start()
        {

            m_Root = m_UIDocument.rootVisualElement.Q<VisualElement>("textchat-container");
            m_Asset.CloneTree(m_Root);

            m_Root.Q<Button>("visibility-button").clicked += ToggleChat;
            m_TextChatView = m_Root.Q<VisualElement>("text-chat");

            m_SendButton = m_Root.Q<Button>("submit");
            m_SendButton.clicked += SendMessage;

            m_MessageInputField = m_Root.Q<TextField>("input-text");
            m_MessageInputField.RegisterCallback<FocusInEvent>(evt => BlockPlayerControls(true));
            m_MessageInputField.RegisterCallback<FocusOutEvent>(evt => BlockPlayerControls(false));

            m_MessageInputField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
                    SendMessage();
            }, TrickleDown.TrickleDown);

            m_MessageView = m_Root.Q<ListView>("message-list");
            m_MessageView.dataSource = this;
            m_MessageView.SetBinding("itemsSource", new DataBinding
            {
                dataSourcePath = new PropertyPath("m_Messages"),
                bindingMode = BindingMode.TwoWay
            });

            SetViewFocusable(m_IsChatActive);
            m_TextChatView.SetEnabled(false);
            BindSessionEvents(true);

            m_Messages.Clear();
            m_Messages.Add(new ChatMessage("Sample Devs", "Hey, we hope you enjoy our sample :)"));
        }

        void OnDestroy()
        {
            m_SendButton.clicked -= SendMessage;
            BindSessionEvents(false);
        }

        void ToggleChat()
        {
            m_IsChatActive = !m_IsChatActive;
            SetViewFocusable(m_IsChatActive);

            if (m_IsChatActive)
            {
                m_TextChatView.AddToClassList("text-chat--visible");
                return;
            }

            m_TextChatView.RemoveFromClassList("text-chat--visible");
        }

        void SetViewFocusable(bool focusable)
        {
            m_TextChatView.focusable = m_IsChatActive;
            m_MessageInputField.focusable = focusable;
            m_SendButton.focusable = focusable;
            m_MessageView.focusable = focusable;
        }

        void SendMessage()
        {
            if (!string.IsNullOrEmpty(m_MessageInputField.text))
            {
                SendTextMessage(m_MessageInputField.value);
                m_MessageInputField.value = "";
                m_MessageInputField.Focus();
            }
        }

        void BindSessionEvents(bool doBind)
        {
            if (doBind)
            {
                OnChatIsReady += OnOnChatIsReady;
                OnTextMessageReceived -= OnChannelMessageReceived;
                OnTextMessageReceived += OnChannelMessageReceived;
            }
            else
            {
                OnChatIsReady -= OnOnChatIsReady;
                OnTextMessageReceived -= OnChannelMessageReceived;
            }
        }

        void OnOnChatIsReady(bool isReady, string channelName)
        {
            m_TextChatView.SetEnabled(isReady);
        }

        void OnChannelMessageReceived(string sender, string message, bool fromSelf)
        {
            m_Messages.Add(fromSelf ? new ChatMessage("me", message) : new ChatMessage(sender, message));
        }
    }

    [Serializable]
    class ChatMessage
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
