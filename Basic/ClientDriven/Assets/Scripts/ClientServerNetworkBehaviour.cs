using UnityEngine.Assertions;

namespace Unity.Netcode.Samples
{
    public abstract class ClientServerNetworkBehaviour : NetworkBehaviour
    {
        protected virtual bool Both { get; } = false;
        protected virtual bool ServerOnly { get; } = true;
        protected virtual bool ClientOnly { get; } = false;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (Both)
            {
                return;
            }

            if (ClientOnly)
            {
                enabled = IsClient;
                return;
            }

            if (ServerOnly)
            {
                enabled = IsServer;
                return;
            }
        }
    }
}