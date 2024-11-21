using System;
using System.Threading.Tasks;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Multiplayer.Samples.SocialHub.UI;
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
                SessionName = sessionName;
                await LoginVivox(AuthenticationService.Instance.PlayerName, AuthenticationService.Instance.PlayerId);
                await JoinChannel(sessionName);
                GameplayEventHandler.SetTextChatReady(true, sessionName);
            };
        }

        async Task LogoutVivox()
        {
            GameplayEventHandler.SetTextChatReady(false, "");
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

            VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAddedToChannel;
            VivoxService.Instance.ParticipantRemovedFromChannel += OnParticipantLeftChannel;

            var positionalChannelProperties = new Channel3DProperties(10,1,1f,AudioFadeModel.InverseByDistance);

            await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.AudioOnly, positionalChannelProperties);
            await VivoxService.Instance.JoinGroupChannelAsync(channelName+"_text", ChatCapability.TextOnly);

            var activeParticipants = VivoxService.Instance.ActiveChannels[channelName];

            foreach (var participant in activeParticipants)
            {
                OnParticipantAddedToChannel(participant);
            }

            GameplayEventHandler.OnSendTextMessage -= SendVivoxMessage;
            GameplayEventHandler.OnSendTextMessage += SendVivoxMessage;
            VivoxService.Instance.ChannelMessageReceived -= OnMessageReceived;
            VivoxService.Instance.ChannelMessageReceived += OnMessageReceived;
        }

        void OnParticipantLeftChannel(VivoxParticipant vivoxParticipant)
        {
            if(vivoxParticipant.ChannelName != SessionName)
                return;
            var controller = FindFirstObjectByType<PlayersTopUIController>();
            controller.RemoveVivoxParticipant(vivoxParticipant);
        }

        void OnParticipantAddedToChannel(VivoxParticipant vivoxParticipant)
        {
            if(vivoxParticipant.ChannelName != SessionName)
                return;
            var controller = FindFirstObjectByType<PlayersTopUIController>();
            controller.ConnectVivoxParticipant(vivoxParticipant);
        }

        static void OnMessageReceived(VivoxMessage vivoxMessage)
        {
            var senderName = vivoxMessage.SenderDisplayName.Split("#")[0];
            GameplayEventHandler.ProcessTextMessageReceived(senderName, vivoxMessage.MessageText, vivoxMessage.FromSelf);
        }

        async void SendVivoxMessage(string message)
        {
            await VivoxService.Instance.SendChannelTextMessageAsync(SessionName+"_text", message);
        }

        internal void SetPlayer3DPosition(GameObject avatar)
        {
            VivoxService.Instance.Set3DPosition(avatar, SessionName);
        }

        void OnDestroy()
        {
            GameplayEventHandler.OnSendTextMessage -= SendVivoxMessage;
            VivoxService.Instance.ChannelMessageReceived -= OnMessageReceived;
        }
    }
}
