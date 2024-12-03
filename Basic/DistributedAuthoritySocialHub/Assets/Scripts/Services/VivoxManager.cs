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
        bool m_MicPermissionChecked;

        internal static VivoxManager Instance { get; private set; }

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

        IEnumerator RequestMicrophonePermissionsIOsMacOS()
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
            m_MicPermissionChecked = true;
        }

        internal async Task Initialize()
        {
            await VivoxService.Instance.InitializeAsync();
            BindGlobalEvents(true);
        }

        async void LoginVivox(Task t, string sessionName)
        {
#if UNITY_STANDALONE_OSX || UNITY_IOS
            // Vivox is not allowed to access the microphone on iOS and macOS without user permission.
            StartCoroutine(RequestMicrophonePermissionsIOsMacOS());
            while (m_MicPermissionChecked == false)
            {
                await Task.Yield();
            }
#endif
            m_TextChannelName = sessionName + "_text";
            m_VoiceChannelName = sessionName + "_voice";
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
            await JoinChannels(m_TextChannelName);
        }

        async Task JoinChannels(string _)
        {
            var positionalChannelProperties = new Channel3DProperties(10, 1, 1f, AudioFadeModel.InverseByDistance);
            BindChannelEvents(true);
            await VivoxService.Instance.JoinPositionalChannelAsync(m_VoiceChannelName, ChatCapability.AudioOnly, positionalChannelProperties);
            await VivoxService.Instance.JoinGroupChannelAsync(m_TextChannelName, ChatCapability.TextOnly);
        }

        void OnParticipantLeftChannel(VivoxParticipant vivoxParticipant)
        {
            // UI only needs to react to VoiceChannel participants.
            if (vivoxParticipant.ChannelName != m_VoiceChannelName)
                return;

            GameplayEventHandler.ParticipantLeftVoiceChat(vivoxParticipant);
        }

        void OnParticipantAddedToChannel(VivoxParticipant vivoxParticipant)
        {
            // UI only needs to react to VoiceChannel participants.
            if (vivoxParticipant.ChannelName != m_VoiceChannelName)
                return;

            GameplayEventHandler.ParticipantJoinedVoiceChat(vivoxParticipant);
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
            var senderName = UIUtils.ExtractPlayerNameFromAuthUserName(vivoxMessage.SenderDisplayName);
            GameplayEventHandler.ProcessTextMessageReceived(senderName, vivoxMessage.MessageText, vivoxMessage.FromSelf);
        }

        internal void SetPlayer3DPosition(GameObject avatar)
        {
            VivoxService.Instance.Set3DPosition(avatar, m_VoiceChannelName, false);
        }

        void BindGlobalEvents(bool doBind)
        {
            GameplayEventHandler.OnConnectToSessionCompleted -= LoginVivox;
            VivoxService.Instance.LoggedIn -= OnLoggedInVivox;
            VivoxService.Instance.ChannelJoined -= OnChannelJoined;
            GameplayEventHandler.OnExitedSession -= LogoutVivox;

            if (doBind)
            {
                GameplayEventHandler.OnConnectToSessionCompleted += LoginVivox;
                VivoxService.Instance.LoggedIn += OnLoggedInVivox;
                VivoxService.Instance.ChannelJoined += OnChannelJoined;
                GameplayEventHandler.OnExitedSession += LogoutVivox;
            }
        }

        void BindChannelEvents(bool doBind)
        {
            VivoxService.Instance.ParticipantAddedToChannel -= OnParticipantAddedToChannel;
            VivoxService.Instance.ParticipantRemovedFromChannel -= OnParticipantLeftChannel;
            GameplayEventHandler.OnSendTextMessage -= SendVivoxMessage;
            VivoxService.Instance.ChannelMessageReceived -= OnMessageReceived;

            if (doBind)
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
