using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Connection state corresponding to a host starting up. Starts the host when entering the state. If successful,
    /// transitions to the Hosting state, if not, transitions back to the Offline state.
    /// </summary>
    class StartingHostState : ConnectionState
    {
        public StartingHostState(OptionalConnectionManager connectionManager)
        {
            m_ConnectionManager = connectionManager;
        }
    
        public override void Enter()
        {
            StartHost();
        }

        public override void Exit() { }
        
        void StartHost()
        {
            var transport = m_ConnectionManager.m_NetworkManager.GetComponent<UnityTransport>();
            transport.SetConnectionData(m_ConnectionManager.m_ConnectAddress, m_ConnectionManager.m_Port);
            m_ConnectionManager.m_NetworkManager.StartHost();
        }
        
        public override void OnClientDisconnect(ulong clientId)
        {
            if (clientId == m_ConnectionManager.m_NetworkManager.LocalClientId)
            {
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_Offline);
            }
        }

        public override void OnServerStarted()
        {
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_Hosting);
        }
    }
}
