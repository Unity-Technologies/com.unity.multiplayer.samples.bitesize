using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.DedicatedGameServerSample.Runtime.ApplicationLifecycle;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    partial class MatchmakerHandler : MonoBehaviour
    {
        const string k_QueueName = "Queue01";

        public static MatchmakerHandler Instance { get; private set; }

        CancellationTokenSource m_CancellationTokenSource;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        internal async Task ConnectClientAsync()
        {
            var matchmakerOptions = new MatchmakerOptions
            {
                QueueName = k_QueueName
            };

            var sessionOptions = new SessionOptions()
            {
                MaxPlayers = ApplicationEntryPoint.k_MaxPlayers
            }.WithDirectNetwork();

            m_CancellationTokenSource = new CancellationTokenSource();

            await MultiplayerService.Instance.MatchmakeSessionAsync(matchmakerOptions, sessionOptions, m_CancellationTokenSource.Token);
        }

        internal void CancelConnectToDedicatedGameServer()
        {
            m_CancellationTokenSource.Cancel();
        }
    }
}
