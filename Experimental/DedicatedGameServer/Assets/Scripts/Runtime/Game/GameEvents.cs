namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class WinButtonClickedEvent : AppEvent { }
    internal class MatchEndAcknowledgedEvent : AppEvent { }

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

    internal class EndMatchEvent : AppEvent { }
}
