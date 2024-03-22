using System;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to a server starting up. Starts the server when entering the state. If successful,
    /// transitions to the ServerListening state, if not, transitions back to the Offline state.
    /// </summary>
    class StartingServerState : OnlineState
    {
        string m_IPAddress;
        ushort m_Port;

        public void Configure(string iPAddress, ushort port)
        {
            m_IPAddress = iPAddress;
            m_Port = port;
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

        void StartServer()
        {
            try
            {
                var utp = (UnityTransport)ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
                utp.SetConnectionData(m_IPAddress, m_Port);
                
                // NGO's StartServer launches everything
                Debug.Log($"Starting server, listening on {m_IPAddress} with port {m_Port}");
                if (!ConnectionManager.NetworkManager.StartServer())
                {
                    StartServerFailed();
                }
            }
            catch (Exception)
            {
                StartServerFailed();
                throw;
            }
        }
    }
}
