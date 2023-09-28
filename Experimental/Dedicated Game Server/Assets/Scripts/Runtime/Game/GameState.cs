using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Holds the logical state of a game and synchronizes it across the network
    /// </summary>
    public class GameState : NetworkBehaviour
    {
        internal NetworkVariable<uint> matchCountdown = new NetworkVariable<uint>();
        internal NetworkVariable<bool> matchEnded = new NetworkVariable<bool>();
        internal NetworkVariable<bool> matchStarted = new NetworkVariable<bool>();

        [SerializeField]
        GameApplication m_GameApp;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsClient)
            {
                matchCountdown.OnValueChanged += OnClientMatchCountdownChanged;
                matchEnded.OnValueChanged += OnClientMatchEndedChanged;
                matchStarted.OnValueChanged += OnClientMatchStartedChanged;
                MetagameApplication.Instance.Broadcast(new MatchEnteredEvent());
                m_GameApp.Broadcast(new StartMatchEvent(false, true));

            }
        }

        public override void OnNetworkDespawn()
        {
            matchCountdown.OnValueChanged -= OnClientMatchCountdownChanged;
            matchEnded.OnValueChanged -= OnClientMatchEndedChanged;
            matchStarted.OnValueChanged -= OnClientMatchStartedChanged;
        }

        void OnClientMatchCountdownChanged(uint previousValue, uint newValue)
        {
            GameApplication.Instance.Broadcast(new CountdownChangedEvent(newValue));
        }

        void OnClientMatchEndedChanged(bool previousValue, bool newValue)
        {
            //you can block inputs here, play animations and so on
            Debug.Log($"New match ended value: {newValue}");
        }

        void OnClientMatchStartedChanged(bool previousValue, bool newValue)
        {
            //you can enable inputs here, play animations and so on
            Debug.Log($"New match started value: {newValue}");
        }

        [ClientRpc]
        internal void OnClientMatchResultComputedClientRpc(ulong winnerClientId)
        {
            GameApplication.Instance.Broadcast(new MatchResultComputedEvent(winnerClientId));
        }
    }
}
