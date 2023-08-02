using Unity.Netcode;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class MatchController : Controller<GameApplication>
    {
        MatchView View => App.View.Match;

        void Awake()
        {
            AddListener<CountdownChangedEvent>(OnCountdownChanged);
            AddListener<WinButtonClickedEvent>(OnClientWinButtonClicked);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<CountdownChangedEvent>(OnCountdownChanged);
            RemoveListener<WinButtonClickedEvent>(OnClientWinButtonClicked);
        }

        void OnCountdownChanged(CountdownChangedEvent evt)
        {
            View.OnCountdownChanged(evt.NewValue);
        }

        void OnClientWinButtonClicked(WinButtonClickedEvent evt)
        {
            NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Player>().OnPlayerAskedToWinServerRpc();
        }
    }
}
