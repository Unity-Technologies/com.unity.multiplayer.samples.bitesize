using UnityEngine;

namespace Game
{
    /// <summary>
    /// Connection state corresponding to a connected client. When being disconnected, it transitions to the Offline
    /// state.
    /// </summary>
    class ClientConnectedState : ConnectionState
    {
        public ClientConnectedState(OptionalConnectionManager connectionManager)
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

        public override void OnClientDisconnect(ulong clientId)
        {
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
        }
    }
}
