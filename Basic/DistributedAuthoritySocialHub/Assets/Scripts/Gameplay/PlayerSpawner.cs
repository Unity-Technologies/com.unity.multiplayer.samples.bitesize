using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    class PlayerSpawner : NetworkBehaviour
    {
        [SerializeField]
        NetworkObject m_PlayerPrefab;

        protected override void OnNetworkSessionSynchronized()
        {
            Debug.Assert(m_PlayerPrefab != null, $"Prefab reference '{nameof(m_PlayerPrefab)}' is missing or not assigned.");

            if (m_PlayerPrefab != null)
            {
                var spawnPoint = PlayerSpawnPoints.Instance.GetRandomSpawnPoint();
                m_PlayerPrefab.InstantiateAndSpawn(networkManager: NetworkManager, ownerClientId: NetworkManager.LocalClientId, isPlayerObject: true, position: spawnPoint.position, rotation: spawnPoint.rotation);
            }

            base.OnNetworkSessionSynchronized();
        }
    }
}
