using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Input
{
    static class GameInput
    {
        /// <summary>
        /// This initialization is required in the Editor to avoid the instance from a previous Playmode to stay alive in the next session.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeInitializeOnLoad()
        {
            Actions = new AvatarActions();
            Actions.Enable();
        }

        internal static AvatarActions Actions { get; private set; } = null!;
    }
}
