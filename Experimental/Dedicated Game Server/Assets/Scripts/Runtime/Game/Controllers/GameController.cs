using System.Collections;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class GameController : Controller<GameApplication>
    {
        static readonly WaitForSeconds k_WaitOneSecond = new WaitForSeconds(1);

        GameModel Model => App.Model;
        Coroutine m_CountdownRoutine;

        void Awake()
        {
            AddListener<StartMatchEvent>(OnServerStartMatch);
            AddListener<EndMatchEvent>(OnServerMatchEnded);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<StartMatchEvent>(OnServerStartMatch);
            RemoveListener<EndMatchEvent>(OnServerMatchEnded);
        }

        void OnServerStartMatch(StartMatchEvent evt)
        {
            if (evt.IsServer)
            {
                Debug.Log("[S] Starting match!");
                Model.MatchEnded = false;
                OnServerStartCountdown();
            }
            if (evt.IsClient)
            {
                Debug.Log("[C] Starting match!");
            }
        }

        void OnServerStartCountdown()
        {
            Model.CountdownValue = GameModel.k_CountdownStartValue;
            m_CountdownRoutine = StartCoroutine(OnServerDoCountdown());
        }

        IEnumerator OnServerDoCountdown()
        {
            while (Model.CountdownValue > 0
            && !Model.MatchEnded)
            {
                yield return k_WaitOneSecond;
                Model.CountdownValue--;
            }

            if (Model.MatchEnded) //somebody won
            {
                yield break;
            }
            OnServerCountdownExpired();
        }

        void OnServerCountdownExpired()
        {
            Broadcast(new EndMatchEvent(null));
        }

        void OnServerMatchEnded(EndMatchEvent evt)
        {
            if (Model.MatchEnded)
            {
                return;
            }
            Model.MatchEnded = true;
            if (m_CountdownRoutine != null)
            {
                StopCoroutine(m_CountdownRoutine);
                m_CountdownRoutine = null;
            }

            ulong winnerClientId = ulong.MaxValue;
            if (evt.Winner != null)
            {
                winnerClientId = evt.Winner.OwnerClientId;
            }
            Model.matchDataSynchronizer.OnClientMatchResultComputedClientRpc(winnerClientId);
        }
    }
}
