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
        internal NetworkVariable<bool> matchEnded = new NetworkVariable<bool>();
        internal NetworkVariable<bool> matchStarted = new NetworkVariable<bool>();

        const uint k_CountdownStartValue = 60;
        const float k_ShutdownDelayAfterCountdownEnd = 30;
        
        Coroutine m_CountdownRoutine;
        
        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                matchStarted.Value = false;
                matchEnded.Value = false;
                ConnectionManager.EventManager.AddListener<MinNumberPlayersConnectedEvent>(OnServerMinNumberPlayersConnected);
                ConnectionManager.EventManager.AddListener<ClientConnectedEvent>(OnServerClientConnected);
                ConnectionManager.EventManager.AddListener<ClientDisconnectedEvent>(OnServerClientDisconnected);
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
            }
        }

        void OnServerMinNumberPlayersConnected(MinNumberPlayersConnectedEvent evt)
        {
            if (matchStarted.Value)
            {
                throw new Exception("[Server] Match has already started and received an unexpected MinNumberPlayersConnectedEvent");
            }
            Debug.Log("[Server] Starting match!");
            matchStarted.Value = true;
            OnServerStartCountdown();
        }

        void OnServerClientConnected(ClientConnectedEvent evt)
        {
            playersConnected.Value++;
        }

        void OnServerClientDisconnected(ClientDisconnectedEvent evt)
        {
            playersConnected.Value--;
        }

        void OnServerStartCountdown()
        {
            matchCountdown.Value = k_CountdownStartValue;
            m_CountdownRoutine = StartCoroutine(OnServerDoCountdown());
        }

        IEnumerator OnServerDoCountdown()
        {
            while (matchCountdown.Value > 0
                && !matchEnded.Value)
            {
                yield return CoroutinesHelper.OneSecond;
                matchCountdown.Value--;
            }
            OnServerCountdownExpired();
        }

        void OnServerCountdownExpired()
        {
            matchEnded.Value = true;
            if (m_CountdownRoutine != null)
            {
                StopCoroutine(m_CountdownRoutine);
                m_CountdownRoutine = null;
            }

            StartCoroutine(CoroutinesHelper.WaitAndDo(new WaitForSeconds(k_ShutdownDelayAfterCountdownEnd), () => ConnectionManager.RequestShutdown()));
        }
    }
}
