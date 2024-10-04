using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    class SessionOwnerNetworkObjectSpawner : NetworkBehaviour
    {
        [SerializeField]
        NetworkObject m_NetworkObjectToSpawn;

        NetworkVariable<bool> m_IsRespawning = new NetworkVariable<bool>();

        NetworkVariable<int> m_TickToRespawn = new NetworkVariable<int>();

        public override void OnNetworkSpawn()
        {
            if (IsSessionOwner)
            {
                Spawn();
            }
        }

        public override void OnNetworkDespawn()
        {
            StopAllCoroutines();
        }

        void Spawn()
        {
            var spawnedNetworkObject = m_NetworkObjectToSpawn.InstantiateAndSpawn(NetworkManager, position: transform.position, rotation: transform.rotation);
            var spawnable = spawnedNetworkObject.GetComponent<ISpawnable>();
            spawnable.Init(this);
            m_IsRespawning.Value = false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="respawnTime"> Network tick at which to respawn this NetworkObject prefab </param>
        [Rpc(SendTo.Authority)]
        public void RespawnRpc(int respawnTime)
        {
            m_TickToRespawn.Value = respawnTime;
            m_IsRespawning.Value = true;
            StartCoroutine(WaitToRespawn());
        }

        IEnumerator WaitToRespawn()
        {
            yield return new WaitUntil(() => NetworkManager.NetworkTickSystem.ServerTime.Tick > m_TickToRespawn.Value);
            Spawn();
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            if (HasAuthority && m_IsRespawning.Value)
            {
                StartCoroutine(WaitToRespawn());
            }
            else
            {
                StopAllCoroutines();
            }
        }

        // add gizmo to show the spawn position
        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
        }
    }
}
