using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using UnityEngine;
using Unity.Services.Vivox;

namespace Services
{
    public class VivoxManager : MonoBehaviour
    {
        public static VivoxManager Instance { get; private set; }

        public static string PlayerProfileName { get; private set; }
        public string SessionName { get; set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async void LoginVivox(string playerName, string playerId)
        {
            await VivoxService.Instance.InitializeAsync();
            var loginOptions = new LoginOptions()
            {
                DisplayName = playerName,
                PlayerId = playerId,
            };
            await VivoxService.Instance.LoginAsync(loginOptions);
        }

        public async void JoinChannel(string channelName)
        {
            SessionName = channelName;
            var channelOptions = new ChannelOptions();
            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.TextAndAudio, channelOptions);
            GameplayEventHandler.OnSendTextMessage += SendVivoxMessage;
            VivoxService.Instance.ChannelMessageReceived += OnMessageReceived;
        }

        static void OnMessageReceived(VivoxMessage vivoxMessage)
        {
            GameplayEventHandler.ProcessTextMessageReceived(vivoxMessage.SenderDisplayName, vivoxMessage.MessageText, vivoxMessage.FromSelf);
        }

        async void FetchLatestTextMessages()
        {
            var lastMessages = await VivoxService.Instance.GetChannelTextMessageHistoryAsync(VivoxManager.Instance.SessionName, 100, null);
            foreach (var vivoxMessage in lastMessages)
            {
                GameplayEventHandler.ProcessTextMessageReceived(vivoxMessage.SenderDisplayName, vivoxMessage.MessageText, vivoxMessage.FromSelf);
            }
        }

        async void SendVivoxMessage(string message)
        {
            await VivoxService.Instance.SendChannelTextMessageAsync(VivoxManager.Instance.SessionName, message);
        }
    }
}
