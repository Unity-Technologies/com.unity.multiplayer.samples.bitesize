using UnityEngine;
using Unity.Multiplayer.Samples.SocialHub.Gameplay;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    public class Chest : CarryableObject
    {
        protected override void OnDestroy()
        {
            // Specific behavior for chest destruction
            GameObject rubble = Instantiate(Resources.Load("Rubble_Chest")) as GameObject;
            rubble.transform.position = transform.position;
            base.OnDestroy(); // Call the base method to propagate the event
        }
    }
}

