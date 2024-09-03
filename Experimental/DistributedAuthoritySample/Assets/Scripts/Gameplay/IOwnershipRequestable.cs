using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    interface IOwnershipRequestable
    {
        event Action<NetworkBehaviour, NetworkObject.OwnershipRequestResponseStatus> OnNetworkObjectOwnershipRequestResponse;
    }
}
