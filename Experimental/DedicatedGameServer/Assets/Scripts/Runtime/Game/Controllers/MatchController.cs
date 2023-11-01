using System;
using Unity.DedicatedGameServerSample.Runtime.ApplicationLifecycle;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class MatchController : Controller<GameApplication>
    {
        MatchView View => App.View.Match;

        void Awake()
        {
            App.Model.Countdown.OnValueChanged += OnCountdownChanged;
            App.Model.PlayersConnected.OnValueChanged += OnPlayersConnectedChanged;
            App.Model.MatchEnded.OnValueChanged += OnMatchEnded;
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            App.Model.Countdown.OnValueChanged -= OnCountdownChanged;
            App.Model.PlayersConnected.OnValueChanged -= OnPlayersConnectedChanged;
            App.Model.MatchEnded.OnValueChanged -= OnMatchEnded;
        }

        void OnCountdownChanged(uint previousValue, uint newValue)
        {
            View.OnCountdownChanged(newValue);
        }

        void OnPlayersConnectedChanged(int previousValue, int newValue)
        {
            View.OnPlayersConnectedChanged(newValue);
        }

        void OnMatchEnded(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                Broadcast(new EndMatchEvent());
            }
        }
    }
}
