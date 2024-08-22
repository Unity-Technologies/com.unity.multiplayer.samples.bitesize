using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    class EnvironmentTransform : NetworkTransform, IOwnershipRequestable, IGameplayEventInvokable
    {
        public event Action<NetworkObject, NetworkObject.OwnershipRequestResponseStatus> OnNetworkObjectOwnershipRequestResponse;

        public event Action<NetworkObject, GameplayEvent> OnGameplayEvent;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            NetworkObject.OnOwnershipRequested += OnOwnershipRequested;
            NetworkObject.OnOwnershipRequestResponse += OnOwnershipRequestResponse;
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkObject)
            {
                NetworkObject.OnOwnershipRequested -= OnOwnershipRequested;
                NetworkObject.OnOwnershipRequestResponse -= OnOwnershipRequestResponse;
            }

            OnGameplayEvent?.Invoke(NetworkObject, GameplayEvent.Despawned);
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            base.OnOwnershipChanged(previous, current);

            OnGameplayEvent?.Invoke(NetworkObject, GameplayEvent.OwnershipChange);
        }

        // note: invoked on owning client
        bool OnOwnershipRequested(ulong clientRequesting)
        {
            // defaulting all ownership requests to true, as is the default for all ownership requests
            // here, you'd introduce game-based logic to deny/approve requests
            return true;
        }

        // note: invoked on requesting client
        void OnOwnershipRequestResponse(NetworkObject.OwnershipRequestResponseStatus ownershipRequestResponse)
        {
            OnNetworkObjectOwnershipRequestResponse?.Invoke(NetworkObject, ownershipRequestResponse);
        }
    }
}
