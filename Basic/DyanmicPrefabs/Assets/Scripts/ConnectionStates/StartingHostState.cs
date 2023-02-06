using Unity.Netcode;
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
        public StartingHostState(ConnectionManager connectionManager)
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
            Debug.Log(nameof(StartHost));
            m_ConnectionManager.networkManager.NetworkConfig.ForceSamePrefabs = false;
            var transport = m_ConnectionManager.networkManager.GetComponent<UnityTransport>();
            transport.SetConnectionData(m_ConnectionManager.m_ConnectAddress, m_ConnectionManager.m_Port);
            m_ConnectionManager.networkManager.StartHost();
        }

        public override void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            var clientId = request.ClientNetworkId;
            // This happens when starting as a host, before the end of the StartHost call. In that case, we simply approve ourselves.
            if (clientId == m_ConnectionManager.networkManager.LocalClientId)
            {
                // connection approval will create a player object for you
                response.Approved = true;
                response.CreatePlayerObject = false;
            }
        }
        
        public override void OnClientDisconnect(ulong clientId)
        {
            if (clientId == m_ConnectionManager.networkManager.LocalClientId)
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
