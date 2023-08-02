using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
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
