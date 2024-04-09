using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using StatusOptions = Unity.Services.Matchmaker.Models.MultiplayAssignment.StatusOptions;

namespace Unity.DedicatedGameServerSample.Runtime
{
    ///<summary>
    ///Holds matchmaker search logic
    ///</summary>
    internal class MatchmakerTicketer : MonoBehaviour
    {
        internal string LastQueueName { get; private set; }
        internal bool Searching { get; private set; }
        string m_TicketId = "";
        Coroutine m_PollingCoroutine = null;

        internal async void FindMatch(string queueName, Action<MultiplayAssignment> onMatchSearchCompleted, Action<int> onMatchmakerTicked)
        {
            try
            {
                if (!Searching)
                {
                    if (m_TicketId.Length > 0)
                    {
                        Debug.LogError($"Already matchmaking!");
                        return;
                    }

                    Searching = true;
                    await StartSearch(queueName, onMatchSearchCompleted, onMatchmakerTicked);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                await StopSearch();
                MetagameApplication.Instance.Broadcast(new ExitMatchmakerQueueEvent());
            }
        }

        async Task StartSearch(string queueName, Action<MultiplayAssignment> onMatchSearchCompleted, Action<int> onMatchmakerTicked)
        {
            var attributes = new Dictionary<string, object>();
            var players = new List<Services.Matchmaker.Models.Player>
            {
                new Services.Matchmaker.Models.Player(AuthenticationService.Instance.PlayerId, new { }),
            };
            var options = new CreateTicketOptions(queueName, attributes);
            var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);
            LastQueueName = queueName;
            m_TicketId = ticketResponse.Id;

            CoroutinesHelper.StopAndNullifyRoutine(ref m_PollingCoroutine, this);
            m_PollingCoroutine = StartCoroutine(PollTicketStatus(onMatchSearchCompleted, onMatchmakerTicked));
        }

        internal async Task StopSearch()
        {
            CoroutinesHelper.StopAndNullifyRoutine(ref m_PollingCoroutine, this);
            if (!string.IsNullOrEmpty(m_TicketId))
            {
                await MatchmakerService.Instance.DeleteTicketAsync(m_TicketId);
                m_TicketId = string.Empty;
            }
            Searching = false;
        }

        IEnumerator PollTicketStatus(Action<MultiplayAssignment> onMatchSearchCompleted, Action<int> onMatchmakerTicked)
        {
            TicketStatusResponse response = null;
            MultiplayAssignment assignment = null;
            bool polling = true;
            int elapsedTime = 0;
            Task<TicketStatusResponse> ticketTask = null;

            while (polling)
            {
                if (elapsedTime % 2 == 0)
                {
                    ticketTask = Task.Run(() => MatchmakerService.Instance.GetTicketAsync(m_TicketId));
                }
                yield return CoroutinesHelper.OneSecond;
                elapsedTime++;
                onMatchmakerTicked?.Invoke(elapsedTime);

                try
                {
                    if (ticketTask.IsCompleted)
                    {
                        response = ticketTask.Result;

                        if (response.Type == typeof(MultiplayAssignment))
                        {
                            assignment = response.Value as MultiplayAssignment;
                        }

                        if (assignment == null)
                        {
                            throw new InvalidOperationException($"GetTicketStatus returned a type that was not a {nameof(MultiplayAssignment)}. This operation is not supported.");
                        }

                        switch (assignment.Status)
                        {
                            case StatusOptions.InProgress:
                                //Do nothing
                                break;
                            case StatusOptions.Found:
                            case StatusOptions.Failed:
                            case StatusOptions.Timeout:
                                polling = false;
                                break;
                            default:
                                throw new InvalidOperationException("Assignment status was a value other than 'In Progress', 'Found', 'Timeout' or 'Failed'! Mismatch between Matchmaker SDK expected responses and service API values! Status value: '{assignment.Status}'");
                        }
                    }
                }
                catch (Exception)
                {
#pragma warning disable CS4014 // Can't await in coroutines, so the method execution will continue
                    StopSearch();
#pragma warning restore CS4014 // Can't await in coroutines, so the method execution will continue
                    onMatchSearchCompleted?.Invoke(assignment);
                    throw;
                }
            }

#pragma warning disable CS4014 // Can't await in coroutines, so the method execution will continue
            StopSearch();
#pragma warning restore CS4014 // Can't await in coroutines, so the method execution will continue
            onMatchSearchCompleted?.Invoke(assignment);
        }
    }
}
