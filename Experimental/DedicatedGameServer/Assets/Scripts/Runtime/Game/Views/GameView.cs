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
        
        internal GameMenuView Menu => m_GameMenuView;

        [SerializeField]
        GameMenuView m_GameMenuView;

        internal MatchRecapView MatchRecap => m_MatchRecapView;

        [SerializeField]
        MatchRecapView m_MatchRecapView;
    }
}
