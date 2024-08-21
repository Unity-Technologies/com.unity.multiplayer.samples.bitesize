using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    class DestructibleObject : NetworkBehaviour
    {
        [SerializeField]
        NetworkVariable<int> m_Impacts = new NetworkVariable<int>();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            enabled = HasAuthority;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            Debug.Log($"previous {previous} current {current}");
            enabled = HasAuthority;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!HasAuthority)
            {
                return;
            }

            if (collision.gameObject.layer == gameObject.layer)
            {
                Debug.Log($"Hit {collision.gameObject.name} of owner {collision.gameObject.GetComponent<NetworkObject>().OwnerClientId} collision.impulse.magnitude {collision.impulse.magnitude}");
                m_Impacts.Value += 1;
            }
        }
    }
}
