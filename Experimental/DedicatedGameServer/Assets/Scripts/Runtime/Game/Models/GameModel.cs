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

        public NetworkVariable<uint> Countdown => m_NetworkedGameState.matchCountdown;

        public NetworkVariable<int> PlayersConnected => m_NetworkedGameState.playersConnected;

        public bool MenuVisible { get; set; } = false;

        public ClientPlayerCharacter PlayerCharacter { get; set; }
    }
}
