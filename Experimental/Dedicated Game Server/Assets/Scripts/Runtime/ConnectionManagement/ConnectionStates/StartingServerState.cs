using System;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime.ConnectionManagement
{
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
