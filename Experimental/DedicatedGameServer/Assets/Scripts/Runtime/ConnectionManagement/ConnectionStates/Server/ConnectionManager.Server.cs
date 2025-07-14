using System;
using Unity.Multiplayer;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    public partial class ConnectionManager
    {
        internal readonly StartingServerState m_StartingServer = new();
        internal readonly ServerListeningState m_ServerListening = new();

        partial void InitializeServerStates()
        {
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Server)
            {
                m_StartingServer.ConnectionManager = this;
                m_ServerListening.ConnectionManager = this;
            }
        }
    }
}
