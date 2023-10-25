using System;
using Unity.DedicatedGameServerSample.Runtime.ApplicationLifecycle;
using Unity.DedicatedGameServerSample.Runtime.ConnectionManagement;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    public class ClientConnectingController : Controller<MetagameApplication>
    {
        ClientConnectingView View => App.View.ClientConnecting;
        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;

        void Awake()
        {
            ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
            AddListener<CancelConnectionEvent>(OnCancelConnection);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            ConnectionManager.EventManager.RemoveListener<ConnectionEvent>(OnConnectionEvent);
            RemoveListener<CancelConnectionEvent>(OnCancelConnection);
        }

        void OnConnectionEvent(ConnectionEvent evt)
        {
            switch (evt.status)
            {
                case ConnectStatus.Connecting:
                    App.Model.ClientConnecting.InitializeTimer();
                    View.Show();
                    break;
                case ConnectStatus.Success:
                case ConnectStatus.ServerFull:
                case ConnectStatus.IncompatibleVersions:
                case ConnectStatus.UserRequestedDisconnect:
                case ConnectStatus.GenericDisconnect:
                case ConnectStatus.ServerEndedSession:
                case ConnectStatus.StartClientFailed:
                    View.Hide();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(); // other statuses should not be received here
            }
        }

        void OnCancelConnection(CancelConnectionEvent evt)
        {
            ConnectionManager.RequestShutdown();
        }
    }
}
