using System;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    interface IOwnershipRequestable
    {
        event Action<NetworkObject, NetworkObject.OwnershipRequestResponseStatus> OnNetworkObjectOwnershipRequestResponse;
    }
}
