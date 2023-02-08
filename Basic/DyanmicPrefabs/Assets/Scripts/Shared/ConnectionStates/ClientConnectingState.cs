using System;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Connection state corresponding to when a client is attempting to connect to a server. Starts the client when
    /// entering. If successful, transitions to the ClientConnected state. If not, transitions to the Offline state.
    /// </summary>
    class ClientConnectingState : ConnectionState
    {
        public ClientConnectingState(ConnectionManager connectionManager)
        {
            m_ConnectionManager = connectionManager;
        }
        
        public override void Enter()
        {
            StartClient();
        }

        public override void Exit() { }
        
        void StartClient()
        {
            Debug.Log(nameof(StartClient));
            
            m_ConnectionManager.networkManager.NetworkConfig.ForceSamePrefabs = false;
            var transport = m_ConnectionManager.networkManager.GetComponent<UnityTransport>();
            transport.SetConnectionData(m_ConnectionManager.m_ConnectAddress, m_ConnectionManager.m_Port);
            m_ConnectionManager.networkManager.NetworkConfig.ConnectionData = 
                DynamicPrefabLoadingUtilities.GenerateRequestPayload();
            m_ConnectionManager.networkManager.StartClient();
        }
        
        public override void OnClientConnected(ulong _)
        {
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientConnected);
        }

        public override void OnClientDisconnect(ulong _)
        {
            // client ID is for sure ours here
            var disconnectReason = m_ConnectionManager.networkManager.DisconnectReason;
            if (string.IsNullOrEmpty(disconnectReason))
            {
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
            }
            else
            {
                var disconnectionPayload = JsonUtility.FromJson<DisconnectionPayload>(disconnectReason);

                switch (disconnectionPayload.reason)
                {
                    case DisconnectReason.Undefined:
                        Debug.Log("Undefined");
                        m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
                        break;
                    case DisconnectReason.ClientNeedsToPreload:
                    {
                        Debug.Log("Client needs to preload");
                        m_ConnectionManager.m_ClientPreloading.disconnectionPayload = disconnectionPayload;
                        m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientPreloading);
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
