using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    class NetworkObjectDespawner : NetworkBehaviour
    {
        [SerializeField]
        float m_SecondsUntilDespawn;

        NetworkVariable<float> m_DespawnTime = new NetworkVariable<float>();

        public override void OnNetworkSpawn()
        {
            if (HasAuthority)
            {
                m_DespawnTime.Value = (float)NetworkManager.LocalTime.Time + m_SecondsUntilDespawn;
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
            var timeRemaining = m_DespawnTime.Value - Time.time;
            yield return new WaitForSeconds(timeRemaining);
            // TODO: add hook to this NetworkObject's pool system
            NetworkObject.Despawn();
        }
    }
}
