using System;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class MatchController : Controller<GameApplication>
    {
        MatchView View => App.View.Match;

        void Awake()
        {
            App.Model.Countdown.OnValueChanged += OnCountdownChanged;
            App.Model.MatchEnded.OnValueChanged += OnMatchEnded;
            AddListener<WinButtonClickedEvent>(OnClientWinButtonClicked);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            App.Model.Countdown.OnValueChanged -= OnCountdownChanged;
            App.Model.MatchEnded.OnValueChanged -= OnMatchEnded;
            RemoveListener<WinButtonClickedEvent>(OnClientWinButtonClicked);
        }

        void OnCountdownChanged(uint previousValue, uint newValue)
        {
            View.OnCountdownChanged(newValue);
        }

        void OnMatchEnded(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                Broadcast(new EndMatchEvent());
            }
        }

        void OnClientWinButtonClicked(WinButtonClickedEvent evt) { } // todo replace with disconnect logic when updating UI
    }
}
