using Unity.Netcode;

namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    /// <summary>
    /// Base class representing a connection state.
    /// </summary>
    abstract class ConnectionState
    {
        public ConnectionManager ConnectionManager { get; set; }

        public abstract void Enter();

        public abstract void Exit();

        public virtual void OnClientConnected(ulong clientId) { }
        
        public virtual void OnClientDisconnect(ulong clientId) { }

        public virtual void OnServerStarted() { }

        public virtual void StartClient(string ipaddress, ushort port) { }

        public virtual void StartServerIP(string ipaddress, ushort port) { }

        public virtual void StartServerMatchmaker() { }

        public virtual void OnUserRequestedShutdown() { }

        public virtual void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) { }

        public virtual void OnTransportFailure() { }

        public virtual void OnServerStopped() { }
    }
}
