using Unity.Netcode;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Main model of the <see cref="GameApplication"></see>
    /// </summary>
    public class GameModel : Model<GameApplication>
    {
        [SerializeField]
        NetworkedGameState m_NetworkedGameState;

        public NetworkedGameState NetworkedGameState => m_NetworkedGameState;

        internal NetworkVariable<uint> Countdown => m_NetworkedGameState.matchCountdown;
        
        internal NetworkVariable<int> PlayersConnected => m_NetworkedGameState.playersConnected;

        internal NetworkVariable<bool> MatchEnded => m_NetworkedGameState.matchEnded;

        internal bool MatchStarted => m_NetworkedGameState.matchStarted.Value;

        internal bool m_MenuVisible = false;

        public ClientPlayerCharacter PlayerCharacter { get; set; }
    }
}
