#if UNITY_SERVER
using System;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    public partial class ConnectionManager
    {
        internal readonly StartingServerState m_StartingServer = new();
        internal readonly ServerListeningState m_ServerListening = new();

        partial void InitializeServerStates()
        {
            m_StartingServer.ConnectionManager = this;
            m_ServerListening.ConnectionManager = this;
        }
    }
}
#endif
