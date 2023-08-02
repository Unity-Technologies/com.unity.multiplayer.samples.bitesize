namespace Unity.Template.Multiplayer.NGO.Runtime.ConnectionManagement
{
    class ClientConnectedState : OnlineState
    {
        public override void Enter() { }

        public override void Exit() { }

        public override void OnClientDisconnect(ulong clientId)
        {
            ConnectionManager.ChangeState(ConnectionManager.m_Offline);
        }
    }
}
