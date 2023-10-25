using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Main View of the <see cref="GameApplication"></see>
    /// </summary>
    public class GameView : View<GameApplication>
    {
        internal MatchView Match => m_MatchView;

        [SerializeField]
        MatchView m_MatchView;

        internal MatchRecapView MatchRecap => m_MatchRecapView;

        [SerializeField]
        MatchRecapView m_MatchRecapView;

        void Awake()
        {
            if (App.IsDedicatedServer)
            {
                OnDedicatedServerDestroyViews();
            }
        }

        void OnDedicatedServerDestroyViews()
        {
            Destroy(gameObject);
        }
    }
}
