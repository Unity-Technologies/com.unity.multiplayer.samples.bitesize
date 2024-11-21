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
        string ChannelName { get; set; }

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
            GameplayEventHandler.OnConnectToSessionCompleted += LoginVivox;
            VivoxService.Instance.LoggedIn += OnLoggedInVivox;
            VivoxService.Instance.ChannelJoined += OnChannelJoined;
            GameplayEventHandler.OnExitedSession += LogoutVivox;
        }

        async void LoginVivox(Task t, string sessionName)
        {
            ChannelName = sessionName;
            await VivoxService.Instance.InitializeAsync();
            var loginOptions = new LoginOptions()
            {
                DisplayName = AuthenticationService.Instance.PlayerName,
                PlayerId = AuthenticationService.Instance.PlayerId
            };
            await VivoxService.Instance.LoginAsync(loginOptions);
        }

        async void OnLoggedInVivox()
        {
            await JoinChannel(ChannelName);
        }

        async Task JoinChannel(string channelName)
        {
            var channelOptions = new ChannelOptions();

            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.TextAndAudio, channelOptions);
            GameplayEventHandler.OnSendTextMessage -= SendVivoxMessage;
            GameplayEventHandler.OnSendTextMessage += SendVivoxMessage;
            VivoxService.Instance.ChannelMessageReceived -= OnMessageReceived;
            VivoxService.Instance.ChannelMessageReceived += OnMessageReceived;
        }

        void OnChannelJoined(string channelName)
        {
            GameplayEventHandler.SetTextChatReady(true);
        }

        async void LogoutVivox()
        {
            GameplayEventHandler.SetTextChatReady(false);
            await VivoxService.Instance.LogoutAsync();
        }

        async void SendVivoxMessage(string message)
        {
            await VivoxService.Instance.SendChannelTextMessageAsync(VivoxManager.Instance.ChannelName, message);
        }

        void OnMessageReceived(VivoxMessage vivoxMessage)
        {
            var senderName = vivoxMessage.SenderDisplayName.Split("#")[0];
            GameplayEventHandler.ProcessTextMessageReceived(senderName, vivoxMessage.MessageText, vivoxMessage.FromSelf);
        }

        void OnDestroy()
        {
            GameplayEventHandler.OnSendTextMessage -= SendVivoxMessage;
            VivoxService.Instance.ChannelMessageReceived -= OnMessageReceived;
            GameplayEventHandler.OnExitedSession -= LogoutVivox;
            GameplayEventHandler.OnConnectToSessionCompleted -= LoginVivox;
            VivoxService.Instance.LoggedIn -= OnLoggedInVivox;
        }
    }
}
