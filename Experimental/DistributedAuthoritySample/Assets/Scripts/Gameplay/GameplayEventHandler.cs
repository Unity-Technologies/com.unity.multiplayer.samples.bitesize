using Unity.Netcode;
using UnityEngine;
using System;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    abstract class GameplayEventHandler
    {
        internal static event Action<NetworkObject> OnNetworkObjectDespawned;
        internal static event Action<NetworkObject, ulong, ulong> OnNetworkObjectOwnershipChanged;

        internal static void NetworkObjectDespawned(NetworkObject networkObject)
        {
            OnNetworkObjectDespawned?.Invoke(networkObject);
        }

        internal static void NetworkObjectOwnershipChanged(NetworkObject networkObject, ulong previous, ulong current)
        {
            OnNetworkObjectOwnershipChanged?.Invoke(networkObject, previous, current);
        }
    }
}
