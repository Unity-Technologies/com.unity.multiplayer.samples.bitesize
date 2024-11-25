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
        string m_TextChannelName;
        string m_VoiceChannelName;

        internal static VivoxManager Instance { get; private set; }

        PlayersTopUIController m_PlayersTopUIController;
        PlayersTopUIController PlayersTopUIController
        {
            get
            {
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
            BindGlobalEvents(true);
        }

        async void LoginVivox(Task t, string sessionName)
        {
            m_TextChannelName = sessionName + "_text";
            m_VoiceChannelName = sessionName + "_voice";

            await VivoxService.Instance.InitializeAsync();
            var loginOptions = new LoginOptions()
            {
                ParticipantUpdateFrequency = ParticipantPropertyUpdateFrequency.OnePerSecond,
                DisplayName = AuthenticationService.Instance.PlayerName,
                PlayerId = AuthenticationService.Instance.PlayerId
            };
            await VivoxService.Instance.LoginAsync(loginOptions);
        }

        async void OnLoggedInVivox()
        {
            await JoinChannel(m_TextChannelName);
        }

        async Task JoinChannel(string channelName)
        {
            var positionalChannelProperties = new Channel3DProperties(10, 1, 1f, AudioFadeModel.InverseByDistance);

            await VivoxService.Instance.JoinPositionalChannelAsync(m_VoiceChannelName, ChatCapability.AudioOnly, positionalChannelProperties);
            await VivoxService.Instance.JoinGroupChannelAsync(m_TextChannelName, ChatCapability.TextOnly);

            BindChannelEvents(true);

            var activeVoiceChatUsers = VivoxService.Instance.ActiveChannels[m_TextChannelName];
            foreach (var participant in activeVoiceChatUsers)
            {
                OnParticipantAddedToChannel(participant);
            }
        }

        void OnParticipantLeftChannel(VivoxParticipant vivoxParticipant)
        {
            if (vivoxParticipant.ChannelName != m_VoiceChannelName)
                return;

            if (PlayersTopUIController != null)
                PlayersTopUIController.RemoveVivoxParticipant(vivoxParticipant);
        }

        void OnParticipantAddedToChannel(VivoxParticipant vivoxParticipant)
        {
            // UI only reacts to VoiceChannel participants.
            if (vivoxParticipant.ChannelName != m_VoiceChannelName)
                return;

            if (PlayersTopUIController != null)
                PlayersTopUIController.AttachVivoxParticipant(vivoxParticipant);
        }

        void OnChannelJoined(string channelName)
        {
            if (channelName == m_TextChannelName)
                GameplayEventHandler.SetTextChatReady(true, m_TextChannelName);
        }

        async void LogoutVivox()
        {
            GameplayEventHandler.SetTextChatReady(false, m_TextChannelName);
            await VivoxService.Instance.LogoutAsync();
        }

        async void SendVivoxMessage(string message)
        {
            await VivoxService.Instance.SendChannelTextMessageAsync(m_TextChannelName, message);
        }

        void OnMessageReceived(VivoxMessage vivoxMessage)
        {
            var senderName =   UIUtils.GetPlayerNameAuthenticationPlayerName(vivoxMessage.SenderDisplayName);
            GameplayEventHandler.ProcessTextMessageReceived(senderName, vivoxMessage.MessageText, vivoxMessage.FromSelf);
        }

        internal void SetPlayer3DPosition(GameObject avatar)
        {
            VivoxService.Instance.Set3DPosition(avatar, m_VoiceChannelName, false);
        }

        void BindGlobalEvents(bool bind)
        {
            GameplayEventHandler.OnConnectToSessionCompleted -= LoginVivox;
            VivoxService.Instance.LoggedIn -= OnLoggedInVivox;
            VivoxService.Instance.ChannelJoined -= OnChannelJoined;
            GameplayEventHandler.OnExitedSession -= LogoutVivox;

            if (bind)
            {
                GameplayEventHandler.OnConnectToSessionCompleted += LoginVivox;
                VivoxService.Instance.LoggedIn += OnLoggedInVivox;
                VivoxService.Instance.ChannelJoined += OnChannelJoined;
                GameplayEventHandler.OnExitedSession += LogoutVivox;
            }
        }

        void BindChannelEvents(bool bind)
        {
            VivoxService.Instance.ParticipantAddedToChannel -= OnParticipantAddedToChannel;
            VivoxService.Instance.ParticipantRemovedFromChannel -= OnParticipantLeftChannel;
            GameplayEventHandler.OnSendTextMessage -= SendVivoxMessage;
            VivoxService.Instance.ChannelMessageReceived -= OnMessageReceived;

            if (bind)
            {
                VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAddedToChannel;
                VivoxService.Instance.ParticipantRemovedFromChannel += OnParticipantLeftChannel;
                GameplayEventHandler.OnSendTextMessage += SendVivoxMessage;
                VivoxService.Instance.ChannelMessageReceived += OnMessageReceived;
            }
        }

        void OnDestroy()
        {
            BindGlobalEvents(false);
            BindChannelEvents(false);
        }
    }
}
