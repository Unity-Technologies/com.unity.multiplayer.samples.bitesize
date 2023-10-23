using System;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to when a client is attempting to connect to a server. Starts the client when
    /// entering. If successful, transitions to the ClientConnected state. If not, transitions to the Offline state.
    /// </summary>
    class ClientConnectingState : OnlineState
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
            if (string.IsNullOrEmpty(disconnectReason))
            {
                ConnectionManager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.StartClientFailed });
            }
            else
            {
                var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                ConnectionManager.EventManager.Broadcast(new ConnectionEvent { status = connectStatus });
            }
            ConnectionManager.ChangeState(ConnectionManager.m_Offline);
        }
        
        void ConnectClient()
        {
            try
            {
                // Setup NGO with current connection method
                SetConnectionPayload();
                var utp = (UnityTransport)ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
                utp.SetConnectionData(m_IPAddress, m_Port);
                
                Debug.Log($"Attempting to connect to server on {m_IPAddress} with port {m_Port}");
                // NGO's StartClient launches everything
                if (!ConnectionManager.NetworkManager.StartClient())
                {
                    throw new Exception("NetworkManager StartClient failed");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error connecting client, see following exception");
                Debug.LogException(e);
                StartingClientFailed();
                throw;
            }
        }

        void SetConnectionPayload()
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                applicationVersion = Application.version
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            ConnectionManager.NetworkManager.NetworkConfig.ConnectionData = payloadBytes;
        }
    }
}
