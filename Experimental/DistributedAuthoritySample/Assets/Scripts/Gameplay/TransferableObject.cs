using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    class TransferableObject : NetworkBehaviour, IOwnershipRequestable
    {
        public GameObject LeftHand;
        public GameObject RightHand;
        private Collider m_Collider;

        public event Action<NetworkBehaviour, NetworkObject.OwnershipRequestResponseStatus> OnNetworkObjectOwnershipRequestResponse;

        public Vector3 GetCenterOffset()
        {
            return m_Collider.bounds.center;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            m_Collider = GetComponent<Collider>();
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
    }
}
