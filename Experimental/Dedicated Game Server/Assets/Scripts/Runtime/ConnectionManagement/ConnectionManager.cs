using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime.ConnectionManagement
{
    public enum ConnectStatus
    {
        Undefined,
        Success,                  // client successfully connected. This may also be a successful reconnect.
        ServerFull,               // can't join, server is already at capacity.
        LoggedInAgain,            // logged in on a separate client, causing this one to be kicked out.
        UserRequestedDisconnect,  // intentional Disconnect triggered by the user.
        GenericDisconnect,        // server disconnected, but no specific reason given.
        Reconnecting,             // client lost connection and is attempting to reconnect.
        IncompatibleVersions,     // client build version is incompatible with server.
        ServerEndedSession,       // server intentionally ended the session.
        StartHostFailed,          // server failed to bind
        StartClientFailed         // failed to connect to server and/or invalid network endpoint
    }
    
    [Serializable]
    public class ConnectionPayload
    {
        public string playerId;
        public string applicationVersion;
    }

    /// <summary>
    /// This state machine handles connection through the NetworkManager. It is responsible for listening to
    /// NetworkManger callbacks and other outside calls and redirecting them to the current ConnectionState object.
    /// </summary>
    public class ConnectionManager : MonoBehaviour
    {
        ConnectionState m_CurrentState;

        [SerializeField]
        NetworkManager m_NetworkManager;
        public NetworkManager NetworkManager => m_NetworkManager;
        
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
            NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            NetworkManager.OnServerStarted += OnServerStarted;
            NetworkManager.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.OnTransportFailure += OnTransportFailure;
            NetworkManager.OnServerStopped += OnServerStopped;
        }

        void OnDestroy()
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
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

        void OnClientDisconnectCallback(ulong clientId)
        {
            m_CurrentState.OnClientDisconnect(clientId);
        }

        void OnClientConnectedCallback(ulong clientId)
        {
            m_CurrentState.OnClientConnected(clientId);
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

        void OnServerStopped(bool _) // we don't need this parameter as the ConnectionState already carries the relevant information
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
