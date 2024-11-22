using System;
using System.Collections;
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

        string TextChannelName { get; set; }
        string VoiceChannelName { get; set; }

        PlayersTopUIController m_PlayersTopUIController;
        public PlayersTopUIController PlayersTopUIController
        {
            get {
                if (m_PlayersTopUIController == null)
                    m_PlayersTopUIController = FindFirstObjectByType<PlayersTopUIController>();

                return m_PlayersTopUIController;
            }
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                StartCoroutine(RequestMicrophonePermissions());
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        IEnumerator RequestMicrophonePermissions()
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
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
            TextChannelName = sessionName+"_text";
            VoiceChannelName = sessionName+"_voice";

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
            await JoinChannel(TextChannelName);
        }

        async Task JoinChannel(string channelName)
        {
            VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAddedToChannel;
            VivoxService.Instance.ParticipantRemovedFromChannel += OnParticipantLeftChannel;

            var positionalChannelProperties = new Channel3DProperties(10,1,1f,AudioFadeModel.InverseByDistance);

            await VivoxService.Instance.JoinPositionalChannelAsync(VoiceChannelName, ChatCapability.AudioOnly, positionalChannelProperties);
            await VivoxService.Instance.JoinGroupChannelAsync(TextChannelName, ChatCapability.TextOnly);

            var activeVoiceChatUsers = VivoxService.Instance.ActiveChannels[TextChannelName];

            foreach (var participant in activeVoiceChatUsers)
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
            if(vivoxParticipant.ChannelName != VoiceChannelName)
                return;

            if(PlayersTopUIController != null)
                PlayersTopUIController.RemoveVivoxParticipant(vivoxParticipant);
        }

        void OnParticipantAddedToChannel(VivoxParticipant vivoxParticipant)
        {
            if(vivoxParticipant.ChannelName != VoiceChannelName)
                return;

            if(PlayersTopUIController != null)
                PlayersTopUIController.ConnectVivoxParticipant(vivoxParticipant);
        }

        void OnChannelJoined(string channelName)
        {
            if(channelName == TextChannelName)
                GameplayEventHandler.SetTextChatReady(true, TextChannelName);
        }

        async void LogoutVivox()
        {
            GameplayEventHandler.SetTextChatReady(false, TextChannelName);
            await VivoxService.Instance.LogoutAsync();
        }

        async void SendVivoxMessage(string message)
        {
            await VivoxService.Instance.SendChannelTextMessageAsync(TextChannelName, message);
        }

        internal void SetPlayer3DPosition(GameObject avatar)
        {
            VivoxService.Instance.Set3DPosition(avatar, VoiceChannelName);
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
