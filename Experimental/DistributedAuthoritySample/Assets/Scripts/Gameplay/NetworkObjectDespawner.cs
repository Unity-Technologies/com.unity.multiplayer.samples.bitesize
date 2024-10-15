using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    class NetworkObjectDespawner : NetworkBehaviour
    {
        [SerializeField]
        float m_SecondsUntilDespawn;

        NetworkVariable<int> m_DespawnTick = new NetworkVariable<int>();

        public override void OnNetworkSpawn()
        {
            if (HasAuthority)
            {
                m_DespawnTick.Value = NetworkManager.ServerTime.Tick + Mathf.RoundToInt(NetworkManager.ServerTime.TickRate * m_SecondsUntilDespawn);
            }
            OnOwnershipChanged(0L, 0L);
        }

        protected override void OnOwnershipChanged(ulong previous, ulong current)
        {
            if (HasAuthority)
            {
                StartCoroutine(DespawnCoroutine());
            }
            else
            {
                StopAllCoroutines();
            }
        }

        IEnumerator DespawnCoroutine()
        {
            yield return new WaitUntil(() => NetworkManager.NetworkTickSystem.ServerTime.Tick > m_DespawnTick.Value);
            // TODO: add hook to this NetworkObject's pool system
            NetworkObject.Despawn();
        }
    }
}
