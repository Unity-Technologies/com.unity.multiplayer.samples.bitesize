using Unity.Netcode;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class Player : NetworkBehaviour
    {
        [ServerRpc]
        internal void OnPlayerAskedToWinServerRpc()
        {
            OnServerPlayerAskedToWin();
        }

        internal void OnServerPlayerAskedToWin()
        {
            GameApplication.Instance.Broadcast(new EndMatchEvent(this));
        }
    }
}
