using System;
using Unity.DedicatedGameServerSample.Runtime.ConnectionManagement;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    class DirectIPController : Controller<MetagameApplication>
    {
        DirectIPView View => App.View.DirectIP;

        void Awake()
        {
            AddListener<EnterIPConnectionEvent>(OnEnterIPConnection);
            AddListener<ExitIPConnectionEvent>(OnExitIPConnection);
            AddListener<JoinThroughDirectIPEvent>(OnJoinGame);
            if (ConnectionManager.Instance != null)
            {
                ConnectionManager.Instance.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
            }
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<EnterIPConnectionEvent>(OnEnterIPConnection);
            RemoveListener<ExitIPConnectionEvent>(OnExitIPConnection);
            RemoveListener<JoinThroughDirectIPEvent>(OnJoinGame);
            if (ConnectionManager.Instance != null)
            {
                ConnectionManager.Instance.EventManager.RemoveListener<ConnectionEvent>(OnConnectionEvent);
            }
        }

        void OnEnterIPConnection(EnterIPConnectionEvent evt)
        {
            View.Show();
        }

        void OnExitIPConnection(ExitIPConnectionEvent evt)
        {
            View.Hide();
        }

        void OnJoinGame(JoinThroughDirectIPEvent evt)
        {
            if (ConnectionManager.Instance != null)
            {
                ConnectionManager.Instance.StartClientIP(evt.ipAddress, evt.port);
            }
        }

        void OnConnectionEvent(ConnectionEvent evt)
        {
            if (evt.status == ConnectStatus.Connecting)
            {
                View.Hide();
            }
        }
    }
}
