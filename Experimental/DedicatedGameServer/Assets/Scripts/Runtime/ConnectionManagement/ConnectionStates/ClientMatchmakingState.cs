using System;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to when a client is attempting to connect to a match via multiplayer services.
    /// If successful, transitions to the ClientConnected state. If not, transitions to the Offline state.
    /// </summary>
    class ClientMatchmakingState : OnlineState
    {
        ConnectionMethodBase m_ConnectionMethod;

        public void Configure(ConnectionMethodBase baseConnectionMethod)
        {
            m_ConnectionMethod = baseConnectionMethod;
        }

        public override void Enter()
        {
            ConnectClient();
        }

        public override void Exit() { }

        public override void OnClientConnected(ulong clientId)
        {
            ConnectionManager.ChangeState(ConnectionManager.m_ClientConnected);
        }

        public override void StopClient()
        {
            m_ConnectionMethod.StopClient();
            ConnectionManager.ChangeState(ConnectionManager.m_Offline);
        }

        async void ConnectClient()
        {
            try
            {
                m_ConnectionMethod.SetupClientConnection();
                await m_ConnectionMethod.ConnectClientAsync();
            }
            catch (SessionException e)
            {
                if (e.Error is SessionError.MatchmakerCancelled)
                {
                    Debug.Log("Client cancelled matchmaking request");
                }
                else
                {
                    Debug.LogError("Error connecting client, see following exception");
                    Debug.LogException(e);
                    ConnectionManager.ChangeState(ConnectionManager.m_Offline);
                    throw;
                }
            }
        }
    }
}
