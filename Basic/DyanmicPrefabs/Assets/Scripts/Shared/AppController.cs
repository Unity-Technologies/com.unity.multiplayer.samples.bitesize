using System;
using Unity.Netcode;
using UnityEngine;

namespace Game
{
    public sealed class AppController : NetworkBehaviour
    {
        // placeholder until this is fetched from UI
        string m_ConnectAddress = "127.0.0.1";
        
        // placeholder until this is fetched from UI
        ushort m_Port = 7777;
        
        [SerializeField] GameObject m_ConnectionUI;
        
        [SerializeField] GameObject m_SpawnUI;

        [SerializeField]
        ConnectionManager m_ConnectionManager;

        public void StartClient()
        {
            Debug.Log(nameof(StartClient));
            m_ConnectionManager.StartClientIp(m_ConnectAddress, m_Port);
            m_ConnectionUI.SetActive(false);
        }

        public void StartHost()
        {
            Debug.Log(nameof(StartHost));
            m_ConnectionManager.StartHostIp(m_ConnectAddress, m_Port);
            m_ConnectionUI.SetActive(false);
        }

        public override void OnNetworkSpawn()
        {
            m_SpawnUI.SetActive(IsServer);
        }
        
        public override void OnNetworkDespawn()
        {
            m_ConnectionUI.SetActive(true);
            m_SpawnUI.SetActive(true);
        }

        // placeholder until this is triggered by UI
        [ContextMenu(nameof(OnClickedShutdown))]
        public void OnClickedShutdown()
        {
            Debug.Log(nameof(OnClickedShutdown));
            m_ConnectionManager.RequestShutdown();
        }
    }
}
