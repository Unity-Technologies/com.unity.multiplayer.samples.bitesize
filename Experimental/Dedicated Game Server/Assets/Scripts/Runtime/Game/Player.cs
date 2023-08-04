using Unity.Netcode;
using UnityEngine;

namespace Unity.Template.Multiplayer.NGO.Runtime
{
    public class Player : NetworkBehaviour
    {
        [ServerRpc]
        internal void OnPlayerAskedToWinServerRpc()
        {
            GameApplication.Instance.Broadcast(new EndMatchEvent(this));
        }
    }
}
