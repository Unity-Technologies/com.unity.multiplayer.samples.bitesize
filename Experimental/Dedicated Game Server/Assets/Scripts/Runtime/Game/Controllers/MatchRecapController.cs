using Unity.DedicatedGameServerSample.Runtime.ApplicationLifecycle;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class MatchRecapController : Controller<GameApplication>
    {
        MatchRecapView View => App.View.MatchRecap;

        void Awake()
        {
            AddListener<MatchResultComputedEvent>(OnClientMatchResultComputed);
            AddListener<MatchEndAcknowledgedEvent>(OnClientMatchEndAcknowledged);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<MatchResultComputedEvent>(OnClientMatchResultComputed);
            RemoveListener<MatchEndAcknowledgedEvent>(OnClientMatchEndAcknowledged);
        }

        void OnClientMatchResultComputed(MatchResultComputedEvent evt)
        {
            View.OnClientMatchResultComputed(evt);
        }

        void OnClientMatchEndAcknowledged(MatchEndAcknowledgedEvent evt)
        {
            ApplicationEntryPoint.Singleton.OnClientDoPostMatchCleanupAndReturnToMetagame();
        }
    }
}
