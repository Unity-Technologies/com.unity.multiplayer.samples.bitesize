using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class GameModel : Model<GameApplication>
    {
        [SerializeField]
        GameState matchDataSnchronizerPrefab;
        internal GameState gameState;
        internal const uint k_CountdownStartValue = 60;
        internal uint CountdownValue
        {
            get { return gameState.matchCountdown.Value; }
            set { gameState.matchCountdown.Value = value; }
        }

        internal bool MatchEnded
        {
            get { return gameState.matchEnded.Value; }
            set { gameState.matchEnded.Value = value; }
        }

        void Awake()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                gameState = Instantiate(matchDataSnchronizerPrefab);
                gameState.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}
