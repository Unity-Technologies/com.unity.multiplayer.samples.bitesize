using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;

namespace Services
{
    public class TextChatManager : MonoBehaviour
    {
        public InputField m_ChatInputField;
        public Text m_ChatDisplay;
        public Button m_SendButton;

        private string currentChannel = VivoxManager.Instance.SessionName;

        void Start()
        {
            m_SendButton.onClick.AddListener(SendMessage);
            m_ChatInputField.onEndEdit.AddListener(OnChatInputEndEdit);

            BindSessionEvents(true);
        }

        private async void SendMessage()
        {
            if (!string.IsNullOrEmpty(m_ChatInputField.text))
            {
                await VivoxService.Instance.SendChannelTextMessageAsync(currentChannel, m_ChatInputField.text);
                m_ChatInputField.text = "";
            }
        }

        private void OnChatInputEndEdit(string input)
        {
            if (Input.GetKeyDown(KeyCode.Return))
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
