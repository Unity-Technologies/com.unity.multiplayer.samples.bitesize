using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Main model of the <see cref="GameApplication"></see>
    /// </summary>
    public class GameModel : Model<GameApplication>
    {
        [SerializeField]
        GameState m_GameState;

        public GameState GameState { get => m_GameState; }
        
        internal const uint k_CountdownStartValue = 60;
        internal uint CountdownValue
        {
            get => m_GameState.matchCountdown.Value;
            set => m_GameState.matchCountdown.Value = value;
        }

        internal bool MatchEnded
        {
            get => m_GameState.matchEnded.Value;
            set => m_GameState.matchEnded.Value = value;
        }
        
        internal bool MatchStarted
        {
            get => m_GameState.matchStarted.Value;
            set => m_GameState.matchStarted.Value = value;
        }
    }
}
