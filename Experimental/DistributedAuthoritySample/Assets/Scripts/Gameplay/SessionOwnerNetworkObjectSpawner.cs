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
    }
}
