using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Samples.SocialHub.Gameplay
{
    public class CarryableObject : MonoBehaviour
    {
        public GameObject LeftHand;
        public GameObject RightHand;
        public int Health = 1;

        // Add health component to object
        public int CurrentHealth
        {
            get => Health;
            set
            {
                Health = value;
                if (Health <= 0)
                {
                    DestroyObject();
                }
            }
        }

        // Destroy object if health is <= 0
        protected virtual void DestroyObject()
        {
            Destroy(gameObject);
        }

        // Method called when object is destroyed
        protected virtual void OnDestroy()
        {
            // Send local player's destruction event to server, propagate to other clients
            /*if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject
                    .GetComponent<PlayerObject>()
                    .SendObjectDestruction(transform.position, gameObject.GetType().Name); // Sending the object type name here
            }*/
        }
    }
}


