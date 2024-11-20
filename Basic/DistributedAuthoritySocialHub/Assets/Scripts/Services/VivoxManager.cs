using System;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Services.Authentication;
using UnityEngine;
using Unity.Services.Vivox;

namespace Unity.Multiplayer.Samples.SocialHub.Services
{
    class VivoxManager : MonoBehaviour
    {
        internal static VivoxManager Instance { get; set; }
        string SessionName { get; set; }

        void Awake()
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

        internal async Task Initialize()
        {
            await VivoxService.Instance.InitializeAsync();
            GameplayEventHandler.OnExitedSession += async () => await LogoutVivox();
            GameplayEventHandler.OnConnectToSessionCompleted += async (Task t, string sessionName) =>
            {
                await LoginVivox(AuthenticationService.Instance.PlayerName, AuthenticationService.Instance.PlayerId);
                await JoinChannel(sessionName);
                GameplayEventHandler.SetTextChatReady(true);
            };
        }

        async Task LogoutVivox()
        {
            GameplayEventHandler.SetTextChatReady(false);
            await VivoxService.Instance.LogoutAsync();
        }

        async Task LoginVivox(string playerName, string playerId)
        {
            await VivoxService.Instance.InitializeAsync();
            var loginOptions = new LoginOptions()
            {
                DisplayName = playerName,
                PlayerId = playerId,
            };
            await VivoxService.Instance.LoginAsync(loginOptions);
        }

        async Task JoinChannel(string channelName)
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
