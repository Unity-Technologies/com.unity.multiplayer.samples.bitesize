using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    interface IGameplayEventInvokable
    {
        event System.Action<NetworkObject, GameplayEvent> OnGameplayEvent;
    }
}
