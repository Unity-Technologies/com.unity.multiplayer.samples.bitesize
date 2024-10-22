using Unity.Netcode;
using UnityEngine;
using System;
using System.Threading.Tasks;

namespace Unity.Multiplayer.Samples.SocialHub.GameManagement
{
    static class GameplayEventHandler
    {
        internal static event Action<NetworkObject> OnNetworkObjectDespawned;
        internal static event Action<NetworkObject, ulong, ulong> OnNetworkObjectOwnershipChanged;
        internal static event Action<string> OnStartButtonPressed;
        internal static event Action OnReturnToMainMenuButtonPressed;
        internal static event Action OnQuitGameButtonPressed;
        internal static event Action<Task> OnConnectToSessionCompleted;

        internal static void NetworkObjectDespawned(NetworkObject networkObject)
        {
            OnNetworkObjectDespawned?.Invoke(networkObject);
        }

        internal static void NetworkObjectOwnershipChanged(NetworkObject networkObject, ulong previous, ulong current)
        {
            OnNetworkObjectOwnershipChanged?.Invoke(networkObject, previous, current);
        }

        internal static void StartButtonPressed(string sessionName)
        {
            OnStartButtonPressed?.Invoke(sessionName);
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
    }
}
