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

        NetworkVariable<float> m_TimeToRespawn = new NetworkVariable<float>();

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
        /// <param name="respawnTime"> Time.time at which to respawn this NetworkObject prefab </param>
        [Rpc(SendTo.Authority)]
        public void RespawnRpc(float respawnTime)
        {
            m_TimeToRespawn.Value = respawnTime;
            m_IsRespawning.Value = true;
            StartCoroutine(WaitToRespawn());
        }

        IEnumerator WaitToRespawn()
        {
            var timeRemaining = m_TimeToRespawn.Value - Time.time;
            yield return new WaitForSeconds(timeRemaining);
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
