using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    /// <summary>
    /// ConnectionMethod contains all setup needed to setup NGO to be ready to start a connection, either host or client
    /// side.
    /// Please override this abstract class to add a new transport or way of connecting.
    /// </summary>
    public abstract class ConnectionMethodBase
    {
        protected ConnectionManager m_ConnectionManager;

        /// <summary>
        /// Setup the host connection prior to starting the NetworkManager
        /// </summary>
        /// <returns></returns>
        public abstract void SetupServerConnection();

        /// <summary>
        /// Setup the client connection prior to starting the NetworkManager
        /// </summary>
        /// <returns></returns>
        public abstract void SetupClientConnection();

        public abstract Task ConnectServerAsync();

        public abstract Task ConnectClientAsync();

        protected ConnectionMethodBase(ConnectionManager connectionManager)
        {
            m_ConnectionManager = connectionManager;
        }

        protected void SetConnectionPayload()
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                applicationVersion = Application.version
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            m_ConnectionManager.NetworkManager.NetworkConfig.ConnectionData = payloadBytes;
        }
    }

    /// <summary>
    /// Simple IP connection setup with UTP
    /// </summary>
    partial class ConnectionMethodIP : ConnectionMethodBase
    {
        string m_Ipaddress;
        ushort m_Port;

        public ConnectionMethodIP(ConnectionManager connectionManager, string ip, ushort port)
            : base(connectionManager)
        {
            m_Ipaddress = ip;
            m_Port = port;
            m_ConnectionManager = connectionManager;
        }

        public override void SetupClientConnection()
        {
            SetConnectionPayload();
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(m_Ipaddress, m_Port);
        }

        public override Task ConnectClientAsync()
        {
            if (!m_ConnectionManager.NetworkManager.StartClient())
            {
                throw new Exception("NetworkManager StartClient failed");
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// UTP's Relay connection setup using the Session integration
    /// </summary>
    partial class ConnectionMethodMatchmaker : ConnectionMethodBase
    {
        int m_MaxPlayers;
        string m_QueueName;

        public ConnectionMethodMatchmaker(
            ConnectionManager connectionManager,
            string queueName,
            int maxPlayers)
            : base(connectionManager)
        {
            m_QueueName = queueName;
            m_MaxPlayers = maxPlayers;
            m_ConnectionManager = connectionManager;
        }

        public override void SetupClientConnection()
        {
            SetConnectionPayload();
        }

        public override async Task ConnectClientAsync()
        {
            var matchmakerOptions = new MatchmakerOptions
            {
                QueueName = m_QueueName
            };

            var sessionOptions = new SessionOptions()
            {
                MaxPlayers = m_MaxPlayers
            }.WithDirectNetwork();

            var matchmakerCancellationSource = new CancellationTokenSource();

            await MultiplayerService.Instance.MatchmakeSessionAsync(matchmakerOptions, sessionOptions, matchmakerCancellationSource.Token);
        }
    }
}
