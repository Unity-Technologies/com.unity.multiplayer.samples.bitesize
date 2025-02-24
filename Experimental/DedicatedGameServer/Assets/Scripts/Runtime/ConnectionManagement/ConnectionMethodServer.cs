using System;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
#if UNITY_SERVER
using Unity.Services.Authentication.Server;
#endif
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    /// <summary>
    /// Simple IP connection setup with UTP
    /// </summary>
    partial class ConnectionMethodIP
    {
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
    }

    /// <summary>
    /// UTP's Relay connection setup using the Session integration
    /// </summary>
    partial class ConnectionMethodMatchmaker
    {
        const string k_DefaultServerName = "DedicatedGameServer";
        const string k_DefaultGameType = "DefaultGameType";
        const string k_DefaultBuildId = "47984";
        const string k_DefaultMap = "DefaultMap";

        const int k_PlayerConnectionTimeout = 30;
        const int k_BackfillingLoopInterval = 1;

        #if UNITY_SERVER
        IMultiplaySessionManager m_SessionManager;
        #endif

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
                    }.WithDirectNetwork().WithBackfillingConfiguration(true, true, true, k_PlayerConnectionTimeout, k_BackfillingLoopInterval),

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
    }
}
