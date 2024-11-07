using System;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Services
{
    public class TextChatManager : MonoBehaviour
    {
        public InputField m_ChatInputField;
        public Text m_ChatDisplay;
        public Button m_SendButton;

        readonly string currentChannel = VivoxManager.Instance.SessionName;
        bool isChatActive = true;
        InputAction toggleChatAction;

        void Awake()
        {
            // Initialize the new input action
            toggleChatAction = new InputAction("ToggleChat", binding: "<Keyboard>/slash");
            toggleChatAction.performed += ctx => ToggleChat();
            toggleChatAction.Enable();
        }

        void Start()
        {
            m_SendButton.onClick.AddListener(SendMessage);
            m_ChatInputField.onEndEdit.AddListener(OnChatInputEndEdit);

            BindSessionEvents(true);
        }

        void OnDestroy()
        {
            toggleChatAction.Disable();
            m_SendButton.onClick.RemoveListener(SendMessage);
            m_ChatInputField.onEndEdit.RemoveListener(OnChatInputEndEdit);
            BindSessionEvents(false);
        }

        private void ToggleChat()
        {
            isChatActive = !isChatActive;
            m_ChatInputField.enabled = isChatActive;
            m_SendButton.enabled = isChatActive;
            Debug.Log("Chat: " + (isChatActive ? "Activated" : "Disabled"));
        }

        private async void SendMessage()
        {
            if (!string.IsNullOrEmpty(m_ChatInputField.text))
            {
                await VivoxService.Instance.SendChannelTextMessageAsync(currentChannel, m_ChatInputField.text);
                m_ChatInputField.text = "";
            }
        }

        // Modified to use new input system key detection
        private void OnChatInputEndEdit(string input)
        {
            if (Keyboard.current.enterKey.isPressed)
            {
                SendMessage();
            }
        }

        private void BindSessionEvents(bool doBind)
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

            m_ChatDisplay.text += $"{senderDisplayName}: {messageText}\n";
        }
    }
}

