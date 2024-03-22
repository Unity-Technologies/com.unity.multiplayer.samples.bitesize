namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    public enum ConnectStatus
    {
        /// <summary>
        /// Status is not defined. This likely means an unexpected error occurred.
        /// </summary>
        Undefined,
        /// <summary>
        /// Client is attempting to connect.
        /// </summary>
        Connecting,
        /// <summary>
        /// Client successfully connected.
        /// </summary>
        Success,
        /// <summary>
        /// Can't join, server is already at capacity.
        /// </summary>
        ServerFull,
        /// <summary>
        /// Client build version is incompatible with server.
        /// </summary>
        IncompatibleVersions,
        /// <summary>
        /// Intentional Disconnect triggered by the user.
        /// </summary>
        UserRequestedDisconnect,
        /// <summary>
        /// Server disconnected, but no specific reason given.
        /// </summary>
        GenericDisconnect,
        /// <summary>
        /// Server intentionally ended the session.
        /// </summary>
        ServerEndedSession,
        /// <summary>
        /// Failed to connect to server and/or invalid network endpoint.
        /// </summary>
        StartClientFailed,
        /// <summary>
        /// Server failed to bind.
        /// </summary>
        StartServerFailed
    }

    public class ConnectionEvent : AppEvent
    {
        public ConnectStatus status;
    }
    
    public class ClientConnectedEvent: AppEvent { }
    
    public class ClientDisconnectedEvent: AppEvent { }
    
    public class MinNumberPlayersConnectedEvent: AppEvent { }
}
