using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
#if UNITY_SERVER
using Unity.Services.Authentication.Server;
using Unity.Services.Core;
#endif
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
    class ConnectionMethodIP : ConnectionMethodBase
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

        public override void SetupServerConnection()
        {
            SetConnectionPayload();
            var utp = (UnityTransport)m_ConnectionManager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(m_Ipaddress, m_Port);
        }

        public override Task ConnectServerAsync()
        {
            if (!m_ConnectionManager.NetworkManager.StartServer())
            {
                throw new Exception("NetworkManager StartServer failed");
            }
            return Task.CompletedTask;
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
    class ConnectionMethodMatchmaker : ConnectionMethodBase
    {
        const string k_DefaultServerName = "DedicatedGameServer";
        const string k_DefaultGameType = "DefaultGameType";
        const string k_DefaultBuildId = "47984";
        const string k_DefaultMap = "DefaultMap";

#if  UNITY_SERVER
        IMultiplaySessionManager m_SessionManager;
#endif

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

        public override void SetupServerConnection()
        {
            // nothing to set up here
        }

#if UNITY_SERVER
        public override async Task ConnectServerAsync()
        {
            if (UnityServices.Instance.GetMultiplayerService() != null)
            {
                // Authenticate
                await ServerAuthenticationService.Instance.SignInFromServerAsync();
                var token = ServerAuthenticationService.Instance.AccessToken;

                // Callbacks should be used to ensure proper state of the server allocation.
                // Awaiting the StartMultiplaySessionManagerAsync won't guarantee proper state.
                var callbacks = new MultiplaySessionManagerEventCallbacks();
                callbacks.Allocated += OnServerAllocatedCallback;

                var sessionManagerOptions = new MultiplaySessionManagerOptions()
                {
                    SessionOptions = new SessionOptions()
                    {
                        MaxPlayers = m_MaxPlayers
                    }.WithDirectNetwork().WithBackfillingConfiguration(true, true, true, 30, 1),

                    // Server options are REQUIRED for the underlying SQP server
                    MultiplayServerOptions = new MultiplayServerOptions(
                        serverName: k_DefaultServerName,
                        gameType: k_DefaultGameType,
                        buildId: k_DefaultBuildId,
                        map: k_DefaultMap
                    ),
                    Callbacks = callbacks
                };
                m_SessionManager = await MultiplayerServerService.Instance.StartMultiplaySessionManagerAsync(sessionManagerOptions);

                // Ensure that the session is only accessed after the allocation happened.
                // Otherwise you risk the Session being in an uninitialized state.
                async void OnServerAllocatedCallback(IMultiplayAllocation obj)
                {
                    var session = m_SessionManager.Session;
                    await m_SessionManager.SetPlayerReadinessAsync(true);
                    Debug.Log("[Multiplay] Server is ready to accept players");
                }
            }
        }
#else
        public override Task ConnectServerAsync()
        {
            throw new NotImplementedException("Client should not be invoking ConnectServerAsync");
        }
#endif

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
