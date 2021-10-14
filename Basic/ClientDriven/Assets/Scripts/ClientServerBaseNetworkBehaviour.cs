namespace Unity.Netcode.Samples
{
    /// <summary>
    /// Helper to reduce the amount of times you forget about isServer/isClient checks. With this, all classes are server driven by default,
    /// which can be overriden with the virtual properties Both/ServerOnly/ClientOnly
    /// This really is an experiment, there will still be Netcode events called on disabled GameObjects. This class might give a false sense of security
    /// </summary>
    public abstract class ClientServerBaseNetworkBehaviour : NetworkBehaviour
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
            }
        }
    }
}