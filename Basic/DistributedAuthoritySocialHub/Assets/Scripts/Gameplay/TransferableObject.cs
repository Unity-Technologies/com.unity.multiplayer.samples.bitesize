using System;
using Unity.Multiplayer.Samples.SocialHub.GameManagement;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    class TransferableObject : NetworkBehaviour, IOwnershipRequestable
    {
        public GameObject LeftHand;
        public GameObject RightHand;

        public event Action<NetworkBehaviour, NetworkObject.OwnershipRequestResponseStatus> OnNetworkObjectOwnershipRequestResponse;

        internal ObjectState CurrentObjectState { get; private set; }

        public override void OnNetworkSpawn()
        {
            if (HasAuthority)
            {
                NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Distributable, clearAndSet: true);
                NetworkObject.SetOwnershipStatus(NetworkObject.OwnershipStatus.Transferable);
            }

            base.OnNetworkSpawn();

            NetworkObject.OnOwnershipRequested += OnOwnershipRequested;
            NetworkObject.OnOwnershipRequestResponse += OnOwnershipRequestResponse;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (NetworkObject)
            {
                NetworkObject.OnOwnershipRequested -= OnOwnershipRequested;
                NetworkObject.OnOwnershipRequestResponse -= OnOwnershipRequestResponse;
            }

            GameplayEventHandler.NetworkObjectDespawned(NetworkObject);
            OnNetworkObjectOwnershipRequestResponse = null;
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            base.OnOwnershipChanged(previous, current);

            GameplayEventHandler.NetworkObjectOwnershipChanged(NetworkObject, previous, current);
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
            OnNetworkObjectOwnershipRequestResponse?.Invoke(this, ownershipRequestResponse);
        }

        internal void SetObjectState(ObjectState state)
        {
            CurrentObjectState = state;
        }

        public enum ObjectState
        {
            AtRest,
            PickedUp,
            Thrown
        }
    }
}
