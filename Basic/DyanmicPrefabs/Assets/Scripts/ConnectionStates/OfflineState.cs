using UnityEngine;

namespace Game
{
    /// <summary>
    /// Connection state corresponding to when the NetworkManager is shut down. Also, all loaded prefabs are cleared
    /// from memory, and NetworkManager's NetworkPrefabs array is cleared. From this state we can transition to the
    /// ClientConnecting sate, if starting as a client, or the StartingHost state, if starting as a host.
    /// </summary>
    class OfflineState : ConnectionState
    {
        public OfflineState(ConnectionManager connectionManager)
        {
            m_ConnectionManager = connectionManager;
        }
        
        public override void Enter()
        {
            m_ConnectionManager.dynamicPrefabManager.UnloadAndReleaseAllDynamicPrefabs();
            m_ConnectionManager.networkManager.Shutdown();
        }

        public override void Exit() { }

        public override void StartClientIP(string ipaddress, ushort port)
        {
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientConnecting);
        }

        public override void StartHostIP(string ipaddress, ushort port)
        {
            m_ConnectionManager.ChangeState(m_ConnectionManager.m_StartingHost);
        }
    }
}