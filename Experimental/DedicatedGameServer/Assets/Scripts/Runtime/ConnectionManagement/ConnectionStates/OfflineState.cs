using Unity.Multiplayer;

namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to when the NetworkManager is shut down. From this state we can transition to the
    /// ClientConnecting sate, if starting as a client, or the StartingServer state, if starting as a server.
    /// </summary>
    class OfflineState : ConnectionState
    {
        public override void Enter()
        {
            ConnectionManager.NetworkManager.Shutdown();
        }

        public override void Exit() { }

        public override void StartClientIP(string ipaddress, ushort port)
        {
            var connectionMethod = new ConnectionMethodIP(ConnectionManager, ipaddress, port);
            ConnectionManager.m_ClientConnecting.Configure(connectionMethod);
            ConnectionManager.ChangeState(ConnectionManager.m_ClientConnecting);
        }

        public override void StartClientMatchmaker(string queueName, int maxPlayers)
        {
            var connectionMethod = new ConnectionMethodMatchmaker(ConnectionManager, queueName, maxPlayers);
            ConnectionManager.m_ClientConnecting.Configure(connectionMethod);
            ConnectionManager.ChangeState(ConnectionManager.m_ClientConnecting);
        }

        public override void StartServerIP(string ipaddress, ushort port)
        {
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Server)
            {
                var connectionMethod = new ConnectionMethodIP(ConnectionManager, ipaddress, port);
                ConnectionManager.m_StartingServer.Configure(connectionMethod);
                ConnectionManager.ChangeState(ConnectionManager.m_StartingServer);
            }
        }

        public override void StartServerMatchmaker(int maxPlayers)
        {
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Server)
            {
                var connectionMethod = new ConnectionMethodMatchmaker(ConnectionManager, string.Empty, maxPlayers);
                ConnectionManager.m_StartingServer.Configure(connectionMethod);
                ConnectionManager.ChangeState(ConnectionManager.m_StartingServer);
            }
        }
    }
}
