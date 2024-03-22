using System;
using UnityEngine;
using UnityEngine.AI;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Handles the server-side logic of AI characters.
    /// Navigation code is based on the example shown in https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavAgentPatrol.html
    /// </summary>
    [RequireComponent(typeof(NetworkedAICharacter))]
    public class ServerAICharacter : MonoBehaviour
    {
        [SerializeField]
        NetworkedAICharacter m_NetworkedAICharacter;

        [SerializeField]
        NavMeshAgent m_NavMeshAgent;

        public Vector3[] PatrolPoints { get; set; }
        int m_PatrolIndex;

        const float k_ReachDist = 0.5f;

        void Awake()
        {
            m_NetworkedAICharacter.OnNetworkSpawnHook += OnNetworkSpawn;
        }

        void OnDestroy()
        {
            if (m_NetworkedAICharacter != null)
            {
                m_NetworkedAICharacter.OnNetworkSpawnHook -= OnNetworkSpawn;
            }
        }

        void OnNetworkSpawn()
        {
            m_PatrolIndex = 0;
            GotoNextPoint();
        }

        void GotoNextPoint()
        {
            if (PatrolPoints.Length == 0)
                return;

            m_NavMeshAgent.destination = PatrolPoints[m_PatrolIndex];

            m_PatrolIndex = (m_PatrolIndex + 1) % PatrolPoints.Length;
        }

        void Update()
        {
            if (!m_NavMeshAgent.pathPending && m_NavMeshAgent.remainingDistance < k_ReachDist)
            {
                GotoNextPoint();
            }

            m_NetworkedAICharacter.Speed = m_NavMeshAgent.velocity.magnitude;
        }
    }
}
