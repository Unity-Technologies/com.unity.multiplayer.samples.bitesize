using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    interface ISpawnable
    {
        NetworkObject NetworkObject
        {
            get;
        }

        void Init(SessionOwnerNetworkObjectSpawner networkObjectSpawner);
    }
}
