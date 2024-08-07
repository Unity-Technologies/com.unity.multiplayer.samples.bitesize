using System;
using System.Collections.Generic;
using Unity.Multiplayer;
using Unity.Netcode;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    [Serializable]
    public class ConnectionPayload
    {
        public string applicationVersion;
    }

    /// <summary>
    /// This state machine handles connection through the NetworkManager. It is responsible for listening to
    /// NetworkManger callbacks and other outside calls and redirecting them to the current ConnectionState object.
    /// </summary>
    [MultiplayerRoleRestricted]
    public class ConnectionManager : MonoBehaviour
    {
        ConnectionState m_CurrentState;

        [SerializeField]
        NetworkManager m_NetworkManager;
        public NetworkManager NetworkManager => m_NetworkManager;

        public EventManager EventManager
        {
            get
            {
                if (m_EventManager == null)
                {
                    m_EventManager = new EventManager();
                }

                return m_EventManager;
            }
        }

        EventManager m_EventManager;

        internal readonly OfflineState m_Offline = new();
        internal readonly ClientConnectingState m_ClientConnecting = new();
        internal readonly ClientConnectedState m_ClientConnected = new();
        internal readonly StartingServerState m_StartingServer = new();
        internal readonly ServerListeningState m_ServerListening = new();

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            List<ConnectionState> states = new() {m_Offline, m_ClientConnecting, m_ClientConnected, m_StartingServer, m_ServerListening};
            foreach (var state in states)
            {
                state.ConnectionManager = this;
            }

            m_CurrentState = m_Offline;
            NetworkManager.OnConnectionEvent += OnConnectionEvent;
            NetworkManager.OnServerStarted += OnServerStarted;
            NetworkManager.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.OnTransportFailure += OnTransportFailure;
            NetworkManager.OnServerStopped += OnServerStopped;
        }

        void OnDestroy()
        {
            NetworkManager.OnConnectionEvent -= OnConnectionEvent;
            NetworkManager.OnServerStarted -= OnServerStarted;
            NetworkManager.ConnectionApprovalCallback -= ApprovalCheck;
            NetworkManager.OnTransportFailure -= OnTransportFailure;
            NetworkManager.OnServerStopped -= OnServerStopped;
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

        void OnConnectionEvent(NetworkManager arg1, ConnectionEventData arg2)
        {
            switch (arg2.EventType)
            {
                case Netcode.ConnectionEvent.ClientConnected:
                    m_CurrentState.OnClientConnected(arg2.ClientId);
                    break;
                case Netcode.ConnectionEvent.ClientDisconnected:
                    m_CurrentState.OnClientDisconnect(arg2.ClientId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(arg2.EventType), arg2.EventType, "Unhandled ConnectionEvent encountered.");
            }
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

        void OnServerStopped(bool isHost) // we don't need this parameter as the ConnectionState already carries the relevant information
        {
            m_CurrentState.OnServerStopped();
        }

        public void StartClient(string ipaddress, ushort port)
        {
            m_CurrentState.StartClient(ipaddress, port);
        }

        public void StartServerMatchmaker()
        {
            m_CurrentState.StartServerMatchmaker();
        }

        public void StartServerIP(string ipaddress, ushort port)
        {
            m_CurrentState.StartServerIP(ipaddress, port);
        }

        public void RequestShutdown()
        {
            m_CurrentState.OnUserRequestedShutdown();
        }
    }
}
