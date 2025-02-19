using System;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to when a client is attempting to connect to a server. Starts the client when
    /// entering. If successful, transitions to the ClientConnected state. If not, transitions to the Offline state.
    /// </summary>
    class ClientConnectingState : OnlineState
    {
        ConnectionMethodBase m_ConnectionMethod;

        public void Configure(ConnectionMethodBase baseConnectionMethod)
        {
            m_ConnectionMethod = baseConnectionMethod;
        }

        public override void Enter()
        {
            ConnectionManager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.Connecting });
            ConnectClient();
        }

        public override void Exit() { }

        public override void OnClientConnected(ulong clientId)
        {
            ConnectionManager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.Success });
            ConnectionManager.ChangeState(ConnectionManager.m_ClientConnected);
        }

        public override void OnClientDisconnect(ulong clientId)
        {
            // client ID is for sure ours here
            StartingClientFailed();
        }

        void StartingClientFailed()
        {
            var disconnectReason = ConnectionManager.NetworkManager.DisconnectReason;
            var connectStatus = string.IsNullOrEmpty(disconnectReason) ||
                (disconnectReason != k_HostDisconnectReason && disconnectReason != k_ServerDisconnectReason)
                    ? ConnectStatus.GenericDisconnect
                    : ConnectStatus.ServerEndedSession;
            ConnectionManager.EventManager.Broadcast(new ConnectionEvent { status = connectStatus });
            ConnectionManager.ChangeState(ConnectionManager.m_Offline);
        }

        async void ConnectClient()
        {
            try
            {
                m_ConnectionMethod.SetupClientConnection();
                await m_ConnectionMethod.ConnectClientAsync();
            }
            catch (Exception e)
            {
                Debug.LogError("Error connecting client, see following exception");
                Debug.LogException(e);
                StartingClientFailed();
                throw;
            }
        }
    }
}
