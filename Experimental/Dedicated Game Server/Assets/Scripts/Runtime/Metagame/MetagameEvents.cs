using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    internal class EnterMatchmakerQueueEvent : AppEvent
    {
        public string QueueName { get; private set; }

        public EnterMatchmakerQueueEvent(string queueName)
        {
            QueueName = queueName;
        }
    }

    internal class ExitMatchmakerQueueEvent : AppEvent { }
    
    internal class EnterIPConnectionEvent : AppEvent { }
    
    internal class ExitIPConnectionEvent : AppEvent { }

    internal class JoinThroughDirectIPEvent : AppEvent
    {
        public string ipAddress;
        public ushort port;
    }
    
    /// <summary>
    /// Called when a match is entered (I.E: after matchmaking finds enough players)
    /// </summary>
    internal class MatchEnteredEvent : AppEvent { }
    
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

    internal class ObjectClickedEvent : AppEvent
    {
        public GameObject Object { get; private set; }

        public ObjectClickedEvent(GameObject obj)
        {
            Object = obj;
        }
    }
}
