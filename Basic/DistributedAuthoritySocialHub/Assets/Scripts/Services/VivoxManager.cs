using System;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using UnityEngine;
using Unity.Services.Vivox;

namespace Services
{
    public class VivoxManager : MonoBehaviour
    {
        public static VivoxManager Instance { get; private set; }
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

        public async Task LoginVivox(string playerName, string playerId)
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
            GameplayEventHandler.OnSendTextMessage -= SendVivoxMessage;
            GameplayEventHandler.OnSendTextMessage += SendVivoxMessage;
            VivoxService.Instance.ChannelMessageReceived -= OnMessageReceived;
            VivoxService.Instance.ChannelMessageReceived += OnMessageReceived;
        }

        static void OnMessageReceived(VivoxMessage vivoxMessage)
        {
            var senderName = vivoxMessage.SenderDisplayName.Split("#")[0];
            GameplayEventHandler.ProcessTextMessageReceived(senderName, vivoxMessage.MessageText, vivoxMessage.FromSelf);
        }

        async void SendVivoxMessage(string message)
        {
            await VivoxService.Instance.SendChannelTextMessageAsync(VivoxManager.Instance.SessionName, message);
        }

        void OnDestroy()
        {
            GameplayEventHandler.OnSendTextMessage -= SendVivoxMessage;
            VivoxService.Instance.ChannelMessageReceived -= OnMessageReceived;
        }
    }
}
