#if UNITY_SERVER
using System;
using System.Threading.Tasks;
using Unity.DedicatedGameServerSample.Runtime.ApplicationLifecycle;
using Unity.Services.Authentication.Server;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    partial class MatchmakerHandler : MonoBehaviour
    {
        const string k_DefaultServerName = "DedicatedGameServer";
        const string k_DefaultGameType = "DefaultGameType";
        const string k_DefaultBuildId = "47984";
        const string k_DefaultMap = "DefaultMap";

        const int k_PlayerConnectionTimeout = 30;
        const int k_BackfillingLoopInterval = 1;

        IMultiplaySessionManager m_SessionManager;

        IServerQueryHandler m_ServerQueryHandler;

        internal async Task ConnectToDedicatedGameServer()
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
                        MaxPlayers = ApplicationEntryPoint.k_MaxPlayers
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
                // Otherwise, you risk the Session being in an uninitialized state.
                async void OnServerAllocatedCallback(IMultiplayAllocation obj)
                {
                    var session = m_SessionManager.Session;
                    await m_SessionManager.SetPlayerReadinessAsync(true);
                    Debug.Log("[Multiplay] Server is ready to accept players");
                }
            }
        }

        internal async void StartServerQuery()
        {
            m_ServerQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(ApplicationEntryPoint.k_MaxPlayers, k_DefaultServerName, k_DefaultGameType, k_DefaultBuildId, k_DefaultMap);
        }

        internal void UpdatePlayerCount(ushort newPlayerCount)
        {
            m_ServerQueryHandler.CurrentPlayers = newPlayerCount;
        }

        internal void Cleanup()
        {
            m_ServerQueryHandler.Dispose();
        }

        void Update()
        {
            if (m_ServerQueryHandler != null)
            {
                m_ServerQueryHandler.UpdateServerCheck();
            }
        }
    }
}
#endif
