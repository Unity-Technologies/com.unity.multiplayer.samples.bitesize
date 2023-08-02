using System;
using Unity.Services.Matchmaker.Models;
using Unity.Template.Multiplayer.NGO.Runtime.ConnectionManagement;
using UnityEngine;
using static Unity.Services.Matchmaker.Models.MultiplayAssignment;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    internal class MatchmakerController : Controller<MetagameApplication>
    {

        [SerializeField]
        ConnectionManager m_ConnectionManager;
        
        MatchmakerView View => App.View.Matchmaker;

        void Awake()
        {
            AddListener<EnterMatchmakerQueueEvent>(OnEnterMatchmakerQueue);
            AddListener<ExitMatchmakerQueueEvent>(OnExitMatchmakerQueue);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        void OnApplicationQuit()
        {
            StopMatchmaker();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<EnterMatchmakerQueueEvent>(OnEnterMatchmakerQueue);
            RemoveListener<ExitMatchmakerQueueEvent>(OnExitMatchmakerQueue);
        }

        void OnEnterMatchmakerQueue(EnterMatchmakerQueueEvent evt)
        {
            View.Show();
            UnityServicesInitializer.Instance.Matchmaker.FindMatch(evt.QueueName, OnMatchSearchCompleted, View.UpdateTimer);
        }

        void OnExitMatchmakerQueue(ExitMatchmakerQueueEvent evt)
        {
            StopMatchmaker();
            View.Hide();
        }

        void OnMatchSearchCompleted(MultiplayAssignment assignment)
        {
            var error = string.Empty;
            switch (assignment.Status)
            {
                case StatusOptions.Found:
                    Debug.Log("Match found!");
                    //todo investigate why assignment.Port is of type Nullable<int>
                    m_ConnectionManager.StartClient(assignment.Ip, assignment.Port.Value);
                    break;
                case StatusOptions.Failed:
                    error = $"Failed to get ticket status. See logged exception for more details: {assignment.Message}";
                    break;
                case StatusOptions.Timeout:
                    //note: this is a good spot where to plug-in a fake pvp matchmaking logic that redirects the player to a PvE game
                    error = "Could not find enough players in a reasonable amount of times";
                    break;
                default:
                    throw new InvalidOperationException("Assignment status was a value other than 'In Progress', 'Found', 'Timeout' or 'Failed'! " +
                        $"Mismatch between Matchmaker SDK expected responses and service API values! Status value: '{assignment.Status}'");
            }

            if (!string.IsNullOrEmpty(error))
            {
                Broadcast(new ExitMatchmakerQueueEvent());
                Debug.LogError(error);
            }
        }

        void StopMatchmaker()
        {
            if (UnityServicesInitializer.Instance.Matchmaker)
            {
#pragma warning disable CS4014 // Can't await, so the method execution will continue
                UnityServicesInitializer.Instance.Matchmaker.StopSearch();
#pragma warning restore CS4014 // Can't await, so the method execution will continue
            }
        }
    }
}
