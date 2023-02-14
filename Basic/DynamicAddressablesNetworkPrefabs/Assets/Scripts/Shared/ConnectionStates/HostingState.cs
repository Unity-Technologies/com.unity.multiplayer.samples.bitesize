using UnityEngine;

namespace Game
{
    /// <summary>
    /// Connection state corresponding to a listening host. Handles incoming client connections. When shutting down or
    /// being timed out, transitions to the Offline state.
    /// </summary>
    class HostingState : ConnectionState
    {
        public HostingState(OptionalConnectionManager connectionManager)
        {
            m_ConnectionManager = connectionManager;
        }
        
        public override void Enter() { }

        public override void Exit() { }

        public override void OnUserRequestedShutdown()
        {
            m_ConnectionManager.m_NetworkManager.Shutdown();
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
        }

        public override void OnClientConnected(ulong clientId)
        {
            Debug.Log($"Client {clientId} connected");
        }
        
        public override void OnClientDisconnect(ulong clientId)
        {
            if (clientId == m_ConnectionManager.m_NetworkManager.LocalClientId)
            {
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
            }
            else
            {
                Debug.Log($"Client {clientId} disconnected");
            }
        }
    }
}
