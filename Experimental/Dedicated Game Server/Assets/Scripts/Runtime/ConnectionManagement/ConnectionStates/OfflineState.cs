namespace Unity.Template.Multiplayer.NGO.Runtime.ConnectionManagement
{
    class OfflineState : ConnectionState
    {
        public override void Enter()
        {
            ConnectionManager.NetworkManager.Shutdown();
        }

        public override void Exit() { }

        public override void StartClient(string ipaddress, int port)
        {
            ConnectionManager.m_ClientConnecting.Configure(ipaddress, (ushort) port);
            ConnectionManager.ChangeState(ConnectionManager.m_ClientConnecting);
        }

        public override void StartServerIP(string ipaddress, int port)
        {
            ConnectionManager.m_StartingServer.Configure(ipaddress, (ushort) port);
            ConnectionManager.ChangeState(ConnectionManager.m_StartingServer);
        }
    }
}
