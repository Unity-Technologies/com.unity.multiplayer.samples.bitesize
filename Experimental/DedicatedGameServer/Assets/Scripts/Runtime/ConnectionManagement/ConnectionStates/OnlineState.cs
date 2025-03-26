namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    /// <summary>
    /// Base class representing an online connection state.
    /// </summary>
    abstract class OnlineState : ConnectionState
    {
        protected const string k_ServerDisconnectReason = "Disconnected due to server shutting down.";
        protected const string k_HostDisconnectReason = "Disconnected due to host shutting down.";

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
