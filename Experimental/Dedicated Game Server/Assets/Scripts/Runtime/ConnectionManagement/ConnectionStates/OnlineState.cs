namespace Unity.Template.Multiplayer.NGO.Runtime.ConnectionManagement
{
    abstract class OnlineState : ConnectionState
    {
        public override void OnUserRequestedShutdown()
        {
            // This behaviour will be the same for every online state
            ConnectionManager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.UserRequestedDisconnect });
            ConnectionManager.ChangeState(ConnectionManager.m_Offline);
        }

        public override void OnTransportFailure()
        {
            // This behaviour will be the same for every online state
            ConnectionManager.ChangeState(ConnectionManager.m_Offline);
        }
    }
}
