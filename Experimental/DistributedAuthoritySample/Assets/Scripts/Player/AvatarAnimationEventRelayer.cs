using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Player
{
    class AvatarAnimationEventRelayer : MonoBehaviour
    {
        internal event System.Action PickupActionAnimationEvent;

        // invoked by animation
        void PickupAction()
        {
            PickupActionAnimationEvent?.Invoke();
        }
    }
}
