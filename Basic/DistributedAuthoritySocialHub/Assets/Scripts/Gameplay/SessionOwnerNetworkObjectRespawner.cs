using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    class SessionOwnerNetworkObjectRespawner : NetworkBehaviour
    {
        [SerializeField]
        float m_SecondsUntilRespawn;

        NetworkVariable<bool> m_IsRespawning = new NetworkVariable<bool>();

        NetworkVariable<int> m_TickToRespawn = new NetworkVariable<int>();

        Vector3 m_OriginalPosition;
        Quaternion m_OriginalRotation;

        void Awake()
        {
            m_OriginalPosition = transform.position;
            m_OriginalRotation = transform.rotation;
        }

        public override void OnDestroy()
        {
            StopAllCoroutines();
            base.OnDestroy();
        }

        void Spawn()
        {
            transform.SetPositionAndRotation(m_OriginalPosition, m_OriginalRotation);
            NetworkObject.Spawn(destroyWithScene: true);
            m_IsRespawning.Value = false;
        }

        public void Respawn()
        {
            m_TickToRespawn.Value = NetworkManager.NetworkTickSystem.ServerTime.Tick + Mathf.RoundToInt(m_SecondsUntilRespawn * NetworkManager.NetworkTickSystem.ServerTime.TickRate);
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
    }
}
