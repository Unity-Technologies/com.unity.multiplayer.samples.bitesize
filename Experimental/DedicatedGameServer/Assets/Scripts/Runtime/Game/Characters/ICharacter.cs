using Unity.Netcode;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal interface ICharacter
    {
        NetworkObject NetworkObject { get; }
    }
}
