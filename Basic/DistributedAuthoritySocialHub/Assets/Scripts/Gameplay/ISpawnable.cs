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

        void Init(NetworkBehaviour networkObjectSpawner, int key = 0, int index = 0);
    }
}
