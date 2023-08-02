using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    internal class AppEvent { }
    internal class BackButtonClickedEvent : AppEvent { }
    internal class WinButtonClickedEvent : AppEvent { }
    internal class MatchEndAcknowledgedEvent : AppEvent { }
    internal class EnterMatchmakerQueueEvent : AppEvent
    {
        public string QueueName { get; private set; }

        public EnterMatchmakerQueueEvent(string queueName)
        {
            QueueName = queueName;
        }
    }
    internal class ExitMatchmakerQueueEvent : AppEvent { }
    /// <summary>
    /// Called when a match is entered (I.E: after matchmaking finds enough players)
    /// </summary>
    internal class MatchEnteredEvent : AppEvent { }
    internal class CountdownChangedEvent : AppEvent
    {
        public uint NewValue { get; private set; }

        public CountdownChangedEvent(uint newValue)
        {
            NewValue = newValue;
        }
    }
    internal class StartMatchEvent : AppEvent
    {
        public bool IsServer { get; private set; }
        public bool IsClient { get; private set; }

        public StartMatchEvent(bool isServer, bool isClient)
        {
            IsServer = isServer;
            IsClient = isClient;
        }
    }

    internal class PlayerSignedIn : AppEvent
    {
        public bool Success { get; private set; }
        public string PlayerId { get; private set; }

        public PlayerSignedIn(bool success, string playerId)
        {
            Success = success;
            PlayerId = playerId;
        }
    }
    internal class EndMatchEvent : AppEvent
    {
        public Player Winner { get; private set; }

        public EndMatchEvent(Player winner)
        {
            Winner = winner;
        }
    }

    internal class MatchResultComputedEvent : AppEvent
    {
        public ulong WinnerClientId { get; private set; }

        public MatchResultComputedEvent(ulong winnerClientId)
        {
            WinnerClientId = winnerClientId;
        }
    }

    internal class ObjectClickedEvent : AppEvent
    {
        public GameObject Object { get; private set; }

        public ObjectClickedEvent(GameObject obj)
        {
            Object = obj;
        }
    }
}
