using System;
using Unity.DedicatedGameServerSample.Runtime.ApplicationLifecycle;
using Unity.DedicatedGameServerSample.Runtime.ConnectionManagement;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    class MatchmakerController : Controller<MetagameApplication>
    {
        MatchmakerView View => App.View.Matchmaker;

        const string k_QueueName = "Queue01";

        void Awake()
        {
            AddListener<EnterMatchmakerQueueEvent>(OnEnterMatchmakerQueue);
            AddListener<ExitMatchmakerQueueEvent>(OnExitMatchmakerQueue);
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
            RemoveListener<EnterMatchmakerQueueEvent>(OnEnterMatchmakerQueue);
            RemoveListener<ExitMatchmakerQueueEvent>(OnExitMatchmakerQueue);
            if (ConnectionManager.Instance != null)
            {
                ConnectionManager.Instance.EventManager.RemoveListener<ConnectionEvent>(OnConnectionEvent);
            }
        }

        void OnEnterMatchmakerQueue(EnterMatchmakerQueueEvent evt)
        {
            View.Show();
            ConnectionManager.Instance.StartClientMatchmaker(k_QueueName, ApplicationEntryPoint.k_MaxPlayers);
        }

        void OnExitMatchmakerQueue(ExitMatchmakerQueueEvent evt)
        {
            View.Hide();
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
