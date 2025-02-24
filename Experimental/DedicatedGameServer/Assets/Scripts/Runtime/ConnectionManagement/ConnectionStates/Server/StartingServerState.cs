using System;
using Unity.Multiplayer;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to a server starting up. Starts the server when entering the state. If successful,
    /// transitions to the ServerListening state, if not, transitions back to the Offline state.
    /// </summary>
    class StartingServerState : OnlineState
    {
        ConnectionMethodBase m_ConnectionMethod;

        public void Configure(ConnectionMethodBase baseConnectionMethod)
        {
            m_ConnectionMethod = baseConnectionMethod;
        }

        public override void Enter()
        {
            StartServer();
        }

        public override void Exit(){ }

        public override void OnServerStarted()
        {
            ConnectionManager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.Success });
            ConnectionManager.ChangeState(ConnectionManager.m_ServerListening);
        }

        public override void OnServerStopped()
        {
            StartServerFailed();
        }

        void StartServerFailed()
        {
            ConnectionManager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.StartServerFailed });
            ConnectionManager.ChangeState(ConnectionManager.m_Offline);
        }

        async void StartServer()
        {
            try
            {
                m_ConnectionMethod.SetupServerConnection();
                await m_ConnectionMethod.ConnectServerAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                StartServerFailed();
                throw;
            }
        }
    }
}
