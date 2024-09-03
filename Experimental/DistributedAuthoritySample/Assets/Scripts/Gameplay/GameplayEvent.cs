using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    static class NetworkObjectDespawnedEvent
    {
        static event System.Action<NetworkObject> Event;

        public static void Susbscribe(System.Action<NetworkObject> other)
        {
            Event += other;
        }

        public static void Unsusbscribe(System.Action<NetworkObject> other)
        {
            Event -= other;
        }

        public static void Raise(NetworkObject other)
        {
            Event?.Invoke(other);
        }
    }

    enum GameplayEvent
    {
        Despawned,
        OwnershipChange
    }
}
