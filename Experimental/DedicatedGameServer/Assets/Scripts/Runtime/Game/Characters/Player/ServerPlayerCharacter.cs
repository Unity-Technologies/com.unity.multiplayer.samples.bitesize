using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Assumes client authority
    /// </summary>
    [RequireComponent(typeof(NetworkedPlayerCharacter))]
    public class ServerPlayerCharacter : MonoBehaviour
    {
        // Those events are caught by ThirdPersonController on clients, but since it doesn't exist on the server,
        // they have to also be caught here, where they do nothing. This is required because we are using the
        // NetworkAnimator component to synchronize animations between clients. This component requires an animator to
        // also be present on the server. While this prevents the server from stripping out the Animator component, it
        // is simple to do if we want to synchronize an animation between clients without having to manually synchronize
        // the animation triggers and parameters.
        void OnFootstep(AnimationEvent animationEvent) { }

        void OnLand(AnimationEvent animationEvent) { }
    }
}
