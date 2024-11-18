using Unity.Netcode;
using UnityEngine;
using System;
using System.Threading.Tasks;
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
        internal static event Action<Task> OnConnectToSessionCompleted;
        internal static event Action OnExitedSession;

        internal static event Action<string, string, bool> OnTextMessageReceived;
        internal static event Action<string> OnSendTextMessage;

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

        internal static void ConnectToSessionComplete(Task task)
        {
            OnConnectToSessionCompleted?.Invoke(task);
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
    }
}
