using System;
using System.Linq;
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

        public ClientPreloadingState(ConnectionManager connectionManager)
        {
            m_ConnectionManager = connectionManager;
        }

        public override void Enter()
        {
            m_ConnectionManager.networkManager.Shutdown();
            HandleDisconnectReason(); 
        }

        async void HandleDisconnectReason()
        {
            var addressableGuidCollection = new AddressableGUIDCollection()
            {
                GUIDs = disconnectionPayload.guids.Select(item => new AddressableGUID() { Value = item }).ToArray()
            };

            await m_ConnectionManager.dynamicPrefabManager.LoadDynamicPrefabs(addressableGuidCollection);
            Debug.Log("Restarting client");
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientConnecting);
        }

        public override void Exit() { }
    }
}
