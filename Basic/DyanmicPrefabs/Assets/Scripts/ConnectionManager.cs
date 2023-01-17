using System;
using Unity.Netcode;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// A state machine to track a client's connection state.
    /// </summary>
    public class ConnectionManager : MonoBehaviour
    {
        public DynamicPrefabManager dynamicPrefabManager;
        
        public NetworkManager networkManager;
        
        ConnectionState m_CurrentState;
        
        internal OfflineState m_Offline;
        
        internal ClientConnectingState m_ClientConnecting;
        
        internal ClientConnectedState m_ClientConnected;
        
        internal ClientPreloadingState m_ClientPreloading;
        
        internal StartingHostState m_StartingHost;
        
        internal HostingState m_Hosting;
        
        internal string m_ConnectAddress;
        
        internal ushort m_Port;

        void Awake()
        {
            DontDestroyOnLoad(this);
            
            m_Offline = new OfflineState(this);
            m_ClientConnecting = new ClientConnectingState(this);
            m_ClientConnected = new ClientConnectedState(this);
            m_ClientPreloading = new ClientPreloadingState(this);
            m_StartingHost = new StartingHostState(this);
            m_Hosting = new HostingState(this);
        }

        void Start()
        {
            m_CurrentState = m_Offline;

            networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
            networkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            networkManager.OnServerStarted += OnServerStarted;
            networkManager.ConnectionApprovalCallback += ApprovalCheck;
            networkManager.OnTransportFailure += OnTransportFailure;
        }
        
        void OnDestroy()
        {
            networkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            networkManager.OnServerStarted -= OnServerStarted;
            networkManager.ConnectionApprovalCallback -= ApprovalCheck;
            networkManager.OnTransportFailure -= OnTransportFailure;
        }
        
        void OnClientConnectedCallback(ulong clientId)
        {
            m_CurrentState.OnClientConnected(clientId);
        }
        
        void OnClientDisconnectCallback(ulong clientId)
        {
            m_CurrentState.OnClientDisconnect(clientId);
        }
        
        void OnServerStarted()
        {
            m_CurrentState.OnServerStarted();
        }
        
        void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            m_CurrentState.ApprovalCheck(request, response);
        }
        
        void OnTransportFailure()
        {
            m_CurrentState.OnTransportFailure();
        }

        internal void ChangeState(ConnectionState nextState)
        {
            Debug.Log($"{name}: Changed connection state from {m_CurrentState.GetType().Name} to {nextState.GetType().Name}.");

            if (m_CurrentState != null)
            {
                m_CurrentState.Exit();
            }
            m_CurrentState = nextState;
            m_CurrentState.Enter();
        }

        public void StartClientIp(string ipaddress, ushort port)
        {
            m_ConnectAddress = ipaddress;
            m_Port = port;
            m_CurrentState.StartClientIP(ipaddress, port);
        }

        public void StartHostIp(string ipaddress, ushort port)
        {
            m_ConnectAddress = ipaddress;
            m_Port = port;
            m_CurrentState.StartHostIP(ipaddress, port);
        }

        public void RequestShutdown()
        {
            m_CurrentState.OnUserRequestedShutdown();
        }
    }
}