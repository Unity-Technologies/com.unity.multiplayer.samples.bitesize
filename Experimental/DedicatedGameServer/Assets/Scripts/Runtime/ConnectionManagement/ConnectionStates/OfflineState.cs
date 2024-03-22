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

        public override void StartClient(string ipaddress, ushort port)
        {
            ConnectionManager.m_ClientConnecting.Configure(ipaddress, port);
            ConnectionManager.ChangeState(ConnectionManager.m_ClientConnecting);
        }

        public override void StartServerIP(string ipaddress, ushort port)
        {
            ConnectionManager.m_StartingServer.Configure(ipaddress, port);
            ConnectionManager.ChangeState(ConnectionManager.m_StartingServer);
        }
    }
}
