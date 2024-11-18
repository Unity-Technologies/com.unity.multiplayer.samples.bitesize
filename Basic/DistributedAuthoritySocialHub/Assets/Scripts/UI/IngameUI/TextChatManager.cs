using System;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Samples.SocialHub.UI
{
    public class TextChatManager : MonoBehaviour
    {
        [SerializeField]
        UIDocument m_UIDocument;

        [SerializeField]
        VisualTreeAsset m_Asset;

        // Serializable for Bindings.
        [SerializeField, HideInInspector]
        readonly List<ChatMessage> m_Messages = new();

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
            m_TextChatView = m_Root.Q<VisualElement>("text-chat");
            m_MessageView = m_Root.Q<ListView>("message-list");
            m_SendButton = m_Root.Q<Button>("submit");
            m_MessageInputField = m_Root.Q<TextField>("input-text");
            m_Root.Q<Button>("visibilty-button").clicked += ToggleChat;
            m_MessageInputField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if(evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
                    SendMessage();
            }, TrickleDown.TrickleDown);

            m_MessageView.dataSource = this;
            var dataBinding =  new DataBinding(){dataSourcePath = new PropertyPath("m_Messages")};
            dataBinding.bindingMode = BindingMode.TwoWay;
            m_MessageView.SetBinding("itemsSource",dataBinding);
            m_SendButton.clicked += SendMessage;

            SetViewFocusable(m_IsChatActive);
            BindSessionEvents(true);
        }

        void OnDestroy()
        {
            //m_ToggleChatAction.Disable();
            m_SendButton.clicked -= SendMessage;
            BindSessionEvents(false);
        }

        void ToggleChat()
        {
            m_IsChatActive = !m_IsChatActive;
            BindSessionEvents(m_IsChatActive);
            m_TextChatView.focusable = m_IsChatActive;
            m_TextChatView.RemoveFromClassList("text-chat--visible");
            if(m_IsChatActive)
                m_TextChatView.AddToClassList("text-chat--visible");

            SetViewFocusable(m_IsChatActive);
        }

        void SetViewFocusable(bool focusable)
        {
            m_MessageInputField.focusable = focusable;
            m_SendButton.focusable = focusable;
            m_MessageView.focusable = focusable;
        }

        void SendMessage()
        {
            if (!string.IsNullOrEmpty(m_MessageInputField.text))
            {
                GameplayEventHandler.SendTextMessage(m_MessageInputField.value);
                m_MessageInputField.value = "";
                m_MessageInputField.Focus();
            }
        }

        void BindSessionEvents(bool doBind)
        {
            if (doBind)
            {
                GameplayEventHandler.OnTextMessageReceived -= OnChannelMessageReceived;
                GameplayEventHandler.OnTextMessageReceived += OnChannelMessageReceived;
            }
            else
            {
                GameplayEventHandler.OnTextMessageReceived -= OnChannelMessageReceived;
            }
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

