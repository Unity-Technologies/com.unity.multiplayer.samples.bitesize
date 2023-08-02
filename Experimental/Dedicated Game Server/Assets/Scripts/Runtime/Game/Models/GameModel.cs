using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class GameModel : Model<GameApplication>
    {
        [SerializeField]
        MatchDataSynchronizer matchDataSnchronizerPrefab;
        internal MatchDataSynchronizer matchDataSynchronizer;
        internal const uint k_CountdownStartValue = 60;
        internal uint CountdownValue
        {
            get { return matchDataSynchronizer.matchCountdown.Value; }
            set { matchDataSynchronizer.matchCountdown.Value = value; }
        }

        internal bool MatchEnded
        {
            get { return matchDataSynchronizer.matchEnded.Value; }
            set { matchDataSynchronizer.matchEnded.Value = value; }
        }

        void Awake()
        {
            if (CustomNetworkManager.Singleton.IsServer)
            {
                matchDataSynchronizer = Instantiate(matchDataSnchronizerPrefab);
                matchDataSynchronizer.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}
