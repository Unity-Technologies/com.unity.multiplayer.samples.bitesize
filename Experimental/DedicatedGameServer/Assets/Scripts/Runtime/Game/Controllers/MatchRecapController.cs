using Unity.DedicatedGameServerSample.Runtime.ApplicationLifecycle;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class MatchRecapController : Controller<GameApplication>
    {
        MatchRecapView View => App.View.MatchRecap;

        void Awake()
        {
            AddListener<EndMatchEvent>(OnClientEndMatch);
            AddListener<MatchEndAcknowledgedEvent>(OnClientMatchEndAcknowledged);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<EndMatchEvent>(OnClientEndMatch);
            RemoveListener<MatchEndAcknowledgedEvent>(OnClientMatchEndAcknowledged);
        }

        void OnClientEndMatch(EndMatchEvent evt)
        {
            App.Model.PlayerCharacter.SetInputsActive(false);
            View.OnClientEndMatch(evt);
        }

        void OnClientMatchEndAcknowledged(MatchEndAcknowledgedEvent evt)
        {
            ApplicationEntryPoint.Singleton.ConnectionManager.RequestShutdown();
        }
    }
}
