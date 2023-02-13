using System;
using Unity.Netcode;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// A class to bind UI events to invocations from <see cref="OptionalConnectionManager"/>, where client and host
    /// connection requests are initiated. This class also listens for status updates from Netcode for GameObjects to
    /// then display the appropriate UI elements.
    /// </summary>
    public sealed class AppController : MonoBehaviour
    {
        // placeholder until this is fetched from UI
        string m_ConnectAddress = "127.0.0.1";
        
        // placeholder until this is fetched from UI
        ushort m_Port = 7777;

        [SerializeField]
        NetworkManager m_NetworkManager;
        
        [SerializeField] GameObject m_ConnectionUI;
        
        [SerializeField] GameObject m_SpawnUI;

        [SerializeField]
        OptionalConnectionManager m_ConnectionManager;

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        void Start()
        {
            m_SpawnUI.SetActive(true);
            m_ConnectionUI.SetActive(true);
            
            m_NetworkManager.OnClientConnectedCallback += OnClientConnected;
            m_NetworkManager.OnClientDisconnectCallback += OnClientDisconnect;
        }

        void OnDestroy()
        {
            m_NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            m_NetworkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }

        void OnClientConnected(ulong obj)
        {
            m_SpawnUI.SetActive(m_NetworkManager.IsServer);
            m_ConnectionUI.SetActive(false);
        }
        
        void OnClientDisconnect(ulong obj)
        {
            m_ConnectionUI.SetActive(true);
            m_SpawnUI.SetActive(true);
        }
        
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

        // placeholder until this is triggered by UI
        [ContextMenu(nameof(OnClickedShutdown))]
        public void OnClickedShutdown()
        {
            Debug.Log(nameof(OnClickedShutdown));
            m_ConnectionManager.RequestShutdown();
            m_SpawnUI.SetActive(true);
            m_ConnectionUI.SetActive(true);
        }
    }
}
