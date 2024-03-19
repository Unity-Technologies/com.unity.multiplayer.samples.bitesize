using System;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Connection state corresponding to a client attempting to reconnect to a server once the server has determined
    /// that the client needs to preload assets locally. This state handles parsing the information stored inside
    /// the DisconnectionPayload received from NetworkManager and initiating the asynchronous process to preload assets.
    /// Once the assets have been loaded locally, it will transition to the ClientConnecting state to reattempt a
    /// connection. If the DisconnectReason is for whatever reason invalid, it will transition directly to the Offline
    /// state.
    /// </summary>
    class ClientPreloadingState : ConnectionState
    {
        public DisconnectionPayload disconnectionPayload;

        public ClientPreloadingState(OptionalConnectionManager connectionManager)
        {
            m_ConnectionManager = connectionManager;
        }

        public override void Enter()
        {
            m_ConnectionManager.m_NetworkManager.Shutdown();
            HandleDisconnectReason();
        }

        async void HandleDisconnectReason()
        {
            var addressableGuids = new AddressableGUID[disconnectionPayload.guids.Count];
            var index = 0;
            foreach (var guid in disconnectionPayload.guids)
            {
                addressableGuids[index] = new AddressableGUID() { Value = guid };
                index++;
            }

            await DynamicPrefabLoadingUtilities.LoadDynamicPrefabs(addressableGuids);
            Debug.Log("Restarting client");
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientConnecting);
        }

        public override void Exit() { }
    }
}
