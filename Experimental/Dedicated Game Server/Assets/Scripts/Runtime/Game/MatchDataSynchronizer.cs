using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    /// <summary>
    /// Holds the logical state of a game and synchronizes it across the network
    /// </summary>
    public class MatchDataSynchronizer : NetworkBehaviour
    {
        internal NetworkVariable<uint> matchCountdown = new NetworkVariable<uint>();
        internal NetworkVariable<bool> matchEnded = new NetworkVariable<bool>();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsClient)
            {
                matchCountdown.OnValueChanged += OnClientMatchCountdownChanged;
                matchEnded.OnValueChanged += OnClientMatchEndedChanged;
            }
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

        [ClientRpc]
        internal void OnClientMatchResultComputedClientRpc(ulong winnerClientId)
        {
            GameApplication.Instance.Broadcast(new MatchResultComputedEvent(winnerClientId));
        }
    }
}
