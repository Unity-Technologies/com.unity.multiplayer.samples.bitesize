using System;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class MatchController : Controller<GameApplication>
    {
        MatchView View => App.View.Match;

        void Awake()
        {
            App.Model.Countdown.OnValueChanged += OnCountdownChanged;
            App.Model.PlayersConnected.OnValueChanged += OnPlayersConnectedChanged;
            App.Model.NetworkedGameState.OnMatchStarted += OnMatchStarted;
            App.Model.NetworkedGameState.OnMatchEnded += OnMatchEnded;
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            App.Model.Countdown.OnValueChanged -= OnCountdownChanged;
            App.Model.PlayersConnected.OnValueChanged -= OnPlayersConnectedChanged;
            App.Model.NetworkedGameState.OnMatchStarted -= OnMatchStarted;
            App.Model.NetworkedGameState.OnMatchEnded -= OnMatchEnded;
        }

        void OnCountdownChanged(uint previousValue, uint newValue)
        {
            View.OnCountdownChanged(newValue);
        }

        void OnPlayersConnectedChanged(int previousValue, int newValue)
        {
            View.OnPlayersConnectedChanged(newValue);
        }

        void OnMatchEnded()
        {
            Broadcast(new EndMatchEvent());
        }

        void OnMatchStarted()
        {
            Broadcast(new StartMatchEvent());
        }
    }
}
