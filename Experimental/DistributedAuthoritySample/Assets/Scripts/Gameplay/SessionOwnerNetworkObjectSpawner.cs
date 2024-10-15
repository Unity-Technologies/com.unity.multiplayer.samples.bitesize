using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    class SessionOwnerNetworkObjectSpawner : NetworkBehaviour
    {
        [SerializeField]
        NetworkObject m_NetworkObjectToSpawn;

        NetworkVariable<bool> m_InitialSpawnComplete = new NetworkVariable<bool>();

        public override void OnNetworkSpawn()
        {
            if (IsSessionOwner && !m_InitialSpawnComplete.Value)
            {
                Spawn();
            }
        }

        void Spawn()
        {
            m_NetworkObjectToSpawn.InstantiateAndSpawn(NetworkManager, position: transform.position, rotation: transform.rotation);
            m_InitialSpawnComplete.Value = true;
        }

        // Add gizmo to show the spawn position of the network object
        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.848f, 0.501f, 0.694f));
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
    }
}
