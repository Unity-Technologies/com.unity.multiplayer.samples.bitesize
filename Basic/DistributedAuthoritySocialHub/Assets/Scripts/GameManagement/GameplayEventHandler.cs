using Unity.Netcode;
using System;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Multiplayer.Samples.SocialHub.GameManagement
{
    static class GameplayEventHandler
    {
        internal static event Action<NetworkObject> OnNetworkObjectDespawned;
        internal static event Action<NetworkObject, ulong, ulong> OnNetworkObjectOwnershipChanged;
        internal static event Action<string, string> OnStartButtonPressed;
        internal static event Action OnReturnToMainMenuButtonPressed;
        internal static event Action OnQuitGameButtonPressed;
        internal static event Action<Task, string> OnConnectToSessionCompleted;
        internal static event Action OnExitedSession;
        internal static event Action<string, string, bool> OnTextMessageReceived;
        internal static event Action<string> OnSendTextMessage;
        internal static event Action<bool, string> OnChatIsReady;
        internal static event Action<VivoxParticipant> OnParticipantJoinedVoiceChat;
        internal static event Action<VivoxParticipant> OnParticipantLeftVoiceChat;

        internal static event Action<PickupState, Transform> OnPickupStateChanged;

        internal static void NetworkObjectDespawned(NetworkObject networkObject)
        {
            OnNetworkObjectDespawned?.Invoke(networkObject);
        }

        internal static void NetworkObjectOwnershipChanged(NetworkObject networkObject, ulong previous, ulong current)
        {
            OnNetworkObjectOwnershipChanged?.Invoke(networkObject, previous, current);
        }

        internal static void StartButtonPressed(string playerName, string sessionName)
        {
            OnStartButtonPressed?.Invoke(playerName, sessionName);
        }

        internal static void ReturnToMainMenuPressed()
        {
            OnReturnToMainMenuButtonPressed?.Invoke();
        }

        internal static void QuitGamePressed()
        {
            OnQuitGameButtonPressed?.Invoke();
        }

        internal static void ConnectToSessionComplete(Task task, string sessionName)
        {
            OnConnectToSessionCompleted?.Invoke(task, sessionName);
        }

        internal static void ExitedSession()
        {
            OnExitedSession?.Invoke();
        }

        internal static void LoadMainMenuScene()
        {
            SceneManager.LoadScene("MainMenu");
        }

        internal static void LoadInGameScene()
        {
            SceneManager.LoadScene("HubScene_TownMarket");
        }

        internal static void ProcessTextMessageReceived(string senderName, string message, bool fromSelf)
        {
            OnTextMessageReceived?.Invoke(senderName, message, fromSelf);
        }

        internal static void SendTextMessage(string message)
        {
            OnSendTextMessage?.Invoke(message);
        }

        public static void SetTextChatReady(bool enabled, string channelName)
        {
            OnChatIsReady?.Invoke(enabled, channelName);
        }

        public static void ParticipantJoinedVoiceChat(VivoxParticipant vivoxParticipant)
        {
            OnParticipantJoinedVoiceChat?.Invoke(vivoxParticipant);
        }

        public static void ParticipantLeftVoiceChat(VivoxParticipant vivoxParticipant)
        {
            OnParticipantLeftVoiceChat?.Invoke(vivoxParticipant);
        }

        internal static void SetAvatarPickupState(PickupState state, Transform pickup)
        {
            OnPickupStateChanged?.Invoke(state, pickup);
        }
    }

    internal enum PickupState
    {
        Inactive,
        PickupInRange,
        Carry
    }
}
