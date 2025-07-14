using System;
using System.Collections;
using Unity.DedicatedGameServerSample.Runtime.ApplicationLifecycle;
using Unity.DedicatedGameServerSample.Runtime.ConnectionManagement;
using Unity.Netcode;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Holds the logical state of a game and synchronizes it across the network
    /// </summary>
    public class NetworkedGameState : NetworkBehaviour
    {
        internal NetworkVariable<uint> matchCountdown = new NetworkVariable<uint>();
        internal NetworkVariable<int> playersConnected = new NetworkVariable<int>();
        bool m_MatchStarted;
        bool m_MatchEnded;

        internal event Action OnMatchStarted;
        internal event Action OnMatchEnded;


        const uint k_CountdownStartValue = 300;
        const float k_ShutdownDelayAfterCountdownEnd = 30;

        Coroutine m_CountdownRoutine;

        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                m_MatchEnded = false;
                ConnectionManager.EventManager.AddListener<MinNumberPlayersConnectedEvent>(OnServerMinNumberPlayersConnected);
                ConnectionManager.EventManager.AddListener<ClientConnectedEvent>(OnServerClientConnected);
                ConnectionManager.EventManager.AddListener<ClientDisconnectedEvent>(OnServerClientDisconnected);
                playersConnected.Value = NetworkManager.ConnectedClientsIds.Count;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                if (m_CountdownRoutine != null)
                {
                    StopCoroutine(m_CountdownRoutine);
                    m_CountdownRoutine = null;
                }
                ConnectionManager.EventManager.RemoveListener<MinNumberPlayersConnectedEvent>(OnServerMinNumberPlayersConnected);
                ConnectionManager.EventManager.RemoveListener<ClientConnectedEvent>(OnServerClientConnected);
                ConnectionManager.EventManager.RemoveListener<ClientDisconnectedEvent>(OnServerClientDisconnected);
            }
        }

        void OnServerMinNumberPlayersConnected(MinNumberPlayersConnectedEvent evt)
        {
            if (m_MatchStarted)
            {
                throw new Exception("[Server] Match has already started and received an unexpected MinNumberPlayersConnectedEvent");
            }
            Debug.Log("[Server] Starting match!");
            m_MatchStarted = true;
            OnServerStartCountdown();
            ClientStartMatchRpc();
            OnMatchStarted?.Invoke();
        }

        void OnServerClientConnected(ClientConnectedEvent evt)
        {
            playersConnected.Value = NetworkManager.ConnectedClientsIds.Count;
        }

        void OnServerClientDisconnected(ClientDisconnectedEvent evt)
        {
            playersConnected.Value = NetworkManager.ConnectedClientsIds.Count;
        }

        void OnServerStartCountdown()
        {
            matchCountdown.Value = k_CountdownStartValue;
            m_CountdownRoutine = StartCoroutine(OnServerDoCountdown());
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ClientStartMatchRpc()
        {
            OnMatchStarted?.Invoke();
        }

        IEnumerator OnServerDoCountdown()
        {
            while (matchCountdown.Value > 0
                && !m_MatchEnded)
            {
                yield return CoroutinesHelper.OneSecond;
                matchCountdown.Value--;
            }
            OnServerCountdownExpired();
        }

        void OnServerCountdownExpired()
        {
            m_MatchEnded = true;
            if (m_CountdownRoutine != null)
            {
                StopCoroutine(m_CountdownRoutine);
                m_CountdownRoutine = null;
            }

            ClientEndMatchRpc();
            StartCoroutine(CoroutinesHelper.WaitAndDo(new WaitForSeconds(k_ShutdownDelayAfterCountdownEnd), () => ConnectionManager.RequestShutdown()));
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ClientEndMatchRpc()
        {
            OnMatchEnded?.Invoke();
        }
    }
}
