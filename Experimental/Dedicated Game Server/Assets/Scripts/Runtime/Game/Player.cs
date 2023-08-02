using Unity.Netcode;
using Unity.Template.Multiplayer.NGO.Runtime.ApplicationLifecycle;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class Player : NetworkBehaviour
    {
        [ClientRpc]
        internal void OnClientPrepareGameClientRpc()
        {
            if (!IsLocalPlayer)
            {
                return;
            }
            MetagameApplication.Instance.Broadcast(new MatchEnteredEvent());
            Debug.Log("[LC] Preparing game [Showing loading screen]");
            if (!IsServer) //the server already does this before asking clients to do the same
            {
                ApplicationController.Singleton.InstantiateGameApplication();
            }
            OnClientReadyToStart();
        }

        internal void OnClientReadyToStart()
        {
            Debug.Log("[LC] Telling server I'm ready");
            OnServerNotifiedOfClientReadinessServerRpc();
        }

        [ServerRpc]
        internal void OnServerNotifiedOfClientReadinessServerRpc()
        {
            Debug.Log("[S] I'm ready");
            ApplicationController.Singleton.OnServerPlayerIsReady(this);
        }

        [ClientRpc]
        internal void OnClientStartGameClientRpc()
        {
            if (!IsLocalPlayer) { return; }
            GameApplication.Instance.Broadcast(new StartMatchEvent(false, true));
        }

        [ServerRpc]
        internal void OnPlayerAskedToWinServerRpc()
        {
            GameApplication.Instance.Broadcast(new EndMatchEvent(this));
        }
    }
}
