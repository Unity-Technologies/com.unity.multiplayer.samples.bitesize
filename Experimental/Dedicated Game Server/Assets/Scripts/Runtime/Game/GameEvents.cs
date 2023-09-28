namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class WinButtonClickedEvent : AppEvent { }
    internal class MatchEndAcknowledgedEvent : AppEvent { }
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
}
