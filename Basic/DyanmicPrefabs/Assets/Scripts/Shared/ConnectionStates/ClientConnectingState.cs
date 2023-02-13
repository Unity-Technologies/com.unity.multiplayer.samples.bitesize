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
        public ClientConnectingState(OptionalConnectionManager connectionManager)
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
            // For this sample, connection management is used in all use-cases where dynamic prefabs are loaded after
            // the server establishes a connection. We choose to keep ForceSamePrefabs as false, so that a joining
            // client is able to send a connection request payload to the server. In the use-cases where connection
            // approval is implemented, the server can begin to validate a user's connection payload, and either approve
            // or deny connection to the joining client. In the use-cases where connection approval is not implemented,
            // a client will either join if the server has not loaded any dynamic prefabs, or will be denied if the 
            // server has loaded any dynamic prefabs since the NetworkConfig of the client and the server mismatch.
            m_ConnectionManager.m_NetworkManager.NetworkConfig.ForceSamePrefabs = false;
            var transport = m_ConnectionManager.m_NetworkManager.GetComponent<UnityTransport>();
            transport.SetConnectionData(m_ConnectionManager.m_ConnectAddress, m_ConnectionManager.m_Port);
            m_ConnectionManager.m_NetworkManager.NetworkConfig.ConnectionData = 
                DynamicPrefabLoadingUtilities.GenerateRequestPayload();
            m_ConnectionManager.m_NetworkManager.StartClient();
        }
        
        public override void OnClientConnected(ulong _)
        {
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientConnected);
        }

        public override void OnClientDisconnect(ulong _)
        {
            // client ID is for sure ours here
            var disconnectReason = m_ConnectionManager.m_NetworkManager.DisconnectReason;
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
                        Debug.Log("Disconnect reason is undefined");
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
