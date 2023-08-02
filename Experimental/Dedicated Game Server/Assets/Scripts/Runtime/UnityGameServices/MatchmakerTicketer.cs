using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using StatusOptions = Unity.Services.Matchmaker.Models.MultiplayAssignment.StatusOptions;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    ///<summary>
    ///Holds matchmaker search logic
    ///</summary>
    public class MatchmakerTicketer : MonoBehaviour
    {
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
                        Debug.LogError($"A Matchmaking ticket is already active for this client!");
                        return;
                    }

                    Searching = true;
                    await StartSearch(queueName, onMatchSearchCompleted, onMatchmakerTicked);
                }
                else
                {
                    if (m_TicketId.Length == 0)
                    {
                        Debug.LogError("Cannot delete ticket as no ticket is currently active for this client!");
                        return;
                    }

                    await StopSearch();
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
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
            m_TicketId = ticketResponse.Id;
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

            while (polling)
            {
                var task = Task.Run(() => MatchmakerService.Instance.GetTicketAsync(m_TicketId));
                yield return CoroutinesHelper.OneSecond;
                elapsedTime++;
                onMatchmakerTicked?.Invoke(elapsedTime);

                if (task.IsCompleted)
                {
                    response = task.Result;

                    if (response.Type == typeof(MultiplayAssignment))
                    {
                        assignment = response.Value as MultiplayAssignment;
                    }

                    if (assignment == null)
                    {
                        var message = $"GetTicketStatus returned a type that was not a {nameof(MultiplayAssignment)}. This operation is not supported.";
                        throw new InvalidOperationException(message);
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
                            throw new InvalidOperationException("Assignment status was a value other than 'In Progress', 'Found', 'Timeout' or 'Failed'! " +
                                $"Mismatch between Matchmaker SDK expected responses and service API values! Status value: '{assignment.Status}'");
                    }
                }
            }
#pragma warning disable CS4014 // Can't await in coroutines, so the method execution will continue
            StopSearch();
#pragma warning restore CS4014 // Can't await in coroutines, so the method execution will continue
            onMatchSearchCompleted?.Invoke(assignment);
        }
    }
}
