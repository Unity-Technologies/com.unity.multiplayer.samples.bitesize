using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    public class ServerAISpawner : MonoBehaviour
    {
        [SerializeField]
        List<Patrol> m_Patrols;

        [SerializeField]
        GameObject m_CharacterPrefab;

        [SerializeField]
        NetworkedGameState m_NetworkedGameState;

        void Awake()
        {
            m_NetworkedGameState.OnMatchStarted += OnMatchStarted;
        }

        void OnDestroy()
        {
            m_NetworkedGameState.OnMatchStarted -= OnMatchStarted;
        }

        void OnMatchStarted()
        {
            foreach (var patrol in m_Patrols)
            {
                var characterGO = Instantiate(m_CharacterPrefab, patrol.PatrolPointsPositions[0], Quaternion.identity);
                characterGO.GetComponent<ServerAICharacter>().PatrolPoints = patrol.PatrolPointsPositions;
                characterGO.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}
